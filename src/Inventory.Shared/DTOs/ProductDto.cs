namespace Inventory.Shared.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = string.Empty;
    public int ProductModelId { get; set; }
    public string ProductModelName { get; set; } = string.Empty;
    public int ProductGroupId { get; set; }
    public string ProductGroupName { get; set; } = string.Empty;
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public int ManufacturerId { get; set; }
    public int ProductModelId { get; set; }
    public int ProductGroupId { get; set; }
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public string? Note { get; set; }
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public int ManufacturerId { get; set; }
    public int ProductModelId { get; set; }
    public int ProductGroupId { get; set; }
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public string? Note { get; set; }
}

public class ProductStockAdjustmentDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Reference { get; set; }
}
