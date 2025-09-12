namespace Inventory.Shared.Services;

public interface IErrorHandlingService
{
    Task HandleErrorAsync(Exception exception, string? context = null, object? additionalData = null);
    Task<bool> TryExecuteAsync<T>(Func<Task<T>> operation, string operationName, object? contextData = null);
    Task<bool> TryExecuteAsync(Func<Task> operation, string operationName, object? contextData = null);
}
