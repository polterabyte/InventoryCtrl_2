using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

/// <summary>
/// HTTP Interceptor для автоматической проверки и обновления JWT токенов
/// </summary>
public class JwtHttpInterceptor : IHttpInterceptor
{
    private readonly ITokenManagementService _tokenManagementService;
    private readonly ILogger<JwtHttpInterceptor> _logger;

    public JwtHttpInterceptor(
        ITokenManagementService tokenManagementService,
        ILogger<JwtHttpInterceptor> logger)
    {
        _tokenManagementService = tokenManagementService;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> InterceptAsync(HttpRequestMessage request, Func<Task<HttpResponseMessage>> next)
    {
        try
        {
            // Проверяем, нужно ли обновить токен перед запросом
            if (await ShouldRefreshTokenBeforeRequestAsync(request))
            {
                _logger.LogDebug("Token is expiring soon, attempting refresh before request");
                
                var refreshSuccess = await _tokenManagementService.TryRefreshTokenAsync();
                if (!refreshSuccess)
                {
                    _logger.LogWarning("Failed to refresh token before request");
                    // Продолжаем выполнение запроса, обработка 401 произойдет в ApiErrorHandler
                }
            }

            // Выполняем оригинальный запрос
            var response = await next();
            
            _logger.LogDebug("HTTP request completed with status {StatusCode}", response.StatusCode);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in JWT HTTP interceptor");
            throw;
        }
    }

    /// <summary>
    /// Определяет, нужно ли обновить токен перед выполнением запроса
    /// </summary>
    private async Task<bool> ShouldRefreshTokenBeforeRequestAsync(HttpRequestMessage request)
    {
        try
        {
            // Пропускаем проверку для публичных эндпоинтов
            if (IsPublicEndpoint(request.RequestUri?.AbsolutePath))
            {
                return false;
            }

            // Проверяем, есть ли токен в заголовке авторизации
            if (!HasAuthorizationHeader(request))
            {
                return false;
            }

            // Проверяем, истекает ли токен в ближайшее время
            return await _tokenManagementService.IsTokenExpiringSoonAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token should be refreshed");
            return false; // В случае ошибки не обновляем токен
        }
    }

    /// <summary>
    /// Проверяет, является ли эндпоинт публичным
    /// </summary>
    private static bool IsPublicEndpoint(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var publicPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/health",
            "/swagger",
            "/notificationHub"
        };

        return Array.Exists(publicPaths, publicPath => path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Проверяет, есть ли заголовок авторизации в запросе
    /// </summary>
    private static bool HasAuthorizationHeader(HttpRequestMessage request)
    {
        return request.Headers.Authorization != null && 
               request.Headers.Authorization.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase);
    }
}
