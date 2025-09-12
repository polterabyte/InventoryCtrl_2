namespace Inventory.Shared.Models;

public class InventoryTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT, TRANSFER
    public int Quantity { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; } // PO Number, Invoice Number, etc.
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
