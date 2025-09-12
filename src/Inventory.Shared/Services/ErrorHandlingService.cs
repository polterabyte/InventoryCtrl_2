namespace Inventory.Shared.Services;

public class ErrorHandlingService : IErrorHandlingService
{
    private readonly ILoggingService _loggingService;

    public ErrorHandlingService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task HandleErrorAsync(Exception exception, string? context = null, object? additionalData = null)
    {
        var errorContext = !string.IsNullOrEmpty(context) ? context : "Unknown context";
        var errorMessage = $"Error occurred in {errorContext}";

        await _loggingService.LogErrorAsync(errorMessage, exception, additionalData);
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
}
