using Inventory.Shared.Interfaces;
using Inventory.Shared.DTOs;
using System.Threading.Tasks;
using Inventory.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Inventory.API.Enums;
using System.Linq;
using System;
using System.Collections.Generic;
using Serilog;

namespace Inventory.API.Services
{
    public class AuthService 
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IInternalAuditService _auditService;
        private readonly Serilog.ILogger _logger;

        public AuthService(
            UserManager<User> userManager,
            IConfiguration config,
            IRefreshTokenService refreshTokenService,
            IInternalAuditService auditService,
            Serilog.ILogger logger)
        {
            _userManager = userManager;
            _config = config;
            _refreshTokenService = refreshTokenService;
            _auditService = auditService;
            _logger = logger.ForContext<AuthService>();
        }

        public async Task<ApiResponse<LoginResult>> LoginAsync(LoginRequest request, string ipAddress, string userAgent, string requestId)
        {
            _logger.Information("User login attempt: {Username}", request.Username);

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.Warning("Failed login attempt: missing username or password for {Username}", request.Username);
                return ApiResponse<LoginResult>.ErrorResult("Username and password are required");
            }

            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                _logger.Warning("Failed login attempt: user {Username} not found", request.Username);
                await _auditService.LogDetailedChangeAsync("User", "Unknown", "LOGIN_FAILED", ActionType.Login, "User", new { Username = request.Username, Reason = "User not found" }, requestId, $"Failed login attempt for username '{request.Username}'", "WARNING", false, "Invalid credentials");
                return ApiResponse<LoginResult>.ErrorResult("Invalid credentials");
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.Warning("Failed login attempt: invalid password for user {Username}", request.Username);
                
                await _auditService.LogDetailedChangeAsync(
                    "User",
                    user.Id,
                    "LOGIN_FAILED",
                    ActionType.Login,
                    "User",
                    new { 
                        Username = request.Username, 
                        Reason = "Invalid password",
                        AttemptTime = DateTime.UtcNow,
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    },
                    requestId,
                    $"Failed login attempt for username '{request.Username}'",
                    "WARNING",
                    false,
                    "Invalid credentials"
                );
                
                return ApiResponse<LoginResult>.ErrorResult("Invalid credentials");
            }

            var accessToken = await _refreshTokenService.GenerateAccessTokenAsync(user);
            var refreshToken = _refreshTokenService.GenerateRefreshToken();
            
            await _refreshTokenService.SetRefreshTokenAsync(user, refreshToken);

            var roles = await _userManager.GetRolesAsync(user);
            var expires = DateTime.UtcNow.AddMinutes(int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 15);

            _logger.Information("Successful login for user {Username} with role {Role}. Email: {Email}", 
                user.UserName, user.Role, user.Email);

            await _auditService.LogDetailedChangeAsync(
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
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    TokenExpiresAt = expires
                },
                requestId,
                $"User '{user.UserName}' successfully logged in",
                "INFO",
                true
            );

            return ApiResponse<LoginResult>.SuccessResult(new LoginResult
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role,
                Roles = roles.ToList(),
                ExpiresAt = expires
            });
        }

        public async Task<ApiResponse<LoginResult>> RefreshTokenAsync(RefreshRequest request)
        {
            _logger.Information("Token refresh attempt for user: {Username}", request.Username);
            
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _logger.Warning("Failed refresh attempt: missing username or refresh token");
                return ApiResponse<LoginResult>.ErrorResult("Username and refresh token are required");
            }

            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                _logger.Warning("Failed refresh attempt: user {Username} not found", request.Username);
                return ApiResponse<LoginResult>.ErrorResult("Invalid refresh token");
            }

            if (!_refreshTokenService.ValidateRefreshToken(user, request.RefreshToken))
            {
                _logger.Warning("Failed refresh attempt: invalid or expired refresh token for user {Username}", request.Username);
                return ApiResponse<LoginResult>.ErrorResult("Invalid or expired refresh token");
            }

            var accessToken = await _refreshTokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = _refreshTokenService.GenerateRefreshToken();
            
            await _refreshTokenService.SetRefreshTokenAsync(user, newRefreshToken);

            var roles = await _userManager.GetRolesAsync(user);
            var expires = DateTime.UtcNow.AddMinutes(int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 15);

            _logger.Information("Token refreshed successfully for user {Username}", user.UserName);

            return ApiResponse<LoginResult>.SuccessResult(new LoginResult
            {
                Token = accessToken,
                RefreshToken = newRefreshToken,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role,
                Roles = roles.ToList(),
                ExpiresAt = expires
            });
        }

        public async Task<ApiResponse<object>> RegisterAsync(RegisterRequest request)
        {
            _logger.Information("New user registration attempt: {Username}, Email: {Email}", 
                request.Username, request.Email);
            
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                _logger.Warning("Failed registration attempt: missing required fields for {Username}", request.Username);
                return ApiResponse<object>.ErrorResult("Username, email and password are required");
            }

            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                _logger.Warning("Failed registration attempt: username {Username} already exists", request.Username);
                return ApiResponse<object>.ErrorResult("Username already exists");
            }

            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                _logger.Warning("Failed registration attempt: email {Email} already in use", request.Email);
                return ApiResponse<object>.ErrorResult("Email already exists");
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.Username,
                Email = request.Email,
                EmailConfirmed = true,
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                _logger.Information("Successful user registration: {Username} with email {Email}", 
                    user.UserName, user.Email);
                return ApiResponse<object>.SuccessResult(new { message = "User created successfully" });
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.Error("Failed user registration for {Username}: {Errors}", 
                request.Username, string.Join(", ", errors));
            return ApiResponse<object>.ErrorResult("User creation failed", errors);
        }

        public async Task LogoutAsync(string username)
        {
            _logger.Information("User logout: {Username}", username);
            
            if (!string.IsNullOrEmpty(username))
            {
                await _refreshTokenService.RevokeRefreshTokenAsync(username);
            }
        }
    }
}
