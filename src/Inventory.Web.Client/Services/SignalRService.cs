using Microsoft.Extensions.Logging;
using Inventory.Shared.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Threading;

namespace Inventory.Web.Client.Services;

public class SignalRService : ISignalRService
{
    private readonly IApiUrlService _apiUrlService;
    private readonly ILogger<SignalRService> _logger;
    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private Task<bool>? _initializationTask;
    private string? _currentAccessToken;
    
    public event Action<string>? ConnectionStateChanged;
    
    public SignalRService(IApiUrlService apiUrlService, ILogger<SignalRService> logger)
    {
        _apiUrlService = apiUrlService;
        _logger = logger;
    }
    
    public string GetConnectionState()
    {
        return _connection?.State.ToString() ?? "Disconnected";
    }

    public async Task<bool> InitializeConnectionAsync<T>(string accessToken, DotNetObjectReference<T> dotNetRef) where T : class
    {
        _ = dotNetRef; // Reserved for future interop callbacks

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning("Cannot initialize SignalR: access token is empty");
            return false;
        }

        _currentAccessToken = accessToken;

        if (_connection != null)
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                return true;
            }

            if (_connection.State is HubConnectionState.Connecting or HubConnectionState.Reconnecting)
            {
                var existingTask = _initializationTask;
                if (existingTask != null)
                {
                    return await existingTask.ConfigureAwait(false);
                }

                try
                {
                    await _connection.StartAsync().ConfigureAwait(false);
                    return _connection.State == HubConnectionState.Connected;
                }
                catch (Exception startEx)
                {
                    _logger.LogWarning(startEx, "SignalR connection start retry failed");
                }
            }
        }

        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                return true;
            }

            if (_initializationTask != null)
            {
                return await _initializationTask.ConfigureAwait(false);
            }

            _initializationTask = InitializeConnectionInternalAsync(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling SignalR initialization");
            _initializationTask = null;
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }

        try
        {
            return await _initializationTask.ConfigureAwait(false);
        }
        finally
        {
            if (_initializationTask?.IsCompleted == true)
            {
                _initializationTask = null;
            }
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
            await _connectionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                _initializationTask = null;

                if (_connection is not null)
                {
                    if (_connection.State != HubConnectionState.Disconnected)
                    {
                        await _connection.StopAsync().ConfigureAwait(false);
                    }

                    await _connection.DisposeAsync().ConfigureAwait(false);
                    _connection = null;
                    _currentAccessToken = null;
                }
            }
            finally
            {
                _connectionLock.Release();
            }

            ConnectionStateChanged?.Invoke("Disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting SignalR");
        }
    }

    private async Task<bool> InitializeConnectionInternalAsync(string accessToken)
    {
        try
        {
            var baseUrl = await _apiUrlService.GetSignalRUrlAsync().ConfigureAwait(false);

            // Normalize: ensure exactly one /notificationHub
            var normalizedBase = baseUrl.TrimEnd('/');
            if (!normalizedBase.EndsWith("/notificationHub", StringComparison.OrdinalIgnoreCase))
            {
                normalizedBase = normalizedBase.Replace("/api", string.Empty).TrimEnd('/') + "/notificationHub";
            }

            _currentAccessToken = accessToken;

            _connection = new HubConnectionBuilder()
                .WithUrl(normalizedBase, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_currentAccessToken)!;
                })
                .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            _connection.Reconnected += id =>
            {
                _logger.LogInformation("SignalR reconnected: {Id}", id);
                ConnectionStateChanged?.Invoke("Connected");
                return Task.CompletedTask;
            };

            _connection.Closed += ex =>
            {
                _logger.LogWarning(ex, "SignalR closed");
                ConnectionStateChanged?.Invoke("Disconnected");
                return Task.CompletedTask;
            };

            _connection.Reconnecting += ex =>
            {
                _logger.LogInformation("SignalR reconnecting...");
                ConnectionStateChanged?.Invoke("Reconnecting");
                return Task.CompletedTask;
            };

            await _connection.StartAsync().ConfigureAwait(false);
            _logger.LogInformation("SignalR connection started: {State}", _connection.State);

            ConnectionStateChanged?.Invoke(_connection.State.ToString());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SignalR connection");
            await DisconnectAsync().ConfigureAwait(false);
            return false;
        }
    }
}
