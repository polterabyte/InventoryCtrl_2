using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class UpdateLocationDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public int? ParentLocationId { get; set; }
}
