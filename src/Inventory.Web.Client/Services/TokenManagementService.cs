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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Сервис для управления JWT токенами
/// </summary>
public class TokenManagementService : ITokenManagementService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly TokenConfiguration _config;
    private readonly ILogger<TokenManagementService> _logger;
    
    // Семафор для предотвращения дублирующих запросов обновления
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
    private volatile bool _isRefreshing = false;

    private const string AccessTokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";

    public TokenManagementService(
        ILocalStorageService localStorage,
        IAuthService authService,
        HttpClient httpClient,
        IOptions<TokenConfiguration> config,
        ILogger<TokenManagementService> logger)
    {
        _localStorage = localStorage;
        _authService = authService;
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
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
        // Проверяем, не идет ли уже обновление
        if (_isRefreshing)
        {
            _logger.LogDebug("Token refresh already in progress, waiting...");
            await _refreshSemaphore.WaitAsync();
            _refreshSemaphore.Release();
            return true; // Предполагаем успех, так как другой поток уже обновляет
        }

        await _refreshSemaphore.WaitAsync();
        try
        {
            if (_isRefreshing)
            {
                _logger.LogDebug("Token refresh already completed by another thread");
                return true;
            }

            _isRefreshing = true;
            _logger.LogInformation("Starting token refresh process");

            var refreshToken = await _localStorage.GetItemAsStringAsync(RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("No refresh token available");
                return false;
            }

            // Проверяем, не истек ли refresh токен
            if (!await HasValidRefreshTokenAsync())
            {
                _logger.LogWarning("Refresh token has expired");
                await ClearTokensAsync();
                return false;
            }

            // Выполняем обновление с повторными попытками
            for (int attempt = 1; attempt <= _config.MaxRefreshRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("Token refresh attempt {Attempt}/{MaxRetries}", attempt, _config.MaxRefreshRetries);

                    var result = await _authService.RefreshTokenAsync(refreshToken);
                    
                    if (result.Success && !string.IsNullOrEmpty(result.Token))
                    {
                        await SaveTokensAsync(result.Token, result.RefreshToken ?? refreshToken);
                        
                        // Обновляем заголовок авторизации
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new AuthenticationHeaderValue("Bearer", result.Token);

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

            _logger.LogError("Token refresh failed after {MaxRetries} attempts", _config.MaxRefreshRetries);
            await ClearTokensAsync();
            return false;
        }
        finally
        {
            _isRefreshing = false;
            _refreshSemaphore.Release();
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
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);
            
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
        _refreshSemaphore?.Dispose();
    }
}
