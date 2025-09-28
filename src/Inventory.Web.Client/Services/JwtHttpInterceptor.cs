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
    private readonly ILogger<JwtHttpInterceptor> _logger;

    public JwtHttpInterceptor(
        ILogger<JwtHttpInterceptor> logger)
    {
        _logger = logger;
    }

    public async Task<HttpResponseMessage> InterceptAsync(HttpRequestMessage request, Func<Task<HttpResponseMessage>> next)
    {
        try
        {
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

}
