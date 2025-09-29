using System.ComponentModel.DataAnnotations;

namespace Inventory.API.Models;

/// <summary>
/// Junction table implementing many-to-many relationship between Users and Warehouses
/// with additional metadata for access control and assignment details
/// </summary>
public class UserWarehouse
{
    /// <summary>
    /// User ID (Foreign Key to User.Id)
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Warehouse ID (Foreign Key to Warehouse.Id)
    /// </summary>
    [Required]
    public int WarehouseId { get; set; }
    
    /// <summary>
    /// Indicates if this is the user's default/primary warehouse
    /// Only one warehouse per user can be marked as default
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// Access level for this warehouse assignment
    /// Values: Full, ReadOnly, Limited
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string AccessLevel { get; set; } = "Full";
    
    /// <summary>
    /// Timestamp when the user was assigned to this warehouse
    /// </summary>
    [Required]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Record creation timestamp
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Record last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    
    /// <summary>
    /// Navigation property to User entity
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Navigation property to Warehouse entity
    /// </summary>
    public Warehouse Warehouse { get; set; } = null!;
}