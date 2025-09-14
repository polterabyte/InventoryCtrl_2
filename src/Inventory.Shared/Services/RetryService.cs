using Inventory.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public interface IRetryService
{
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3);
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, RetryPolicy policy);
}

public class RetryService(ILogger<RetryService> logger) : IRetryService
{
    private readonly ILogger<RetryService> _logger = logger;

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3)
    {
        var policy = new RetryPolicy(maxRetries, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        return await ExecuteWithRetryAsync(operation, operationName, policy);
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, RetryPolicy policy)
    {
        var attempt = 0;
        var delay = policy.BaseDelay;

        while (attempt < policy.MaxRetries)
        {
            try
            {
                _logger.LogDebug("Executing {OperationName}, attempt {Attempt}", operationName, attempt + 1);
                return await operation();
            }
            catch (Exception ex) when (attempt < policy.MaxRetries - 1)
            {
                attempt++;
                _logger.LogWarning(ex, "Operation {OperationName} failed on attempt {Attempt}, retrying in {Delay}ms", 
                    operationName, attempt, delay.TotalMilliseconds);

                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * policy.BackoffMultiplier, policy.MaxDelay.TotalMilliseconds));
            }
        }

        _logger.LogError("Operation {OperationName} failed after {MaxRetries} attempts", operationName, policy.MaxRetries);
        throw new InvalidOperationException($"Operation {operationName} failed after {policy.MaxRetries} attempts");
    }
}