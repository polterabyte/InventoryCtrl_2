using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.Models;

public class DbNotification
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty; // INFO, WARNING, ERROR, SUCCESS
    
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty; // STOCK, TRANSACTION, SYSTEM, SECURITY
    
    [StringLength(100)]
    public string? ActionUrl { get; set; }
    
    [StringLength(50)]
    public string? ActionText { get; set; }
    
    public bool IsRead { get; set; }
    
    public bool IsArchived { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReadAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    // Foreign Keys
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    
    public int? TransactionId { get; set; }
    
    // Navigation Properties
    public User? User { get; set; }
    public Product? Product { get; set; }
    public InventoryTransaction? Transaction { get; set; }
}

public class NotificationRule
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty; // STOCK_LOW, STOCK_OUT, TRANSACTION_CREATED, etc.
    
    [Required]
    [StringLength(50)]
    public string NotificationType { get; set; } = string.Empty; // INFO, WARNING, ERROR, SUCCESS
    
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty; // STOCK, TRANSACTION, SYSTEM, SECURITY
    
    [Required]
    public string Condition { get; set; } = string.Empty; // JSON condition for when to trigger
    
    [Required]
    public string Template { get; set; } = string.Empty; // Message template
    
    public bool IsActive { get; set; } = true;
    
    public int Priority { get; set; } // Higher number = higher priority
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
}

public class NotificationPreference
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty;
    
    public bool EmailEnabled { get; set; } = true;
    
    public bool InAppEnabled { get; set; } = true;
    
    public bool PushEnabled { get; set; }
    
    public int? MinPriority { get; set; } // Only notify for priority >= this value
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    public User? User { get; set; }
}

public class NotificationTemplate
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public string SubjectTemplate { get; set; } = string.Empty;
    
    [Required]
    public string BodyTemplate { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string NotificationType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}
