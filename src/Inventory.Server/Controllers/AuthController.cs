using Inventory.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Inventory.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(UserManager<Inventory.Server.Models.User> userManager, IConfiguration config) : ControllerBase
    {
        private readonly IConfiguration _config = config;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required.");

            var user = await userManager.FindByNameAsync(request.Username);
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized("Invalid credentials.");

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
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Username, email and password are required.");

            var existingUser = await userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
                return BadRequest("Username already exists.");

            var existingEmail = await userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
                return BadRequest("Email already exists.");

            var user = new Inventory.Server.Models.User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.Username,
                Email = request.Email,
                EmailConfirmed = true, // Для простоты подтверждаем email автоматически
                Role = "User" // По умолчанию обычный пользователь
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                // Назначить роль пользователю
                await userManager.AddToRoleAsync(user, "User");
                return Ok(new { success = true, message = "User created successfully." });
            }

            return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
