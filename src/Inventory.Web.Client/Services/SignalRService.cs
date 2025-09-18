using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Inventory.Web.Client.Services;

public class SignalRService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<SignalRService> _logger;
    private readonly HttpClient _httpClient;
    private bool _isInitialized = false;

    public SignalRService(IJSRuntime jsRuntime, ILogger<SignalRService> logger, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> InitializeAsync(string accessToken)
    {
        try
        {
            if (_isInitialized)
            {
                _logger.LogInformation("SignalR service already initialized");
                return true;
            }

            // Get the API base URL from the HttpClient
            var apiBaseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                _logger.LogError("API base URL is not configured");
                return false;
            }

            _logger.LogInformation("Initializing SignalR service with API URL: {ApiUrl}", apiBaseUrl);

            // Call the JavaScript SignalR service
            var result = await _jsRuntime.InvokeAsync<bool>("signalRNotificationService.initialize", apiBaseUrl, accessToken);
            
            if (result)
            {
                _isInitialized = true;
                _logger.LogInformation("SignalR service initialized successfully");
            }
            else
            {
                _logger.LogError("Failed to initialize SignalR service");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SignalR service");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_isInitialized)
            {
                await _jsRuntime.InvokeVoidAsync("signalRNotificationService.disconnect");
                _isInitialized = false;
                _logger.LogInformation("SignalR service disconnected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting SignalR service");
        }
    }

    public async Task SubscribeToNotificationsAsync(string notificationType)
    {
        try
        {
            if (_isInitialized)
            {
                await _jsRuntime.InvokeVoidAsync("signalRNotificationService.subscribeToNotifications", notificationType);
                _logger.LogInformation("Subscribed to {NotificationType} notifications", notificationType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to {NotificationType} notifications", notificationType);
        }
    }

    public async Task UnsubscribeFromNotificationsAsync(string notificationType)
    {
        try
        {
            if (_isInitialized)
            {
                await _jsRuntime.InvokeVoidAsync("signalRNotificationService.unsubscribeFromNotifications", notificationType);
                _logger.LogInformation("Unsubscribed from {NotificationType} notifications", notificationType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from {NotificationType} notifications", notificationType);
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            if (!_isInitialized)
                return false;

            return await _jsRuntime.InvokeAsync<bool>("signalRNotificationService.isConnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SignalR connection status");
            return false;
        }
    }

    public async Task<string> GetConnectionStateAsync()
    {
        try
        {
            if (!_isInitialized)
                return "NotInitialized";

            return await _jsRuntime.InvokeAsync<string>("signalRNotificationService.getConnectionState");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SignalR connection state");
            return "Error";
        }
    }
}
