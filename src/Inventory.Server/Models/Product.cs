namespace Inventory.Server.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; } = null!;
    public int ProductModelId { get; set; }
    public ProductModel ProductModel { get; set; } = null!;
    public int ProductGroupId { get; set; }
    public ProductGroup ProductGroup { get; set; } = null!;
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public string? Note { get; set; }
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}
