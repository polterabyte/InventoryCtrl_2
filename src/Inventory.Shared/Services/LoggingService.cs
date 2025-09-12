using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Inventory.Shared.Services;

public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
    }

    public async Task LogInformationAsync(string message, object? data = null)
    {
        await Task.Run(() =>
        {
            if (data != null)
            {
                _logger.LogInformation("{Message} | Data: {Data}", message, JsonSerializer.Serialize(data));
            }
            else
            {
                _logger.LogInformation(message);
            }
        });
    }

    public async Task LogWarningAsync(string message, object? data = null)
    {
        await Task.Run(() =>
        {
            if (data != null)
            {
                _logger.LogWarning("{Message} | Data: {Data}", message, JsonSerializer.Serialize(data));
            }
            else
            {
                _logger.LogWarning(message);
            }
        });
    }

    public async Task LogErrorAsync(string message, Exception? exception = null, object? data = null)
    {
        await Task.Run(() =>
        {
            if (exception != null && data != null)
            {
                _logger.LogError(exception, "{Message} | Data: {Data}", message, JsonSerializer.Serialize(data));
            }
            else if (exception != null)
            {
                _logger.LogError(exception, message);
            }
            else if (data != null)
            {
                _logger.LogError("{Message} | Data: {Data}", message, JsonSerializer.Serialize(data));
            }
            else
            {
                _logger.LogError(message);
            }
        });
    }

    public async Task LogDebugAsync(string message, object? data = null)
    {
        await Task.Run(() =>
        {
            if (data != null)
            {
                _logger.LogDebug("{Message} | Data: {Data}", message, JsonSerializer.Serialize(data));
            }
            else
            {
                _logger.LogDebug(message);
            }
        });
    }
}
