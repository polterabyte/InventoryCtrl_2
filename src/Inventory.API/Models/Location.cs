namespace Inventory.API.Models;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ParentLocationId { get; set; }
    public Location? ParentLocation { get; set; }
    public ICollection<Location> SubLocations { get; set; } = new List<Location>();
    public ICollection<InventoryTransaction> InstallTransactions { get; set; } = new List<InventoryTransaction>();
}
