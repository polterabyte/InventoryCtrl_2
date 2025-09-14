namespace Inventory.API.Models;

/// <summary>
/// Product tags for categorization and filtering
/// </summary>
public class ProductTag(int id, string name, string? description = null, bool isActive = true)
{
    public int Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string? Description { get; set; } = description;
    public bool IsActive { get; set; } = isActive;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
