using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IPushNotificationService
{
    Task<bool> SubscribeAsync(string userId, CreatePushSubscriptionDto subscription);
    Task<bool> UnsubscribeAsync(string userId, string endpoint);
    Task<bool> UnsubscribeAllAsync(string userId);
    Task<List<PushSubscriptionDto>> GetUserSubscriptionsAsync(string userId);
    Task<bool> SendNotificationAsync(string userId, PushNotificationDto notification);
    Task<bool> SendNotificationToAllAsync(PushNotificationDto notification);
    Task<bool> SendNotificationToSubscriptionsAsync(List<string> userIds, PushNotificationDto notification);
    Task<bool> IsUserSubscribedAsync(string userId);
    Task<int> GetActiveSubscriptionsCountAsync();
}
