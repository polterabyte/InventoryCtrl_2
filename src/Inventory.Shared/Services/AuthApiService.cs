using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class AuthApiService(HttpClient httpClient, ILogger<AuthApiService> logger) 
    : BaseApiService(httpClient, ApiEndpoints.BaseUrl, logger), IAuthService
{

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        logger.LogInformation("Attempting login for user: {Username}", request.Username);
        
        var response = await PostAsync<LoginResult>(ApiEndpoints.Login, request);
        
        logger.LogInformation("Login response - Success: {Success}, HasData: {HasData}, HasToken: {HasToken}", 
            response.Success, 
            response.Data != null, 
            response.Data?.Token != null);
        
        if (response.Success && response.Data != null && !string.IsNullOrEmpty(response.Data.Token))
        {
            logger.LogInformation("Login successful for user: {Username}", request.Username);
            return new AuthResult
            {
                Success = true,
                Token = response.Data.Token,
                RefreshToken = response.Data.RefreshToken ?? string.Empty,
                ExpiresAt = response.Data.ExpiresAt
            };
        }

        logger.LogWarning("Login failed for user: {Username}, Error: {Error}", 
            request.Username, response.ErrorMessage);
        
        return new AuthResult
        {
            Success = false,
            ErrorMessage = response.ErrorMessage ?? "Login failed. Please check your credentials."
        };
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var response = await PostAsync<object>(ApiEndpoints.Register, request);
        
        return new AuthResult
        {
            Success = response.Success,
            ErrorMessage = response.ErrorMessage
        };
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var request = new RefreshRequest { Username = "", RefreshToken = refreshToken };
        var response = await PostAsync<LoginResult>(ApiEndpoints.Refresh, request);
        
        if (response.Success && response.Data != null && !string.IsNullOrEmpty(response.Data.Token))
        {
            return new AuthResult
            {
                Success = true,
                Token = response.Data.Token,
                RefreshToken = response.Data.RefreshToken ?? string.Empty,
                ExpiresAt = response.Data.ExpiresAt
            };
        }

        return new AuthResult
        {
            Success = false,
            ErrorMessage = response.ErrorMessage ?? "Token refresh failed."
        };
    }

    public async Task<bool> LogoutAsync(string token)
    {
        // Add token to headers if needed
        var response = await PostAsync<bool>(ApiEndpoints.Logout, new { });
        return response.Success;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        // This would typically be handled by middleware or a separate endpoint
        // For now, we'll assume the token is valid if we can make a request
        try
        {
            var response = await HttpClient.GetAsync("/api/auth/validate");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserInfo?> GetUserInfoAsync(string token)
    {
        try
        {
            var response = await HttpClient.GetAsync("/api/auth/userinfo");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfo>();
            }
        }
        catch
        {
            // Ignore errors
        }
        
        return null;
    }
}
