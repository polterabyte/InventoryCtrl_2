using Inventory.Shared.Models;

namespace Inventory.Shared.Services;

public interface INotificationService
{
    void ShowSuccess(string title, string message, int duration = 5000);
    void ShowError(string title, string message, Action? onRetry = null, string? retryText = "Retry", int duration = 0);
    void ShowWarning(string title, string message, int duration = 5000);
    void ShowInfo(string title, string message, int duration = 5000);
    void ShowDebug(string title, string message, int duration = 10000);
    void RemoveNotification(string id);
    void ClearAll();
    NotificationState GetState();
}

public class NotificationService : INotificationService
{
    private readonly NotificationState _state;
    private readonly Timer? _cleanupTimer;

    public NotificationService()
    {
        _state = new NotificationState();
        
        // Cleanup expired notifications every 30 seconds
        _cleanupTimer = new Timer(CleanupExpiredNotifications, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public void ShowSuccess(string title, string message, int duration = 5000)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Success,
            Duration = duration
        };
        
        _state.AddNotification(notification);
        AutoRemoveNotification(notification.Id, duration);
    }

    public void ShowError(string title, string message, Action? onRetry = null, string? retryText = "Retry", int duration = 0)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Error,
            Duration = duration,
            OnRetry = onRetry,
            RetryText = retryText,
            IsDismissible = true
        };
        
        _state.AddNotification(notification);
        
        if (duration > 0)
        {
            AutoRemoveNotification(notification.Id, duration);
        }
    }

    public void ShowWarning(string title, string message, int duration = 5000)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Warning,
            Duration = duration
        };
        
        _state.AddNotification(notification);
        AutoRemoveNotification(notification.Id, duration);
    }

    public void ShowInfo(string title, string message, int duration = 5000)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Info,
            Duration = duration
        };
        
        _state.AddNotification(notification);
        AutoRemoveNotification(notification.Id, duration);
    }

    public void ShowDebug(string title, string message, int duration = 10000)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Debug,
            Duration = duration
        };
        
        _state.AddNotification(notification);
        AutoRemoveNotification(notification.Id, duration);
    }

    public void RemoveNotification(string id)
    {
        _state.RemoveNotification(id);
    }

    public void ClearAll()
    {
        _state.ClearAll();
    }

    public NotificationState GetState()
    {
        return _state;
    }

    private void AutoRemoveNotification(string id, int duration)
    {
        if (duration > 0)
        {
            Task.Delay(duration).ContinueWith(_ =>
            {
                RemoveNotification(id);
            });
        }
    }

    private void CleanupExpiredNotifications(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredNotifications = _state.Notifications
            .Where(n => n.Duration > 0 && (now - n.CreatedAt).TotalMilliseconds > n.Duration)
            .Select(n => n.Id)
            .ToList();

        foreach (var id in expiredNotifications)
        {
            RemoveNotification(id);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
