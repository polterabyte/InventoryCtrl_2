namespace Inventory.Shared.Models;

// Notification models with primary constructors
public record NotificationMessage(string Title, string Message, NotificationType Type, DateTime Timestamp = default)
{
    public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
}

public record ToastNotification(string Id, string Title, string Message, NotificationType Type, int Duration = 5000)
{
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsVisible { get; set; } = true;
}

public record RetryPolicy(int MaxRetries, TimeSpan BaseDelay, TimeSpan MaxDelay, double BackoffMultiplier = 2.0);

public record ErrorContext(string Operation, string? UserId, Dictionary<string, object>? AdditionalData = null)
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Configuration models with primary constructors

public record CorsConfiguration(string[] AllowedOrigins, string[] AllowedMethods, string[] AllowedHeaders)
{
    public static CorsConfiguration Default => new(
        new[] { "http://localhost:5000", "https://localhost:7000", "http://localhost:5001", "https://localhost:7001" },
        new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" },
        new[] { "Content-Type", "Authorization" }
    );
}

public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info,
    Debug
}
