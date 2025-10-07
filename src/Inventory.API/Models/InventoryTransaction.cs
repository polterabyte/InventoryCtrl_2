using System.Text.Json.Serialization;

namespace Inventory.API.Models;

public enum TransactionType
{
    Income = 0,
    Outcome = 1,
    Install = 2,
    Pending = 3
}

public class InventoryTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    public DateTime Date { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public int? LocationId { get; set; }
    public Location? Location { get; set; }
    public int? RequestId { get; set; }
    
    /// <summary>
    /// Navigation property to request. JsonIgnore prevents circular reference during serialization.
    /// </summary>
    [JsonIgnore]
    public Request? Request { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
