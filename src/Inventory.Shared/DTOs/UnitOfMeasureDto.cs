using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class UnitOfMeasureDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateUnitOfMeasureDto
{
    [Required(ErrorMessage = "Unit of measure name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 50 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Symbol is required")]
    [StringLength(10, MinimumLength = 1, ErrorMessage = "Symbol must be between 1 and 10 characters")]
    public string Symbol { get; set; } = string.Empty;
    
    [StringLength(200, ErrorMessage = "Description must not exceed 200 characters")]
    public string? Description { get; set; }
}

public class UpdateUnitOfMeasureDto
{
    [Required(ErrorMessage = "Unit of measure name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 50 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Symbol is required")]
    [StringLength(10, MinimumLength = 1, ErrorMessage = "Symbol must be between 1 and 10 characters")]
    public string Symbol { get; set; } = string.Empty;
    
    [StringLength(200, ErrorMessage = "Description must not exceed 200 characters")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}
