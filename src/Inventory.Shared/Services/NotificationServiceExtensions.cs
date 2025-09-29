using Radzen;

namespace Inventory.Shared.Services;

/// <summary>
/// Extension methods for Radzen NotificationService to provide simplified Show methods
/// </summary>
public static class NotificationServiceExtensions
{
    /// <summary>
    /// Shows a success notification
    /// </summary>
    /// <param name="notificationService">The notification service</param>
    /// <param name="summary">The notification summary/title</param>
    /// <param name="detail">The notification detail message</param>
    /// <param name="duration">Duration in milliseconds (default: 4000)</param>
    public static void ShowSuccess(this NotificationService notificationService, string summary, string detail, double duration = 4000)
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = summary,
            Detail = detail,
            Duration = duration
        });
    }

    /// <summary>
    /// Shows an error notification
    /// </summary>
    /// <param name="notificationService">The notification service</param>
    /// <param name="summary">The notification summary/title</param>
    /// <param name="detail">The notification detail message</param>
    /// <param name="duration">Duration in milliseconds (default: 5000)</param>
    public static void ShowError(this NotificationService notificationService, string summary, string detail, double duration = 5000)
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = summary,
            Detail = detail,
            Duration = duration
        });
    }

    /// <summary>
    /// Shows a warning notification
    /// </summary>
    /// <param name="notificationService">The notification service</param>
    /// <param name="summary">The notification summary/title</param>
    /// <param name="detail">The notification detail message</param>
    /// <param name="duration">Duration in milliseconds (default: 4500)</param>
    public static void ShowWarning(this NotificationService notificationService, string summary, string detail, double duration = 4500)
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Warning,
            Summary = summary,
            Detail = detail,
            Duration = duration
        });
    }

    /// <summary>
    /// Shows an info notification
    /// </summary>
    /// <param name="notificationService">The notification service</param>
    /// <param name="summary">The notification summary/title</param>
    /// <param name="detail">The notification detail message</param>
    /// <param name="duration">Duration in milliseconds (default: 3500)</param>
    public static void ShowInfo(this NotificationService notificationService, string summary, string detail, double duration = 3500)
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = summary,
            Detail = detail,
            Duration = duration
        });
    }
}