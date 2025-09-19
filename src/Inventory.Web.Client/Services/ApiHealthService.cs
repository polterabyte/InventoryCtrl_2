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

    public ApiHealthService(
        HttpClient httpClient,
        IApiUrlService apiUrlService,
        IOptions<ApiConfiguration> apiConfig,
        ILogger<ApiHealthService> logger)
    {
        _httpClient = httpClient;
        _apiUrlService = apiUrlService;
        _apiConfig = apiConfig.Value;
        _logger = logger;
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
            var healthUrl = GetHealthCheckUrl(apiUrl);
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
            var healthUrl = GetHealthCheckUrl(apiUrl);
            
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

    private string GetHealthCheckUrl(string apiUrl)
    {
        // Пытаемся получить URL для health check из конфигурации
        var envName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        if (_apiConfig.Environments.TryGetValue(envName, out var envConfig) && 
            !string.IsNullOrEmpty(envConfig.HealthCheckUrl))
        {
            return envConfig.HealthCheckUrl;
        }

        // Fallback: добавляем /health к базовому URL API
        return apiUrl.TrimEnd('/') + "/health";
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
