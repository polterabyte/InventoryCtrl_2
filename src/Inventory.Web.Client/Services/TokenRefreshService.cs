using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Inventory.Web.Client.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Сервис для обновления JWT токена без циклических зависимостей
/// </summary>
public class TokenRefreshService : ITokenRefreshService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TokenRefreshService> _logger;
    private readonly TokenConfiguration _config;
    private readonly IUrlBuilderService _urlBuilderService;

    public TokenRefreshService(
        HttpClient httpClient,
        ILogger<TokenRefreshService> logger,
        IOptions<TokenConfiguration> config,
        IUrlBuilderService urlBuilderService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
        _urlBuilderService = urlBuilderService;
    }

    public async Task<AuthResult> RefreshTokenAsync(string username, string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Token refresh aborted: username is missing");
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Token refresh failed: missing username"
                };
            }

            _logger.LogDebug("Attempting to refresh token for user {Username}", username);

            var request = new RefreshRequest
            {
                Username = username,
                RefreshToken = refreshToken
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Build absolute refresh URL to avoid wrong base/cors issues
            var refreshUrl = await _urlBuilderService.BuildFullUrlAsync(ApiEndpoints.Refresh);
            var response = await _httpClient.PostAsync(refreshUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResult = JsonSerializer.Deserialize<LoginResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResult != null && !string.IsNullOrEmpty(loginResult.Token))
                {
                    _logger.LogInformation("Token refresh successful");
                    return new AuthResult
                    {
                        Success = true,
                        Token = loginResult.Token,
                        RefreshToken = loginResult.RefreshToken ?? string.Empty,
                        ExpiresAt = loginResult.ExpiresAt
                    };
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Token refresh failed with status: {StatusCode}. Body: {Body}", response.StatusCode, errorContent);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Token refresh failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Token refresh error"
            };
        }
    }
}

