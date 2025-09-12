namespace Inventory.API.Models;

public class Warehouse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}
