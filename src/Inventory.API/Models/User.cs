namespace Inventory.API.Models;

public class User : Microsoft.AspNetCore.Identity.IdentityUser<string>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ProductHistory> ProductHistories { get; set; } = new List<ProductHistory>();
    public ICollection<UserWarehouse> UserWarehouses { get; set; } = new List<UserWarehouse>();
}
