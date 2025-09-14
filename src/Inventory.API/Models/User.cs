namespace Inventory.API.Models;

public class User : Microsoft.AspNetCore.Identity.IdentityUser<string>
{
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ProductHistory> ProductHistories { get; set; } = new List<ProductHistory>();
}
