using System.ComponentModel.DataAnnotations;

namespace Inventory.API.Models;

public class PushSubscription
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string P256dh { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Auth { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUsedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(100)]
    public string? UserAgent { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}
