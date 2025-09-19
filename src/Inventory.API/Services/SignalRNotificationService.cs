using Microsoft.AspNetCore.SignalR;
using Inventory.API.Hubs;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;

namespace Inventory.API.Services;

public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationToUserAsync(string userId, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Sent notification {NotificationId} to user {UserId}", notification.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to user {UserId}", notification.Id, userId);
        }
    }

    public async Task SendNotificationToUsersAsync(IEnumerable<string> userIds, NotificationDto notification)
    {
        try
        {
            var tasks = userIds.Select(userId => 
                _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification));
            
            await Task.WhenAll(tasks);
            _logger.LogInformation("Sent notification {NotificationId} to {UserCount} users", 
                notification.Id, userIds.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to multiple users", notification.Id);
        }
    }

    public async Task SendNotificationToAllAsync(NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.Group("AllUsers").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Sent notification {NotificationId} to all users", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to all users", notification.Id);
        }
    }

    public async Task SendNotificationToGroupAsync(string groupName, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Sent notification {NotificationId} to group {GroupName}", notification.Id, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to group {GroupName}", notification.Id, groupName);
        }
    }

    public async Task SendNotificationByTypeAsync(string notificationType, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.Group($"Notifications_{notificationType}").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Sent notification {NotificationId} to {NotificationType} subscribers", notification.Id, notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to {NotificationType} subscribers", notification.Id, notificationType);
        }
    }

    public async Task SendStockAlertAsync(string productName, int currentStock, int threshold, string alertType)
    {
        try
        {
            var notification = new NotificationDto
            {
                Title = $"Stock Alert: {productName}",
                Message = $"Current stock: {currentStock}, Threshold: {threshold}",
                Type = alertType == "LOW" ? "WARNING" : "ERROR",
                Category = "STOCK",
                ActionUrl = $"/products?search={productName}",
                ActionText = "View Product",
                CreatedAt = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("Notifications_STOCK").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Sent stock alert for {ProductName} to stock subscribers", productName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send stock alert for {ProductName}", productName);
        }
    }

    public async Task SendTransactionNotificationAsync(string userId, string transactionType, string productName, int quantity)
    {
        try
        {
            var notification = new NotificationDto
            {
                Title = $"Transaction {transactionType}",
                Message = $"{transactionType} {quantity} units of {productName}",
                Type = "INFO",
                Category = "TRANSACTION",
                ActionUrl = "/transactions",
                ActionText = "View Transactions",
                CreatedAt = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Sent transaction notification to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send transaction notification to user {UserId}", userId);
        }
    }

    public async Task SendSystemNotificationAsync(string title, string message, string? actionUrl = null)
    {
        try
        {
            var notification = new NotificationDto
            {
                Title = title,
                Message = message,
                Type = "INFO",
                Category = "SYSTEM",
                ActionUrl = actionUrl,
                ActionText = actionUrl != null ? "View Details" : null,
                CreatedAt = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("AllUsers").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Sent system notification: {Title}", title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send system notification: {Title}", title);
        }
    }

    public Task<int> GetConnectedUsersCountAsync()
    {
        try
        {
            // This is a simplified implementation
            // In a real scenario, you might want to track connections in a more sophisticated way
            var count = NotificationHub.GetAllConnections().Count();
            return Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connected users count");
            return Task.FromResult(0);
        }
    }

    public Task<bool> IsUserConnectedAsync(string userId)
    {
        try
        {
            var isConnected = NotificationHub.GetConnectionsForUser(userId).Any();
            return Task.FromResult(isConnected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} is connected", userId);
            return Task.FromResult(false);
        }
    }
}
