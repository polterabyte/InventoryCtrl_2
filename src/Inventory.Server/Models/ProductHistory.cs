namespace Inventory.Server.Models;

public class ProductHistory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime Date { get; set; }
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Description { get; set; }
}
