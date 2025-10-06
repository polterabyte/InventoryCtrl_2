namespace Inventory.API.Models;

public class ProductGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? ParentProductGroupId { get; set; }
    public ProductGroup? ParentProductGroup { get; set; }
    public ICollection<ProductGroup> SubGroups { get; set; } = new List<ProductGroup>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
