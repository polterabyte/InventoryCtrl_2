using Inventory.Shared.Models;

namespace Inventory.Shared.Services;

public class ErrorHandlingService : IErrorHandlingService
{
    private readonly ILoggingService _loggingService;
    private readonly INotificationService? _notificationService;
    private readonly IRetryService? _retryService;

    public ErrorHandlingService(ILoggingService loggingService, INotificationService? notificationService = null, IRetryService? retryService = null)
    {
        _loggingService = loggingService;
        _notificationService = notificationService;
        _retryService = retryService;
    }

    public async Task HandleErrorAsync(Exception exception, string? context = null, object? additionalData = null)
    {
        var errorContext = !string.IsNullOrEmpty(context) ? context : "Unknown context";
        var errorMessage = $"Error occurred in {errorContext}";

        await _loggingService.LogErrorAsync(errorMessage, exception, additionalData);

        // Show user-friendly notification
        _notificationService?.ShowError(
            "Operation Failed",
            GetUserFriendlyMessage(exception),
            null,
            "Retry"
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
        if (_retryService == null)
        {
            return await TryExecuteAsync(operation, operationName, contextData) ? await operation() : default(T);
        }

        try
        {
            return await _retryService.ExecuteWithRetryAsync(operation, operationName, maxRetries);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, operationName, contextData);
            return default(T);
        }
    }

    public async Task<bool> TryExecuteWithRetryAsync(Func<Task> operation, string operationName, object? contextData = null, int maxRetries = 3)
    {
        if (_retryService == null)
        {
            return await TryExecuteAsync(operation, operationName, contextData);
        }

        try
        {
            await _retryService.ExecuteWithRetryAsync(operation, operationName, maxRetries);
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
        var statusCode = response.StatusCode;

        var userMessage = statusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => "Invalid request. Please check your input.",
            System.Net.HttpStatusCode.Unauthorized => "You are not authorized to perform this action.",
            System.Net.HttpStatusCode.Forbidden => "Access denied. You don't have permission for this operation.",
            System.Net.HttpStatusCode.NotFound => "The requested resource was not found.",
            System.Net.HttpStatusCode.Conflict => "A conflict occurred. The resource may have been modified by another user.",
            System.Net.HttpStatusCode.InternalServerError => "A server error occurred. Please try again later.",
            System.Net.HttpStatusCode.ServiceUnavailable => "The service is temporarily unavailable. Please try again later.",
            _ => $"An error occurred ({(int)statusCode}). Please try again."
        };

        await _loggingService.LogErrorAsync($"API Error in {operationName}: {statusCode} - {errorMessage}");

        _notificationService?.ShowError(
            "API Error",
            userMessage,
            null,
            "Retry"
        );
    }

    private string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => "Network error. Please check your connection and try again.",
            TaskCanceledException => "The operation was cancelled or timed out.",
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            ArgumentException => "Invalid input provided. Please check your data.",
            InvalidOperationException => "The operation cannot be completed in the current state.",
            TimeoutException => "The operation timed out. Please try again.",
            _ => "An unexpected error occurred. Please try again or contact support if the problem persists."
        };
    }
}
