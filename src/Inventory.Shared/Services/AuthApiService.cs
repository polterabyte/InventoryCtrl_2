using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using System.Net.Http.Json;

namespace Inventory.Shared.Services;

public class AuthApiService : BaseApiService, IAuthService
{
    public AuthApiService(HttpClient httpClient) : base(httpClient, ApiEndpoints.BaseUrl)
    {
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var response = await PostAsync<LoginResult>(ApiEndpoints.Login, request);
        
        if (response.Success && response.Data != null)
        {
            return new AuthResult
            {
                Success = true,
                Token = response.Data.Token,
                RefreshToken = response.Data.RefreshToken,
                ExpiresAt = response.Data.ExpiresAt
            };
        }

        return new AuthResult
        {
            Success = false,
            ErrorMessage = response.ErrorMessage
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
