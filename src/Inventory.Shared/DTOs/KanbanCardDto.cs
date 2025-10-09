namespace Inventory.Shared.DTOs;

public class KanbanCardDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int MinThreshold { get; set; }
    public int MaxThreshold { get; set; }
    public int CurrentQuantity { get; set; }
    public string UnitOfMeasureSymbol { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateKanbanCardDto
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int MinThreshold { get; set; }
    public int MaxThreshold { get; set; }
}

public class UpdateKanbanCardDto
{
    public int MinThreshold { get; set; }
    public int MaxThreshold { get; set; }
}
