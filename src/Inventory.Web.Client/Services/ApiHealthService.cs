using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Inventory.Web.Client.Configuration;

namespace Inventory.Web.Client.Services;

public interface IApiHealthService
{
    Task<bool> IsApiAvailableAsync();
    Task<bool> IsApiAvailableAsync(string apiUrl);
    Task<HealthCheckResult> GetHealthStatusAsync();
}

public class ApiHealthService : IApiHealthService
{
    private readonly HttpClient _httpClient;
    private readonly IApiUrlService _apiUrlService;
    private readonly ApiConfiguration _apiConfig;
    private readonly ILogger<ApiHealthService> _logger;
    private readonly Microsoft.JSInterop.IJSRuntime _jsRuntime;

    public ApiHealthService(
        HttpClient httpClient,
        IApiUrlService apiUrlService,
        IOptions<ApiConfiguration> apiConfig,
        ILogger<ApiHealthService> logger,
        Microsoft.JSInterop.IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _apiUrlService = apiUrlService;
        _apiConfig = apiConfig.Value;
        _logger = logger;
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            var apiUrl = await _apiUrlService.GetApiBaseUrlAsync();
            return await IsApiAvailableAsync(apiUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check API availability");
            return false;
        }
    }

    public async Task<bool> IsApiAvailableAsync(string apiUrl)
    {
        try
        {
            var healthUrl = await GetHealthCheckUrlAsync(apiUrl);
            _logger.LogDebug("Checking API health at: {HealthUrl}", healthUrl);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync(healthUrl, cts.Token);
            
            var isHealthy = response.IsSuccessStatusCode;
            _logger.LogDebug("API health check result: {IsHealthy} (Status: {StatusCode})", 
                isHealthy, response.StatusCode);
            
            return isHealthy;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("API health check timed out for: {ApiUrl}", apiUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API health check failed for: {ApiUrl}", apiUrl);
            return false;
        }
    }

    public async Task<HealthCheckResult> GetHealthStatusAsync()
    {
        try
        {
            var apiUrl = await _apiUrlService.GetApiBaseUrlAsync();
            var healthUrl = await GetHealthCheckUrlAsync(apiUrl);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync(healthUrl, cts.Token);
            
            var content = response.IsSuccessStatusCode 
                ? await response.Content.ReadAsStringAsync()
                : string.Empty;

            return new HealthCheckResult
            {
                IsHealthy = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                ResponseTime = DateTime.UtcNow, // В реальном приложении можно измерить время ответа
                Message = content,
                ApiUrl = apiUrl,
                HealthUrl = healthUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
            return new HealthCheckResult
            {
                IsHealthy = false,
                StatusCode = System.Net.HttpStatusCode.ServiceUnavailable,
                Message = ex.Message,
                ApiUrl = string.Empty,
                HealthUrl = string.Empty
            };
        }
    }

    private async Task<string> GetHealthCheckUrlAsync(string apiUrl)
    {
        // Пытаемся получить URL для health check из конфигурации
        var envName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        if (_apiConfig.Environments.TryGetValue(envName, out var envConfig) && 
            !string.IsNullOrEmpty(envConfig.HealthCheckUrl))
        {
            return envConfig.HealthCheckUrl;
        }

        // Fallback: добавляем /health к базовому URL API
        var healthUrl = apiUrl.TrimEnd('/') + "/health";
        
        // Если URL относительный, делаем его абсолютным для текущего origin
        if (!healthUrl.StartsWith("http://") && !healthUrl.StartsWith("https://"))
        {
            try
            {
                var origin = await GetCurrentOriginAsync();
                if (!string.IsNullOrEmpty(origin))
                {
                    return origin.TrimEnd('/') + healthUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get current origin for health check URL");
            }
            
            // Если не удалось получить origin, используем конфигурацию по умолчанию
            _logger.LogWarning("Using fallback health check URL: {HealthUrl}. This may cause issues if the URL is relative.", healthUrl);
        }
        
        return healthUrl;
    }
    
    private async Task<string?> GetCurrentOriginAsync()
    {
        try
        {
            // В Blazor WebAssembly мы можем получить origin через JavaScript
            return await _jsRuntime.InvokeAsync<string>("eval", new object[] { "window.location.origin" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get current origin from JavaScript");
            return null;
        }
    }
}

public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public System.Net.HttpStatusCode StatusCode { get; set; }
    public DateTime ResponseTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string HealthUrl { get; set; } = string.Empty;
}
