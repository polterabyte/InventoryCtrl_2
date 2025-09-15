using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class ProductModelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductModelDto
{
    [Required(ErrorMessage = "Product model name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Product model name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Manufacturer is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Manufacturer ID must be greater than 0")]
    public int ManufacturerId { get; set; }
}

public class UpdateProductModelDto
{
    [Required(ErrorMessage = "Product model name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Product model name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Manufacturer is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Manufacturer ID must be greater than 0")]
    public int ManufacturerId { get; set; }
    
    public bool IsActive { get; set; } = true;
}
