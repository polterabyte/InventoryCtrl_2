namespace Inventory.Shared.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    // Quantity removed - use CurrentQuantity computed property instead
    // public int Quantity { get; set; } // Removed to prevent data duplication with ProductOnHandView
    public int CurrentQuantity { get; set; }
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
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
