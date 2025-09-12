namespace Inventory.Server.Models;

public class User : Microsoft.AspNetCore.Identity.IdentityUser<string>
{
    public string Role { get; set; } = string.Empty;
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ProductHistory> ProductHistories { get; set; } = new List<ProductHistory>();
}
