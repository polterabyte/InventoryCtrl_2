namespace Inventory.Shared.DTOs;

public class LowStockKanbanDto
{
    public int KanbanCardId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int MinThreshold { get; set; }
    public int MaxThreshold { get; set; }
    public string UnitOfMeasureSymbol { get; set; } = string.Empty;
}