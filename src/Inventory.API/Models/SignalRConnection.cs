using System.ComponentModel.DataAnnotations;

namespace Inventory.API.Models;

public class SignalRConnection
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)]
    public string ConnectionId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? UserName { get; set; }
    
    [StringLength(50)]
    public string? UserRole { get; set; }
    
    [StringLength(100)]
    public string? UserAgent { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastActivityAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [StringLength(1000)]
    public string? SubscribedGroups { get; set; } // JSON array of group names
    
    [StringLength(1000)]
    public string? SubscribedNotificationTypes { get; set; } // JSON array of notification types
    
    // Navigation Properties
    public User? User { get; set; }
}
