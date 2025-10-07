using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.API.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    // Quantity removed - use CurrentQuantity computed property instead
    // public int Quantity { get; set; } // Removed to prevent data duplication with ProductOnHandView
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; } = null!;
    public int ProductModelId { get; set; }
    public ProductModel ProductModel { get; set; } = null!;
    public int ProductGroupId { get; set; }
    public ProductGroup ProductGroup { get; set; } = null!;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Computed property for current quantity from ProductOnHandView
    // This replaces the direct Quantity field to ensure data consistency
    [NotMapped]
    public int CurrentQuantity { get; set; }
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
    public ICollection<KanbanCard> KanbanCards { get; set; } = new List<KanbanCard>();
}
