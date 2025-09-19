using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using Inventory.Shared.Interfaces;

namespace Inventory.Web.Client.Services;

public class SignalRService : ISignalRService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IApiUrlService _apiUrlService;
    private readonly ILogger<SignalRService> _logger;

    public SignalRService(IJSRuntime jsRuntime, IApiUrlService apiUrlService, ILogger<SignalRService> logger)
    {
        _jsRuntime = jsRuntime;
        _apiUrlService = apiUrlService;
        _logger = logger;
    }

    public async Task<bool> InitializeConnectionAsync<T>(string accessToken, DotNetObjectReference<T> dotNetRef) where T : class
    {
        try
        {
            var signalRUrl = await _apiUrlService.GetSignalRUrlAsync();
            _logger.LogDebug("Initializing SignalR connection with SignalR URL: {SignalRUrl}", signalRUrl);
            
            var success = await _jsRuntime.InvokeAsync<bool>("initializeSignalRConnection", signalRUrl, accessToken, dotNetRef);
            
            if (success)
            {
                _logger.LogInformation("SignalR connection initialized successfully");
            }
            else
            {
                _logger.LogError("Failed to initialize SignalR connection");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SignalR connection");
            return false;
        }
    }

    public async Task SubscribeToNotificationTypeAsync(string notificationType)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("subscribeToNotificationType", notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to notification type: {NotificationType}", notificationType);
        }
    }

    public async Task UnsubscribeFromNotificationTypeAsync(string notificationType)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("unsubscribeFromNotificationType", notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from notification type: {NotificationType}", notificationType);
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("disconnectSignalR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting SignalR");
        }
    }
}