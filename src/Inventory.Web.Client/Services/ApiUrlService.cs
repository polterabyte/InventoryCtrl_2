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
            return envConfig.ApiUrl;
        }

        // 2. Fallback: динамическое определение для development
        if (_environment.IsDevelopment() && _apiConfig.Fallback.UseDynamicDetection)
        {
            var dynamicUrl = await GetDynamicUrlAsync();
            if (!string.IsNullOrEmpty(dynamicUrl))
            {
                _logger.LogDebug("Using dynamic API URL: {ApiUrl}", dynamicUrl);
                return dynamicUrl;
            }
        }

        // 3. Fallback: относительный путь
        _logger.LogWarning("Using fallback relative API URL");
        return _apiConfig.BaseUrl ?? "/api";
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
        if (_apiConfig.Environments.TryGetValue(envName, out var config))
        {
            return config;
        }
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
            return await _jsRuntime.InvokeAsync<string>("eval", "window.location.origin");
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
            return await _jsRuntime.InvokeAsync<string>("eval", "window.location.port");
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
}

public class ApiUrlInfo
{
    public string ApiUrl { get; set; } = string.Empty;
    public string SignalRUrl { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsDevelopment { get; set; }
    public DateTime Timestamp { get; set; }
}
