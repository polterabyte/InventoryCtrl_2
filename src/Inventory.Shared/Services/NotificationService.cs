using Inventory.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public interface INotificationService
{
    void ShowSuccess(string title, string message);
    void ShowError(string title, string message, Action? retryAction = null, string? retryText = null);
    void ShowWarning(string title, string message);
    void ShowInfo(string title, string message);
    void ShowDebug(string title, string message);
    NotificationState GetState();
    void RemoveNotification(string id);
    event Action<ToastNotification>? NotificationReceived;
}

public class NotificationService(ILogger<NotificationService> logger) : INotificationService
{
    private readonly ILogger<NotificationService> _logger = logger;
    private readonly NotificationState _state = new();
    
    public event Action<ToastNotification>? NotificationReceived;

    public void ShowSuccess(string title, string message)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Success
        };
        
        _state.AddNotification(notification);
        _logger.LogInformation("Success notification: {Title} - {Message}", title, message);
        NotificationReceived?.Invoke(new ToastNotification(notification.Id, title, message, NotificationType.Success));
    }

    public void ShowError(string title, string message, Action? retryAction = null, string? retryText = null)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Error,
            Duration = 10000, // Longer duration for errors
            OnRetry = retryAction,
            RetryText = retryText
        };
        
        _state.AddNotification(notification);
        _logger.LogError("Error notification: {Title} - {Message}", title, message);
        NotificationReceived?.Invoke(new ToastNotification(notification.Id, title, message, NotificationType.Error, notification.Duration));
    }

    public void ShowWarning(string title, string message)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Warning
        };
        
        _state.AddNotification(notification);
        _logger.LogWarning("Warning notification: {Title} - {Message}", title, message);
        NotificationReceived?.Invoke(new ToastNotification(notification.Id, title, message, NotificationType.Warning));
    }

    public void ShowInfo(string title, string message)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Info
        };
        
        _state.AddNotification(notification);
        _logger.LogInformation("Info notification: {Title} - {Message}", title, message);
        NotificationReceived?.Invoke(new ToastNotification(notification.Id, title, message, NotificationType.Info));
    }

    public void ShowDebug(string title, string message)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Debug
        };
        
        _state.AddNotification(notification);
        _logger.LogDebug("Debug notification: {Title} - {Message}", title, message);
        NotificationReceived?.Invoke(new ToastNotification(notification.Id, title, message, NotificationType.Debug));
    }

    public NotificationState GetState()
    {
        return _state;
    }

    public void RemoveNotification(string id)
    {
        _state.RemoveNotification(id);
    }
}