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
            _logger.LogDebug("Returning cached SignalR URL: {SignalRUrl}", _cachedSignalRUrl);
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

        // 3. Fallback: построить URL из BaseUrl или относительный '/api'
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
            _logger.LogDebug("Using configured SignalR URL: {SignalRUrl}", envConfig.SignalRUrl);
            return envConfig.SignalRUrl;
        }

        // 2. Fallback: построить на основе API URL
        var apiUrl = await GetApiBaseUrlAsync();
        
        // Убедимся, что API URL не содержит /notificationHub
        if (apiUrl.Contains("/notificationHub"))
        {
            apiUrl = apiUrl.Replace("/notificationHub", "/api");
            _logger.LogDebug("Removed /notificationHub from API URL: {ApiUrl}", apiUrl);
        }
        
        var signalRUrl = apiUrl.Replace("/api", "/notificationHub");
        
        _logger.LogDebug("Constructed SignalR URL from API URL: {SignalRUrl}", signalRUrl);
        return signalRUrl;
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
            if (string.IsNullOrEmpty(origin) || origin.StartsWith("file", StringComparison.OrdinalIgnoreCase))
            {
                // Fallback to localhost when running from file:// or origin unavailable
                var defaultOrigin = $"http://localhost:{_apiConfig.Fallback.DefaultPorts.Http}";
                var fallback = $"{defaultOrigin}/api";
                _logger.LogWarning("Origin unavailable or file protocol detected. Using fallback dynamic API URL: {ApiUrl}", fallback);
                return fallback;
            }


var port = await GetCurrentPortAsync();
var originUri = new Uri(origin);
var apiPort = DetermineApiPort(port, originUri.Scheme, originUri.Port);

_logger.LogDebug("Dynamic URL construction - Origin: {Origin}, ClientPort: {ClientPort}, ApiPort: {ApiPort}",
    origin, port ?? "null", apiPort);

var builder = new UriBuilder(originUri)
{
    Port = apiPort
};

var authority = builder.Uri.GetLeftPart(UriPartial.Authority);
var dynamicUrl = $"{authority}/api";
_logger.LogDebug("Constructed dynamic URL: {DynamicUrl}", dynamicUrl);

return dynamicUrl;
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


private int DetermineApiPort(string? clientPort, string scheme, int originPort)
{
    if (int.TryParse(clientPort, out var parsedPort) && parsedPort > 0)
    {
        return MapClientPort(parsedPort);
    }

    if (originPort > 0 && originPort != 80 && originPort != 443)
    {
        return originPort;
    }

    var isHttps = scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
    var fallbackPort = isHttps ? _apiConfig.Fallback.DefaultPorts.Https : _apiConfig.Fallback.DefaultPorts.Http;
    return fallbackPort > 0 ? fallbackPort : (isHttps ? 443 : 80);
}

private int MapClientPort(int clientPort)
{
    return clientPort switch
    {
        5001 or 7001 => _apiConfig.Fallback.DefaultPorts.Https,
        5000 => _apiConfig.Fallback.DefaultPorts.Http,
        80 => _apiConfig.Fallback.DefaultPorts.Http > 0 ? _apiConfig.Fallback.DefaultPorts.Http : 80,
        443 => _apiConfig.Fallback.DefaultPorts.Https > 0 ? _apiConfig.Fallback.DefaultPorts.Https : 443,
        _ => clientPort
    };
}

    private string EnsureAbsoluteUrl(string url)
    {
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return url;
        }

        // Если URL относительный, пытаемся построить абсолютный
        if (url.StartsWith("/"))
        {
            // Для относительных путей возвращаем как есть.
            // Абсолютизация выполняется в UrlBuilderService или на стороне хостинга.
            return url;
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

        // Если URL относительный, пытаемся построить абсолютный
        if (url.StartsWith("/"))
        {
            try
            {
                var origin = await GetCurrentOriginAsync();
                if (!string.IsNullOrEmpty(origin) && !origin.StartsWith("file", StringComparison.OrdinalIgnoreCase))
                {
                    return origin.TrimEnd('/') + url;
                }
                
                // Fallback for file:// or missing origin
                var defaultOrigin = $"http://localhost:{_apiConfig.Fallback.DefaultPorts.Http}";
                return defaultOrigin.TrimEnd('/') + url;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get current origin for URL construction");
                var defaultOrigin = $"http://localhost:{_apiConfig.Fallback.DefaultPorts.Http}";
                return defaultOrigin.TrimEnd('/') + url;
            }
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
