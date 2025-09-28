namespace Inventory.API.Models;

public class Warehouse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LocationId { get; set; }
    public Location? Location { get; set; }
    public string? ContactInfo { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
