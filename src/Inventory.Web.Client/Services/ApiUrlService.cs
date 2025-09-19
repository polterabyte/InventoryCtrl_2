using Inventory.Web.Client.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

public interface IApiUrlService
{
    Task<string> GetApiBaseUrlAsync();
    Task<string> GetApiUrlAsync(string endpoint);
    Task<string> GetSignalRUrlAsync();
    Task<ApiUrlInfo> GetApiUrlInfoAsync();
}

public class ApiUrlService : IApiUrlService
{
    private readonly ApiConfiguration _apiConfig;
    private readonly IWebAssemblyHostEnvironment _environment;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ApiUrlService> _logger;
    private string? _cachedApiUrl;
    private string? _cachedSignalRUrl;

    public ApiUrlService(
        IOptions<ApiConfiguration> apiConfig,
        IWebAssemblyHostEnvironment environment,
        IJSRuntime jsRuntime,
        ILogger<ApiUrlService> logger)
    {
        _apiConfig = apiConfig.Value;
        _environment = environment;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<string> GetApiBaseUrlAsync()
    {
        if (_cachedApiUrl != null)
        {
            return _cachedApiUrl;
        }

        _cachedApiUrl = await DetermineApiUrlAsync();
        _logger.LogDebug("Determined API URL: {ApiUrl}", _cachedApiUrl);
        
        return _cachedApiUrl;
    }

    public async Task<string> GetApiUrlAsync(string endpoint)
    {
        var baseUrl = await GetApiBaseUrlAsync();
        return $"{baseUrl.TrimEnd('/')}{endpoint}";
    }

    public async Task<string> GetSignalRUrlAsync()
    {
        if (_cachedSignalRUrl != null)
        {
            return _cachedSignalRUrl;
        }

        _cachedSignalRUrl = await DetermineSignalRUrlAsync();
        _logger.LogDebug("Determined SignalR URL: {SignalRUrl}", _cachedSignalRUrl);
        
        return _cachedSignalRUrl;
    }

    public async Task<ApiUrlInfo> GetApiUrlInfoAsync()
    {
        var apiUrl = await GetApiBaseUrlAsync();
        var signalRUrl = await GetSignalRUrlAsync();
        
        return new ApiUrlInfo
        {
            ApiUrl = apiUrl,
            SignalRUrl = signalRUrl,
            Environment = _environment.Environment,
            IsDevelopment = _environment.IsDevelopment(),
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<string> DetermineApiUrlAsync()
    {
        // 1. Попробовать получить из конфигурации по окружению
        var envConfig = GetEnvironmentConfig();
        if (!string.IsNullOrEmpty(envConfig?.ApiUrl))
        {
            _logger.LogDebug("Using configured API URL: {ApiUrl}", envConfig.ApiUrl);
            return EnsureAbsoluteUrl(envConfig.ApiUrl);
        }

        // 2. Fallback: динамическое определение для development
        if (_environment.IsDevelopment() && _apiConfig.Fallback.UseDynamicDetection)
        {
            var dynamicUrl = await GetDynamicUrlAsync();
            if (!string.IsNullOrEmpty(dynamicUrl))
            {
                _logger.LogDebug("Using dynamic API URL: {ApiUrl}", dynamicUrl);
                return EnsureAbsoluteUrl(dynamicUrl);
            }
        }

        // 3. Fallback: попробовать построить абсолютный URL
        var fallbackUrl = _apiConfig.BaseUrl ?? "/api";
        var absoluteUrl = await EnsureAbsoluteUrlAsync(fallbackUrl);
        
        _logger.LogWarning("Using fallback API URL: {ApiUrl}", absoluteUrl);
        return absoluteUrl;
    }

    private async Task<string> DetermineSignalRUrlAsync()
    {
        // 1. Попробовать получить из конфигурации по окружению
        var envConfig = GetEnvironmentConfig();
        if (!string.IsNullOrEmpty(envConfig?.SignalRUrl))
        {
            return envConfig.SignalRUrl;
        }

        // 2. Fallback: построить на основе API URL
        var apiUrl = await GetApiBaseUrlAsync();
        return apiUrl.Replace("/api", "/notificationHub");
    }

    private EnvironmentConfig? GetEnvironmentConfig()
    {
        var envName = _environment.Environment;
        _logger.LogDebug("Current environment: {Environment}", envName);
        
        if (_apiConfig.Environments.TryGetValue(envName, out var config))
        {
            _logger.LogDebug("Found environment config for: {Environment}", envName);
            return config;
        }
        
        // Fallback: попробовать Development если текущее окружение не найдено
        if (envName != "Development" && _apiConfig.Environments.TryGetValue("Development", out var devConfig))
        {
            _logger.LogWarning("Environment {Environment} not found, falling back to Development config", envName);
            return devConfig;
        }
        
        _logger.LogWarning("No environment config found for: {Environment}", envName);
        return null;
    }

    private async Task<string?> GetDynamicUrlAsync()
    {
        try
        {
            var origin = await GetCurrentOriginAsync();
            if (string.IsNullOrEmpty(origin))
            {
                return null;
            }

            var port = await GetCurrentPortAsync();
            var apiPort = DetermineApiPort(port);
            
            return $"{origin.Replace($":{port}", $":{apiPort}")}/api";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dynamic URL");
            return null;
        }
    }

    private async Task<string?> GetCurrentOriginAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("eval", new object[] { "window.location.origin" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current origin from JavaScript");
            return null;
        }
    }

    private async Task<string?> GetCurrentPortAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("eval", new object[] { "window.location.port" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current port from JavaScript");
            return null;
        }
    }

    private string DetermineApiPort(string? clientPort)
    {
        return clientPort switch
        {
            "5001" => _apiConfig.Fallback.DefaultPorts.Https.ToString(), // HTTPS development
            "5000" => _apiConfig.Fallback.DefaultPorts.Http.ToString(),  // HTTP development
            _ => _apiConfig.Fallback.DefaultPorts.Https.ToString()       // Default to HTTPS
        };
    }

    private string EnsureAbsoluteUrl(string url)
    {
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return url;
        }

        // Если URL уже абсолютный, возвращаем как есть
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            return url;
        }

        // Если URL относительный, пытаемся построить абсолютный
        if (url.StartsWith("/"))
        {
            // В Blazor WebAssembly мы не можем использовать HttpClient.BaseAddress
            // Поэтому используем текущий origin
            return url; // Вернем относительный URL, он будет обработан в WebBaseApiService
        }

        // Если URL не начинается с /, добавляем его
        return "/" + url.TrimStart('/');
    }

    private async Task<string> EnsureAbsoluteUrlAsync(string url)
    {
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return url;
        }

        // Если URL уже абсолютный, возвращаем как есть
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            return url;
        }

        // Если URL относительный, пытаемся построить абсолютный
        if (url.StartsWith("/"))
        {
            try
            {
                var origin = await GetCurrentOriginAsync();
                if (!string.IsNullOrEmpty(origin))
                {
                    return origin.TrimEnd('/') + url;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get current origin for URL construction");
            }
            
            // Если не удалось получить origin, возвращаем относительный URL
            return url;
        }

        // Если URL не начинается с /, добавляем его
        return "/" + url.TrimStart('/');
    }
}

public class ApiUrlInfo
{
    public string ApiUrl { get; set; } = string.Empty;
    public string SignalRUrl { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsDevelopment { get; set; }
    public DateTime Timestamp { get; set; }
}
