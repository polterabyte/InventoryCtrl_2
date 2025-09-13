using System.ComponentModel;

namespace Inventory.Shared.Models;

public class NotificationState : INotifyPropertyChanged
{
    private List<Notification> _notifications = new();
    private bool _isVisible = false;

    public List<Notification> Notifications
    {
        get => _notifications;
        set
        {
            _notifications = value;
            OnPropertyChanged(nameof(Notifications));
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnPropertyChanged(nameof(IsVisible));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void AddNotification(Notification notification)
    {
        _notifications.Add(notification);
        OnPropertyChanged(nameof(Notifications));
        IsVisible = true;
    }

    public void RemoveNotification(string id)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == id);
        if (notification != null)
        {
            _notifications.Remove(notification);
            OnPropertyChanged(nameof(Notifications));
            IsVisible = _notifications.Any();
        }
    }

    public void ClearAll()
    {
        _notifications.Clear();
        OnPropertyChanged(nameof(Notifications));
        IsVisible = false;
    }
}

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Duration { get; set; } = 5000; // milliseconds
    public bool IsDismissible { get; set; } = true;
    public Action? OnRetry { get; set; }
    public string? RetryText { get; set; }
}

public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info,
    Debug
}
