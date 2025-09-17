using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class WarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? ContactInfo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWarehouseDto
{
    [Required(ErrorMessage = "Warehouse name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Warehouse name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Location is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Location must be between 2 and 200 characters")]
    public string Location { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
    public string? Address { get; set; }
    
    [StringLength(200, ErrorMessage = "Contact info cannot exceed 200 characters")]
    public string? ContactInfo { get; set; }
}

public class UpdateWarehouseDto
{
    [Required(ErrorMessage = "Warehouse name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Warehouse name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Location is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Location must be between 2 and 200 characters")]
    public string Location { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
    public string? Address { get; set; }
    
    [StringLength(200, ErrorMessage = "Contact info cannot exceed 200 characters")]
    public string? ContactInfo { get; set; }
    
    public bool IsActive { get; set; } = true;
}
