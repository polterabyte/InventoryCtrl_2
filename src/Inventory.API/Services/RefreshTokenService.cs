using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Inventory.API.Models;

namespace Inventory.API.Services;

/// <summary>
/// Service for managing JWT refresh tokens
/// </summary>
public class RefreshTokenService(
    UserManager<User> userManager,
    IConfiguration configuration,
    ILogger<RefreshTokenService> logger)
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<RefreshTokenService> _logger = logger;

    /// <summary>
    /// Generates a new refresh token for the user
    /// </summary>
    /// <param name="user">The user to generate token for</param>
    /// <returns>Refresh token string</returns>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Sets refresh token for user and saves to database
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="refreshToken">The refresh token</param>
    /// <returns>Task representing the operation</returns>
    public async Task SetRefreshTokenAsync(User user, string refreshToken)
    {
        var refreshTokenExpiryDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpireDays", 7);
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
        
        await _userManager.UpdateAsync(user);
        
        _logger.LogInformation("Refresh token set for user {UserId}, expires at {Expiry}", 
            user.Id, user.RefreshTokenExpiry);
    }

    /// <summary>
    /// Validates refresh token for user
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="refreshToken">The refresh token to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateRefreshToken(User user, string refreshToken)
    {
        if (user.RefreshToken != refreshToken)
        {
            _logger.LogWarning("Invalid refresh token for user {UserId}", user.Id);
            return false;
        }

        if (user.RefreshTokenExpiry == null || user.RefreshTokenExpiry <= DateTime.UtcNow)
        {
            _logger.LogWarning("Expired refresh token for user {UserId}", user.Id);
            return false;
        }

        _logger.LogInformation("Valid refresh token for user {UserId}", user.Id);
        return true;
    }

    /// <summary>
    /// Revokes refresh token for user
    /// </summary>
    /// <param name="user">The user</param>
    /// <returns>Task representing the operation</returns>
    public async Task RevokeRefreshTokenAsync(User user)
    {
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        
        await _userManager.UpdateAsync(user);
        
        _logger.LogInformation("Refresh token revoked for user {UserId}", user.Id);
    }

    /// <summary>
    /// Revokes refresh token for user by username
    /// </summary>
    /// <param name="username">The username</param>
    /// <returns>Task representing the operation</returns>
    public async Task RevokeRefreshTokenAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user != null)
        {
            await RevokeRefreshTokenAsync(user);
        }
    }

    /// <summary>
    /// Cleans up expired refresh tokens for all users
    /// </summary>
    /// <returns>Number of tokens cleaned up</returns>
    public async Task<int> CleanupExpiredTokensAsync()
    {
        var users = _userManager.Users
            .Where(u => u.RefreshTokenExpiry != null && u.RefreshTokenExpiry <= DateTime.UtcNow)
            .ToList();

        var count = 0;
        foreach (var user in users)
        {
            if (user.RefreshToken != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _userManager.UpdateAsync(user);
                count++;
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired refresh tokens", count);
        }

        return count;
    }

    /// <summary>
    /// Generates a new JWT access token for user
    /// </summary>
    /// <param name="user">The user</param>
    /// <returns>JWT token string</returns>
    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        var userClaims = await _userManager.GetClaimsAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
        };

        claims.AddRange(userClaims);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpireMinutes", 15));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
