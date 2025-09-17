using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public bool IsRead { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? TransactionId { get; set; }
}

public class CreateNotificationRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? ActionUrl { get; set; }
    
    [StringLength(50)]
    public string? ActionText { get; set; }
    
    public string? UserId { get; set; }
    public int? ProductId { get; set; }
    public int? TransactionId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class NotificationPreferenceDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; }
    public bool InAppEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public int? MinPriority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateNotificationPreferenceRequest
{
    [Required]
    public string EventType { get; set; } = string.Empty;
    
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = false;
    public int? MinPriority { get; set; }
}

public class NotificationRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class CreateNotificationRuleRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string NotificationType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;
    
    [Required]
    public string Condition { get; set; } = string.Empty;
    
    [Required]
    public string Template { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;
}

public class NotificationStatsDto
{
    public int TotalNotifications { get; set; }
    public int UnreadNotifications { get; set; }
    public int ArchivedNotifications { get; set; }
    public int NotificationsByType { get; set; }
    public int NotificationsByCategory { get; set; }
    public DateTime? LastNotificationDate { get; set; }
}

public class NotificationSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
}
