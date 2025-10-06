using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class ProductGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? ParentProductGroupId { get; set; }
    public string? ParentProductGroupName { get; set; }
    public List<ProductGroupDto> Children { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductGroupDto
{
    [Required(ErrorMessage = "Product group name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Product group name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    public int? ParentProductGroupId { get; set; }
}

public class UpdateProductGroupDto
{
    [Required(ErrorMessage = "Product group name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Product group name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;

    public int? ParentProductGroupId { get; set; }
}
