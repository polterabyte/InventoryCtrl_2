namespace Inventory.Shared.Models;

public class ProductHistory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
