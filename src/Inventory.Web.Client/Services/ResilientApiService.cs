using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Inventory.Web.Client.Configuration;

namespace Inventory.Web.Client.Services;

public interface IResilientApiService
{
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName = "API Operation");
    Task ExecuteWithRetryAsync(Func<Task> operation, string operationName = "API Operation");
}

public class ResilientApiService : IResilientApiService
{
    private readonly IApiHealthService _healthService;
    private readonly ApiConfiguration _apiConfig;
    private readonly ILogger<ResilientApiService> _logger;

    public ResilientApiService(
        IApiHealthService healthService,
        IOptions<ApiConfiguration> apiConfig,
        ILogger<ResilientApiService> logger)
    {
        _healthService = healthService;
        _apiConfig = apiConfig.Value;
        _logger = logger;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName = "API Operation")
    {
        var retryConfig = _apiConfig.Fallback.RetrySettings;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= retryConfig.MaxRetries; attempt++)
        {
            try
            {
                // Проверяем доступность API перед выполнением (только для первых попыток)
                if (attempt == 1)
                {
                    var isApiAvailable = await _healthService.IsApiAvailableAsync();
                    if (!isApiAvailable)
                    {
                        _logger.LogWarning("API is not available, skipping operation: {OperationName}", operationName);
                        throw new InvalidOperationException("API is not available");
                    }
                }

                _logger.LogDebug("Executing {OperationName}, attempt {Attempt}/{MaxRetries}", 
                    operationName, attempt, retryConfig.MaxRetries);

                var result = await operation();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("Operation {OperationName} succeeded on attempt {Attempt}", 
                        operationName, attempt);
                }

                return result;
            }
            catch (Exception ex) when (attempt < retryConfig.MaxRetries)
            {
                lastException = ex;
                var delay = CalculateDelay(attempt, retryConfig);
                
                _logger.LogWarning(ex, 
                    "Operation {OperationName} failed on attempt {Attempt}/{MaxRetries}, retrying in {Delay}ms", 
                    operationName, attempt, retryConfig.MaxRetries, delay);

                await Task.Delay(delay);
            }
        }

        _logger.LogError(lastException, 
            "Operation {OperationName} failed after {MaxRetries} attempts", 
            operationName, retryConfig.MaxRetries);

        throw new InvalidOperationException(
            $"Operation '{operationName}' failed after {retryConfig.MaxRetries} attempts", 
            lastException);
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName = "API Operation")
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true; // Возвращаем dummy значение для void операций
        }, operationName);
    }

    private int CalculateDelay(int attempt, RetryConfig retryConfig)
    {
        // Экспоненциальная задержка с jitter
        var baseDelay = retryConfig.BaseDelayMs * Math.Pow(2, attempt - 1);
        var maxDelay = retryConfig.MaxDelayMs;
        
        // Добавляем случайный jitter (±25%)
        var jitter = Random.Shared.NextDouble() * 0.5 - 0.25; // -0.25 to +0.25
        var delayWithJitter = baseDelay * (1 + jitter);
        
        return Math.Min((int)delayWithJitter, maxDelay);
    }
}

// Расширения для удобного использования
public static class ResilientApiServiceExtensions
{
    public static async Task<T> GetWithRetryAsync<T>(
        this IResilientApiService resilientService,
        Func<Task<T>> getOperation,
        string endpoint = "GET")
    {
        return await resilientService.ExecuteWithRetryAsync(getOperation, $"GET {endpoint}");
    }

    public static async Task<T> PostWithRetryAsync<T>(
        this IResilientApiService resilientService,
        Func<Task<T>> postOperation,
        string endpoint = "POST")
    {
        return await resilientService.ExecuteWithRetryAsync(postOperation, $"POST {endpoint}");
    }

    public static async Task<T> PutWithRetryAsync<T>(
        this IResilientApiService resilientService,
        Func<Task<T>> putOperation,
        string endpoint = "PUT")
    {
        return await resilientService.ExecuteWithRetryAsync(putOperation, $"PUT {endpoint}");
    }

    public static async Task DeleteWithRetryAsync(
        this IResilientApiService resilientService,
        Func<Task> deleteOperation,
        string endpoint = "DELETE")
    {
        await resilientService.ExecuteWithRetryAsync(deleteOperation, $"DELETE {endpoint}");
    }
}
