namespace Inventory.Server.Models;

public class User : Microsoft.AspNetCore.Identity.IdentityUser<string>
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ProductHistory> ProductHistories { get; set; } = new List<ProductHistory>();
}
