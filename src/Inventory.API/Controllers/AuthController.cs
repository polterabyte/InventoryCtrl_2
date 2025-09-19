using Inventory.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Serilog;
using Inventory.API.Services;
using Inventory.API.Enums;
using Microsoft.AspNetCore.RateLimiting;


namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(UserManager<Inventory.API.Models.User> userManager, IConfiguration config, ILogger<AuthController> logger, RefreshTokenService refreshTokenService, AuditService auditService) : ControllerBase
    {
        private readonly IConfiguration _config = config;
        private readonly ILogger<AuthController> _logger = logger;

        /// <summary>
        /// Authenticates a user and returns a JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user information</returns>
        /// <response code="200">Login successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid credentials</response>
        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("User login attempt: {Username}", request.Username);
            
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Failed login attempt: missing username or password for {Username}", request.Username);
                return BadRequest(new ApiResponse<LoginResult>
                {
                    Success = false,
                    ErrorMessage = "Username and password are required"
                });
            }

            var user = await userManager.FindByNameAsync(request.Username);
            _logger.LogInformation("User lookup result for {Username}: {UserFound}", request.Username, user != null ? "Found" : "Not found");
            
            if (user != null)
            {
                _logger.LogInformation("User details - Email: {Email}, Roles: {Roles}", 
                    user.Email, user.Role);
                    
                var passwordCheck = await userManager.CheckPasswordAsync(user, request.Password);
                _logger.LogInformation("Password check result for {Username}: {PasswordValid}", request.Username, passwordCheck);
            }
            
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Failed login attempt: invalid credentials for user {Username}", request.Username);
                
                // Log failed login attempt with enhanced details
                var failedRequestId = HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
                await auditService.LogDetailedChangeAsync(
                    "User",
                    user?.Id ?? "Unknown",
                    "LOGIN_FAILED",
                    ActionType.Login,
                    "User",
                    new { 
                        Username = request.Username, 
                        Reason = "Invalid credentials",
                        AttemptTime = DateTime.UtcNow,
                        IpAddress = GetClientIpAddress(),
                        UserAgent = Request.Headers.UserAgent.ToString()
                    },
                    failedRequestId,
                    $"Failed login attempt for username '{request.Username}'",
                    "WARNING",
                    false,
                    "Invalid credentials"
                );
                
                return Unauthorized(new ApiResponse<LoginResult>
                {
                    Success = false,
                    ErrorMessage = "Invalid credentials"
                });
            }

            // Generate access token using RefreshTokenService
            var accessToken = await refreshTokenService.GenerateAccessTokenAsync(user);
            var refreshToken = refreshTokenService.GenerateRefreshToken();
            
            // Set refresh token for user
            await refreshTokenService.SetRefreshTokenAsync(user, refreshToken);

            var roles = await userManager.GetRolesAsync(user);
            var expires = DateTime.UtcNow.AddMinutes(int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 15);

            _logger.LogInformation("Successful login for user {Username} with role {Role}. Email: {Email}", 
                user.UserName, user.Role, user.Email);

            // Log successful login with enhanced details
            var requestId = HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            await auditService.LogDetailedChangeAsync(
                "User",
                user.Id,
                "LOGIN_SUCCESS",
                ActionType.Login,
                "User",
                new { 
                    Username = user.UserName, 
                    Email = user.Email, 
                    Role = user.Role,
                    LoginTime = DateTime.UtcNow,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    TokenExpiresAt = expires
                },
                requestId,
                $"User '{user.UserName}' successfully logged in",
                "INFO",
                true
            );

            return Ok(new ApiResponse<LoginResult>
            {
                Success = true,
                Data = new LoginResult
                {
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    Username = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Role = user.Role,
                    Roles = roles.ToList(),
                    ExpiresAt = expires
                }
            });
        }

        /// <summary>
        /// Refreshes a JWT token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New JWT token</returns>
        /// <response code="200">Token refreshed successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid refresh token</response>
        [HttpPost("refresh")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            _logger.LogInformation("Token refresh attempt for user: {Username}", request.Username);
            
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _logger.LogWarning("Failed refresh attempt: missing username or refresh token");
                return BadRequest(new ApiResponse<LoginResult>
                {
                    Success = false,
                    ErrorMessage = "Username and refresh token are required"
                });
            }

            var user = await userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning("Failed refresh attempt: user {Username} not found", request.Username);
                return Unauthorized(new ApiResponse<LoginResult>
                {
                    Success = false,
                    ErrorMessage = "Invalid refresh token"
                });
            }

            // Validate refresh token
            if (!refreshTokenService.ValidateRefreshToken(user, request.RefreshToken))
            {
                _logger.LogWarning("Failed refresh attempt: invalid or expired refresh token for user {Username}", request.Username);
                return Unauthorized(new ApiResponse<LoginResult>
                {
                    Success = false,
                    ErrorMessage = "Invalid or expired refresh token"
                });
            }

            // Generate new access token
            var accessToken = await refreshTokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = refreshTokenService.GenerateRefreshToken();
            
            // Set new refresh token for user
            await refreshTokenService.SetRefreshTokenAsync(user, newRefreshToken);

            var roles = await userManager.GetRolesAsync(user);
            var expires = DateTime.UtcNow.AddMinutes(int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 15);

            _logger.LogInformation("Token refreshed successfully for user {Username}", user.UserName);

            return Ok(new ApiResponse<LoginResult>
            {
                Success = true,
                Data = new LoginResult
                {
                    Token = accessToken,
                    RefreshToken = newRefreshToken,
                    Username = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Role = user.Role,
                    Roles = roles.ToList(),
                    ExpiresAt = expires
                }
            });
        }

        /// <summary>
        /// Logs out the current user
        /// </summary>
        /// <returns>Logout confirmation</returns>
        /// <response code="200">Logout successful</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;
            _logger.LogInformation("User logout: {Username}", username);
            
            if (!string.IsNullOrEmpty(username))
            {
                // Revoke refresh token
                await refreshTokenService.RevokeRefreshTokenAsync(username);
            }
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "Logged out successfully" }
            });
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">User registration data</param>
        /// <returns>Registration confirmation</returns>
        /// <response code="201">User created successfully</response>
        /// <response code="400">Invalid request data or user already exists</response>
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
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Username, email and password are required"
                });
            }

            var existingUser = await userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                _logger.LogWarning("Failed registration attempt: username {Username} already exists", request.Username);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Username already exists"
                });
            }

            var existingEmail = await userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                _logger.LogWarning("Failed registration attempt: email {Email} already in use", request.Email);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Email already exists"
                });
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
                return Created("", new ApiResponse<object>
                {
                    Success = true,
                    Data = new { message = "User created successfully" }
                });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed user registration for {Username}: {Errors}", 
                request.Username, errors);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = errors
            });
        }


        private string GetClientIpAddress()
        {
            // Check for forwarded IP first
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for real IP
            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to connection remote IP
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
