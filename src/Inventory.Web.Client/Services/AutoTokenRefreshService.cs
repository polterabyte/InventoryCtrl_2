using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Реализация сервиса автоматического обновления токена
/// </summary>
public class AutoTokenRefreshService : IAutoTokenRefreshService
{
    private readonly ILogger<AutoTokenRefreshService> _logger;
    private readonly ITokenManagementService _tokenManagementService;
    private const int MaxRetries = 1; // Максимальное количество попыток обновления токена

    public AutoTokenRefreshService(
        ILogger<AutoTokenRefreshService> logger,
        ITokenManagementService tokenManagementService)
    {
        _logger = logger;
        _tokenManagementService = tokenManagementService;
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<T>> ExecuteWithAutoRefreshAsync<T>(Func<Task<ApiResponse<T>>> apiCall)
    {
        var retryCount = 0;
        ApiResponse<T>? lastResponse = null;

        while (retryCount <= MaxRetries)
        {
            lastResponse = await apiCall();

            // Если запрос успешен или это не связано с обновлением токена - возвращаем результат
            if (lastResponse.Success || lastResponse.ErrorMessage != ApiResponseCodes.TokenRefreshed)
            {
                return lastResponse;
            }

            // Пробуем обновить токен и повторить запрос
            _logger.LogInformation("Token refresh required, attempt {Attempt} of {MaxRetries}", 
                retryCount + 1, MaxRetries);

            var refreshResult = await _tokenManagementService.TryRefreshTokenAsync();
            if (!refreshResult)
            {
                _logger.LogWarning("Token refresh failed");
                return lastResponse;
            }

            retryCount++;
        }

        return lastResponse ?? ApiResponse<T>.CreateFailure("Maximum retry attempts exceeded");
    }

    /// <inheritdoc/>
    public async Task<PagedApiResponse<T>> ExecutePagedWithAutoRefreshAsync<T>(Func<Task<PagedApiResponse<T>>> apiCall)
    {
        var retryCount = 0;
        PagedApiResponse<T>? lastResponse = null;

        while (retryCount <= MaxRetries)
        {
            lastResponse = await apiCall();

            // Если запрос успешен или это не связано с обновлением токена - возвращаем результат
            if (lastResponse.Success || lastResponse.ErrorMessage != ApiResponseCodes.TokenRefreshed)
            {
                return lastResponse;
            }

            // Пробуем обновить токен и повторить запрос
            _logger.LogInformation("Token refresh required for paged request, attempt {Attempt} of {MaxRetries}", 
                retryCount + 1, MaxRetries);

            var refreshResult = await _tokenManagementService.TryRefreshTokenAsync();
            if (!refreshResult)
            {
                _logger.LogWarning("Token refresh failed");
                return lastResponse;
            }

            retryCount++;
        }

        return lastResponse ?? new PagedApiResponse<T> 
        { 
            Success = false, 
            ErrorMessage = "Maximum retry attempts exceeded" 
        };
    }
}