using Microsoft.Extensions.Logging;
using Inventory.Shared.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

public class SignalRService : ISignalRService
{
    private readonly IApiUrlService _apiUrlService;
    private readonly ILogger<SignalRService> _logger;
    private HubConnection? _connection;

    public SignalRService(IApiUrlService apiUrlService, ILogger<SignalRService> logger)
    {
        _apiUrlService = apiUrlService;
        _logger = logger;
    }

    public async Task<bool> InitializeConnectionAsync<T>(string accessToken, DotNetObjectReference<T> dotNetRef) where T : class
    {
        try
        {
            var baseUrl = await _apiUrlService.GetSignalRUrlAsync();
            // Normalize: ensure exactly one /notificationHub
            var normalizedBase = baseUrl.TrimEnd('/');
            if (!normalizedBase.EndsWith("/notificationHub", StringComparison.OrdinalIgnoreCase))
            {
                normalizedBase = normalizedBase.Replace("/api", string.Empty).TrimEnd('/') + "/notificationHub";
            }

            _connection = new HubConnectionBuilder()
                .WithUrl(normalizedBase, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(accessToken)!;
                })
                .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            // Optional: forward server events if needed in future
            _connection.Reconnected += id =>
            {
                _logger.LogInformation("SignalR reconnected: {Id}", id);
                return Task.CompletedTask;
            };
            _connection.Closed += ex =>
            {
                _logger.LogWarning(ex, "SignalR closed");
                return Task.CompletedTask;
            };

            await _connection.StartAsync();
            _logger.LogInformation("SignalR connection started: {State}", _connection.State);
            return true;
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
            if (_connection is not null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("SubscribeToNotifications", notificationType);
            }
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
            if (_connection is not null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("UnsubscribeFromNotifications", notificationType);
            }
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
            if (_connection is not null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting SignalR");
        }
    }
}