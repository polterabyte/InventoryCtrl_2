using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class WarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
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
}

public class UpdateWarehouseDto
{
    [Required(ErrorMessage = "Warehouse name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Warehouse name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Location is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Location must be between 2 and 200 characters")]
    public string Location { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
}
