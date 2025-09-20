namespace Inventory.Shared.DTOs;

public class PushSubscriptionDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}

public class CreatePushSubscriptionDto
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public class PushNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Badge { get; set; }
    public string? Image { get; set; }
    public string? Tag { get; set; }
    public bool RequireInteraction { get; set; } = false;
    public int? Ttl { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public List<NotificationAction>? Actions { get; set; }
}

public class NotificationAction
{
    public string Action { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
}
