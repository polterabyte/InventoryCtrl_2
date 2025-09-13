using Inventory.Shared.Models;
using Inventory.Shared.Services;

namespace Inventory.Shared.Services;

public interface IRetryService
{
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3);
    Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, int maxRetries = 3);
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, RetryPolicy policy);
    Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, RetryPolicy policy);
}

public class RetryService : IRetryService
{
    private readonly INotificationService _notificationService;
    private readonly ILoggingService _loggingService;

    public RetryService(INotificationService notificationService, ILoggingService loggingService)
    {
        _notificationService = notificationService;
        _loggingService = loggingService;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3)
    {
        var policy = new RetryPolicy
        {
            MaxRetries = maxRetries,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
            BackoffMultiplier = 2
        };

        return await ExecuteWithRetryAsync(operation, operationName, policy);
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, int maxRetries = 3)
    {
        var policy = new RetryPolicy
        {
            MaxRetries = maxRetries,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
            BackoffMultiplier = 2
        };

        await ExecuteWithRetryAsync(operation, operationName, policy);
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, RetryPolicy policy)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= policy.MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    await _loggingService.LogInformationAsync($"Retrying {operationName}, attempt {attempt + 1}/{policy.MaxRetries + 1}");
                }

                var result = await operation();
                
                if (attempt > 0)
                {
                    await _loggingService.LogInformationAsync($"Operation {operationName} succeeded after {attempt + 1} attempts");
                    _notificationService.ShowSuccess("Operation Successful", $"{operationName} completed successfully after retry");
                }

                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await _loggingService.LogErrorAsync($"Operation {operationName} failed on attempt {attempt + 1}: {ex.Message}");

                if (attempt == policy.MaxRetries)
                {
                    await _loggingService.LogErrorAsync($"Operation {operationName} failed after {policy.MaxRetries + 1} attempts");
                    _notificationService.ShowError(
                        "Operation Failed", 
                        $"{operationName} failed after {policy.MaxRetries + 1} attempts. {ex.Message}",
                        () => _ = ExecuteWithRetryAsync(operation, operationName, policy),
                        "Retry Again"
                    );
                    throw;
                }

                var delay = CalculateDelay(attempt, policy);
                await Task.Delay(delay);
            }
        }

        throw lastException ?? new InvalidOperationException($"Operation {operationName} failed unexpectedly");
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, RetryPolicy policy)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, operationName, policy);
    }

    private TimeSpan CalculateDelay(int attempt, RetryPolicy policy)
    {
        var delay = TimeSpan.FromMilliseconds(
            policy.BaseDelay.TotalMilliseconds * Math.Pow(policy.BackoffMultiplier, attempt)
        );

        return delay > policy.MaxDelay ? policy.MaxDelay : delay;
    }
}

public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(10);
    public double BackoffMultiplier { get; set; } = 2;
    public bool ExponentialBackoff { get; set; } = true;
}
