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
    public class AuthController(AuthService authService, ILogger<AuthController> logger) : ControllerBase
    {
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
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();
            var requestId = HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            
            var result = await authService.LoginAsync(request, ipAddress, userAgent, requestId);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
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
            var result = await authService.RefreshTokenAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
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
            await authService.LogoutAsync(username ?? string.Empty);
            return Ok(ApiResponse<object>.SuccessResult(new { message = "Logged out successfully" }));
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">User registration data</param>
        /// <returns>Registration confirmation</returns>
        /// <response code="201">User created successfully</response>
        /// <response code="400">Invalid request data or user already exists</response>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await authService.RegisterAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Created("", result);
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
