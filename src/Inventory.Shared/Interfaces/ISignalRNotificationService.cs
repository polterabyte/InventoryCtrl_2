using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface ISignalRNotificationService
{
    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    Task SendNotificationToUserAsync(string userId, NotificationDto notification);

    /// <summary>
    /// Send notification to multiple users
    /// </summary>
    Task SendNotificationToUsersAsync(IEnumerable<string> userIds, NotificationDto notification);

    /// <summary>
    /// Send notification to all connected users
    /// </summary>
    Task SendNotificationToAllAsync(NotificationDto notification);

    /// <summary>
    /// Send notification to users in a specific group
    /// </summary>
    Task SendNotificationToGroupAsync(string groupName, NotificationDto notification);

    /// <summary>
    /// Send notification to users subscribed to a specific notification type
    /// </summary>
    Task SendNotificationByTypeAsync(string notificationType, NotificationDto notification);

    /// <summary>
    /// Send stock alert notification
    /// </summary>
    Task SendStockAlertAsync(string productName, int currentStock, int threshold, string alertType);

    /// <summary>
    /// Send transaction notification
    /// </summary>
    Task SendTransactionNotificationAsync(string userId, string transactionType, string productName, int quantity);

    /// <summary>
    /// Send system notification
    /// </summary>
    Task SendSystemNotificationAsync(string title, string message, string? actionUrl = null);

    /// <summary>
    /// Get count of connected users
    /// </summary>
    Task<int> GetConnectedUsersCountAsync();

    /// <summary>
    /// Check if user is connected
    /// </summary>
    Task<bool> IsUserConnectedAsync(string userId);
}
