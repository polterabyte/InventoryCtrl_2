using Microsoft.Extensions.Logging;
using Inventory.Shared.Services;

namespace Inventory.API.Services;

public class ErrorHandlingService : IErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService> _logger;

    public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
    {
        _logger = logger;
    }

    public Task HandleErrorAsync(Exception exception, string? context = null, object? additionalData = null)
    {
        _logger.LogError(exception, "Error in {Context}: {Message}", context ?? "Unknown", exception.Message);

        // In API context, we don't show UI notifications - errors are handled via HTTP responses
        return Task.CompletedTask;
    }

    public async Task<bool> TryExecuteAsync<T>(Func<Task<T>> operation, string operationName, object? contextData = null)
    {
        try
        {
            await operation();
            return true;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, operationName, contextData);
            return false;
        }
    }

    public async Task<bool> TryExecuteAsync(Func<Task> operation, string operationName, object? contextData = null)
    {
        try
        {
            await operation();
            return true;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, operationName, contextData);
            return false;
        }
    }

    public async Task<T?> TryExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, object? contextData = null, int maxRetries = 3)
    {
        // For API, we don't implement retry logic here - it's handled at the service level
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, operationName, contextData);
            return default;
        }
    }

    public async Task<bool> TryExecuteWithRetryAsync(Func<Task> operation, string operationName, object? contextData = null, int maxRetries = 3)
    {
        try
        {
            await operation();
            return true;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, operationName, contextData);
            return false;
        }
    }

    public async Task HandleApiErrorAsync(HttpResponseMessage response, string operationName)
    {
        var errorMessage = await response.Content.ReadAsStringAsync();

        _logger.LogError("API error in {Operation}: {StatusCode} - {ErrorMessage}",
            operationName, response.StatusCode, errorMessage);
    }
}