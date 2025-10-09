using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    // This quantity is now populated from ProductOnHandView for data consistency
    public int Quantity { get; set; }
    public int UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; } = string.Empty;
    public string UnitOfMeasureSymbol { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductModelId { get; set; }
    public string ProductModelName { get; set; } = string.Empty;
    public int ProductGroupId { get; set; }
    public string ProductGroupName { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductDto
{
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "SKU must be between 2 and 50 characters")]
    public string SKU { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Unit of measure is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Unit of measure ID must be greater than 0")]
    public int UnitOfMeasureId { get; set; }

    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Category is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be greater than 0")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Product model is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Product model ID must be greater than 0")]
    public int ProductModelId { get; set; }
    
    [Required(ErrorMessage = "Product group is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Product group ID must be greater than 0")]
    public int ProductGroupId { get; set; }
    
    [StringLength(500, ErrorMessage = "Note must not exceed 500 characters")]
    public string? Note { get; set; }
}

public class UpdateProductDto
{
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "SKU must be between 2 and 50 characters")]
    public string SKU { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Unit of measure is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Unit of measure ID must be greater than 0")]
    public int UnitOfMeasureId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [Required(ErrorMessage = "Category is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be greater than 0")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Product model is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Product model ID must be greater than 0")]
    public int ProductModelId { get; set; }
    
    [Required(ErrorMessage = "Product group is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Product group ID must be greater than 0")]
    public int ProductGroupId { get; set; }
    
    [StringLength(500, ErrorMessage = "Note must not exceed 500 characters")]
    public string? Note { get; set; }
}

public class ProductStockAdjustmentDto
{
    [Required(ErrorMessage = "Product ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Product ID must be greater than 0")]
    public int ProductId { get; set; }
    
    [Required(ErrorMessage = "Quantity is required")]
    [Range(int.MinValue, int.MaxValue, ErrorMessage = "Quantity must be a valid number")]
    public int Quantity { get; set; }
    
    [Required(ErrorMessage = "Reason is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Reason must be between 2 and 200 characters")]
    public string Reason { get; set; } = string.Empty;
    
    [StringLength(100, ErrorMessage = "Reference must not exceed 100 characters")]
    public string? Reference { get; set; }
}
