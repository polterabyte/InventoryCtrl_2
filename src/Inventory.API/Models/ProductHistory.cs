namespace Inventory.API.Models;

public class ProductHistory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime Date { get; set; }
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public string? Description { get; set; }
}
