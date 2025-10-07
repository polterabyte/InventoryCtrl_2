namespace Inventory.API.Models;

public class Manufacturer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactInfo { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductModel> Models { get; set; } = new List<ProductModel>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

