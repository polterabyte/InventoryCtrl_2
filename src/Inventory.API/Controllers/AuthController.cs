using Inventory.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Serilog;
using Inventory.API.Services;


namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(UserManager<Inventory.API.Models.User> userManager, IConfiguration config, ILogger<AuthController> logger, IPortConfigurationService portService) : ControllerBase
    {
        private readonly IConfiguration _config = config;
        private readonly ILogger<AuthController> _logger = logger;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("User login attempt: {Username}", request.Username);
            
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Failed login attempt: missing username or password for {Username}", request.Username);
                return BadRequest("Username and password are required.");
            }

            var user = await userManager.FindByNameAsync(request.Username);
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Failed login attempt: invalid credentials for user {Username}", request.Username);
                return Unauthorized("Invalid credentials.");
            }

            var userClaims = await userManager.GetClaimsAsync(user);
            var roles = await userManager.GetRolesAsync(user);

            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            claims.AddRange(userClaims);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 60);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            _logger.LogInformation("Successful login for user {Username} with role {Role}. Email: {Email}", 
                user.UserName, user.Role, user.Email);

            return Ok(new LoginResult
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role,
                Roles = roles.ToList(),
                ExpiresAt = expires
            });
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest();
            return (IActionResult)Login(new LoginRequest { Username = request.Username, Password = "" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            _logger.LogInformation("New user registration attempt: {Username}, Email: {Email}", 
                request.Username, request.Email);
            
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                _logger.LogWarning("Failed registration attempt: missing required fields for {Username}", request.Username);
                return BadRequest("Username, email and password are required.");
            }

            var existingUser = await userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                _logger.LogWarning("Failed registration attempt: username {Username} already exists", request.Username);
                return BadRequest("Username already exists.");
            }

            var existingEmail = await userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                _logger.LogWarning("Failed registration attempt: email {Email} already in use", request.Email);
                return BadRequest("Email already exists.");
            }

            var user = new Inventory.API.Models.User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.Username,
                Email = request.Email,
                EmailConfirmed = true, // Для простоты подтверждаем email автоматически
                Role = "User", // По умолчанию обычный пользователь
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                // Назначить роль пользователю
                await userManager.AddToRoleAsync(user, "User");
                _logger.LogInformation("Successful user registration: {Username} with email {Email}", 
                    user.UserName, user.Email);
                return Ok(new { success = true, message = "User created successfully." });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed user registration for {Username}: {Errors}", 
                request.Username, errors);
            return BadRequest(errors);
        }

        [HttpGet("config/ports")]
        public IActionResult GetPortConfiguration()
        {
            try
            {
                var config = portService.LoadPortConfiguration();
                return Ok(new
                {
                    api = new
                    {
                        http = config.ApiHttp,
                        https = config.ApiHttps,
                        urls = $"https://localhost:{config.ApiHttps};http://localhost:{config.ApiHttp}"
                    },
                    web = new
                    {
                        http = config.WebHttp,
                        https = config.WebHttps,
                        urls = $"https://localhost:{config.WebHttps};http://localhost:{config.WebHttp}"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get port configuration");
                return StatusCode(500, "Failed to get port configuration");
            }
        }
    }
}
