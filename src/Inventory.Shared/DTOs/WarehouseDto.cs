using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class WarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? Description { get; set; }
    public string? ContactInfo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // User assignment information
    public List<UserWarehouseDto> AssignedUsers { get; set; } = new();
    public int TotalAssignedUsers { get; set; }
}

public class CreateWarehouseDto
{
    [Required(ErrorMessage = "Warehouse name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Warehouse name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Location is required")]
    public int? LocationId { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [StringLength(200, ErrorMessage = "Contact info cannot exceed 200 characters")]
    public string? ContactInfo { get; set; }
}

public class UpdateWarehouseDto
{
    [Required(ErrorMessage = "Warehouse name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Warehouse name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Location is required")]
    public int? LocationId { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [StringLength(200, ErrorMessage = "Contact info cannot exceed 200 characters")]
    public string? ContactInfo { get; set; }
    
    public bool IsActive { get; set; } = true;
}
