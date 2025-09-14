using Inventory.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class ErrorHandlingService(ILogger<ErrorHandlingService> logger, IRetryService retryService, INotificationService notificationService) : IErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService> _logger = logger;
    private readonly IRetryService _retryService = retryService;
    private readonly INotificationService _notificationService = notificationService;

    public async Task HandleErrorAsync(Exception exception, string? context = null, object? additionalData = null)
    {
        var errorContext = new ErrorContext(context ?? "Unknown", null);
        
        _logger.LogError(exception, "Error in {Context}: {Message}", context, exception.Message);
        
        // Show user-friendly notification
        _notificationService.ShowError(
            "Operation Failed",
            GetUserFriendlyMessage(exception)
        );
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
        try
        {
            return await _retryService.ExecuteWithRetryAsync(operation, operationName, maxRetries);
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
            await _retryService.ExecuteWithRetryAsync(async () => { await operation(); return true; }, operationName, maxRetries);
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
        
        _notificationService.ShowError(
            "API Error",
            $"Operation '{operationName}' failed: {response.StatusCode}"
        );
    }

    private static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => "Network connection failed. Please check your internet connection.",
            TaskCanceledException => "Operation timed out. Please try again.",
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            ArgumentException => "Invalid data provided. Please check your input.",
            _ => "An unexpected error occurred. Please try again later."
        };
    }
}
