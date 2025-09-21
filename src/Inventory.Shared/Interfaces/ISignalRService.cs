using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Inventory.Shared.Interfaces;

public interface ISignalRService
{
    Task<bool> InitializeConnectionAsync<T>(string accessToken, DotNetObjectReference<T> dotNetRef) where T : class;
    Task SubscribeToNotificationTypeAsync(string notificationType);
    Task UnsubscribeFromNotificationTypeAsync(string notificationType);
    Task DisconnectAsync();
}
