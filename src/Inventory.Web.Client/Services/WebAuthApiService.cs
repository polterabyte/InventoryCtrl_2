using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

public class WebAuthApiService : WebBaseApiService, IAuthService
{
    public WebAuthApiService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<WebAuthApiService> logger) 
        : base(httpClient, jsRuntime, logger)
    {
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        Logger.LogInformation("Attempting login for user: {Username}", request.Username);
        
        var response = await PostAsync<LoginResult>(ApiEndpoints.Login, request);
        
        Logger.LogInformation("Login response - Success: {Success}, HasData: {HasData}, HasToken: {HasToken}", 
            response.Success, 
            response.Data != null, 
            response.Data?.Token != null);
        
        if (response.Success && response.Data != null && !string.IsNullOrEmpty(response.Data.Token))
        {
            Logger.LogInformation("Login successful for user: {Username}", request.Username);
            return new AuthResult
            {
                Success = true,
                Token = response.Data.Token,
                RefreshToken = response.Data.RefreshToken ?? string.Empty,
                ExpiresAt = response.Data.ExpiresAt
            };
        }

        Logger.LogWarning("Login failed for user: {Username}, Error: {Error}", 
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
        var request = new RefreshTokenRequest { RefreshToken = refreshToken };
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

    public async Task<bool> LogoutAsync()
    {
        var response = await PostAsync<object>(ApiEndpoints.Logout, new { });
        return response.Success;
    }
}
