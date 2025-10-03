using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Inventory.Shared.Interfaces;
using Inventory.Web.Client.Configuration;
using Inventory.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Сервис для управления JWT токенами
/// </summary>
public class TokenManagementService : ITokenManagementService
{
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";

    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<TokenManagementService> _logger;
    private readonly ITokenRefreshService _tokenRefreshService;
    private readonly IApiUrlService _apiUrlService;
    private readonly TokenManagementOptions _config;
    private readonly SemaphoreSlim _refreshTokenSemaphore = new(1, 1);

    public TokenManagementService(
        IHttpClientFactory httpClientFactory,
        ILocalStorageService localStorage,
        ILogger<TokenManagementService> logger,
        ITokenRefreshService tokenRefreshService,
        IApiUrlService apiUrlService,
        IOptions<TokenManagementOptions> tokenOptions)
    {
        // Создаем HttpClient без перехватчиков, чтобы избежать рекурсии
        _httpClient = httpClientFactory.CreateClient();
        _localStorage = localStorage;
        _logger = logger;
        _tokenRefreshService = tokenRefreshService;
        _apiUrlService = apiUrlService;
        _config = tokenOptions.Value;
    }

    public async Task<bool> IsTokenExpiringSoonAsync()
    {
        try
        {
            var token = await GetStoredTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("No access token found");
                return true;
            }

            var expTime = GetTokenExpirationTime(token);
            if (expTime == null)
            {
                _logger.LogWarning("Could not parse token expiration time");
                return true;
            }

            var timeUntilExpiry = expTime.Value - DateTimeOffset.UtcNow;
            var isExpiringSoon = timeUntilExpiry.TotalMinutes <= _config.RefreshThresholdMinutes;

            if (_config.EnableLogging)
            {
                _logger.LogDebug("Token expires in {Minutes:F1} minutes, threshold: {Threshold} minutes, expiring soon: {IsExpiringSoon}",
                    timeUntilExpiry.TotalMinutes, _config.RefreshThresholdMinutes, isExpiringSoon);
            }

            return isExpiringSoon;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token expiration");
            return true; // В случае ошибки считаем токен истекшим
        }
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        _logger.LogDebug("Attempting to acquire refresh semaphore.");
        await _refreshTokenSemaphore.WaitAsync();
        try
        {
            // После получения семафора, проверяем, не истек ли токен снова.
            // Возможно, пока мы ждали, другой поток уже все обновил.
            if (!await IsTokenExpiringSoonAsync())
            {
                _logger.LogDebug("Token was already refreshed by another thread while waiting.");
                return true;
            }

            _logger.LogInformation("Starting token refresh process.");

            var refreshToken = await _localStorage.GetItemAsStringAsync(RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("No refresh token available.");
                return false;
            }

            if (!await HasValidRefreshTokenAsync())
            {
                _logger.LogWarning("Refresh token has expired.");
                await ClearTokensAsync();
                return false;
            }

            for (int attempt = 1; attempt <= _config.MaxRefreshRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("Token refresh attempt {Attempt}/{MaxRetries}", attempt, _config.MaxRefreshRetries);

                    var result = await _tokenRefreshService.RefreshTokenAsync(refreshToken);
                    
                    if (result.Success && !string.IsNullOrEmpty(result.Token))
                    {
                        await SaveTokensAsync(result.Token, result.RefreshToken ?? refreshToken);
                        
                        _logger.LogInformation("Token refresh successful on attempt {Attempt}", attempt);
                        return true;
                    }

                    _logger.LogWarning("Token refresh failed on attempt {Attempt}: {Error}", 
                        attempt, result.ErrorMessage);

                    if (attempt < _config.MaxRefreshRetries)
                    {
                        await Task.Delay(_config.RefreshRetryDelayMs);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Token refresh attempt {Attempt} failed with exception", attempt);
                    
                    if (attempt < _config.MaxRefreshRetries)
                    {
                        await Task.Delay(_config.RefreshRetryDelayMs);
                    }
                }
            }

            _logger.LogError("Token refresh failed after {MaxRetries} attempts. Clearing tokens.", _config.MaxRefreshRetries);
            await ClearTokensAsync();
            return false;
        }
        finally
        {
            _logger.LogDebug("Releasing refresh semaphore.");
            _refreshTokenSemaphore.Release();
        }
    }

    public async Task ClearTokensAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync(AccessTokenKey);
            await _localStorage.RemoveItemAsync(RefreshTokenKey);
            
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            _logger.LogInformation("All tokens cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing tokens");
        }
    }

    public async Task<string?> GetStoredTokenAsync()
    {
        try
        {
            return await _localStorage.GetItemAsStringAsync(AccessTokenKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stored token");
            return null;
        }
    }

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        try
        {
            await _localStorage.SetItemAsStringAsync(AccessTokenKey, accessToken);
            await _localStorage.SetItemAsStringAsync(RefreshTokenKey, refreshToken);
            
            // Не обновляем здесь DefaultRequestHeaders, так как это делает интерцептор
            // _httpClient.DefaultRequestHeaders.Authorization = 
            //     new AuthenticationHeaderValue("Bearer", accessToken);
            
            _logger.LogDebug("Tokens saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tokens");
        }
    }

    public async Task<bool> HasValidRefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsStringAsync(RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            // В реальном приложении здесь можно добавить проверку времени истечения refresh токена
            // Для простоты пока возвращаем true, если токен существует
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking refresh token validity");
            return false;
        }
    }

    /// <summary>
    /// Извлекает время истечения из JWT токена
    /// </summary>
    private DateTimeOffset? GetTokenExpirationTime(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var payloadJson = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (payloadJson?.TryGetValue("exp", out var expValue) == true)
            {
                if (long.TryParse(expValue.ToString(), out var expUnix))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(expUnix);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing token expiration time");
            return null;
        }
    }

    /// <summary>
    /// Парсит Base64 строку без padding
    /// </summary>
    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    public void Dispose()
    {
        _refreshTokenSemaphore?.Dispose();
    }
}
