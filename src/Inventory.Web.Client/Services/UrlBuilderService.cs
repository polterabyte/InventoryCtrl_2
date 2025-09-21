using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Реализация сервиса для построения и валидации API URL
/// </summary>
public class UrlBuilderService : IUrlBuilderService
{
    private readonly IApiUrlService _apiUrlService;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<UrlBuilderService> _logger;

    public UrlBuilderService(
        IApiUrlService apiUrlService,
        IJSRuntime jsRuntime,
        ILogger<UrlBuilderService> logger)
    {
        _apiUrlService = apiUrlService;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<string> BuildApiUrlAsync(string endpoint)
    {
        var apiUrl = await _apiUrlService.GetApiBaseUrlAsync();
        _logger.LogDebug("Using API URL: {ApiUrl}", apiUrl);
        // endpoint intentionally ignored here; applied in BuildFullUrlAsync
        return apiUrl;
    }

    public async Task<string> BuildFullUrlAsync(string endpoint)
    {
        var apiUrl = await BuildApiUrlAsync(endpoint);
        var fullUrl = $"{apiUrl.TrimEnd('/')}{endpoint}";
        
        return await ValidateAndFixUrlAsync(fullUrl);
    }

    public async Task<string> ValidateAndFixUrlAsync(string url)
    {
        // Если URL уже абсолютный и валидный, возвращаем как есть
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return url;
        }

        // Если URL относительный, делаем его абсолютным
        if (url.StartsWith("/"))
        {
            try
            {
                // Получаем текущий origin
                var origin = await _jsRuntime.InvokeAsync<string>("eval", "window.location.origin");
                var absoluteUrl = $"{origin.TrimEnd('/')}{url}";
                
                if (Uri.IsWellFormedUriString(absoluteUrl, UriKind.Absolute))
                {
                    _logger.LogDebug("Fixed relative URL: {OriginalUrl} -> {FixedUrl}", url, absoluteUrl);
                    return absoluteUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get window.location.origin, using fallback");
            }

            // Если не удалось получить origin, возвращаем относительный URL без подстановки доменов
            _logger.LogDebug("Origin unavailable, returning relative URL: {Url}", url);
            return url;
        }

        // Если URL невалидный и не относительный, выбрасываем исключение
        throw new InvalidOperationException($"Invalid URL format: {url}");
    }
}
