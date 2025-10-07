namespace Inventory.Shared.DTOs;

public class DashboardStatsDto
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalManufacturers { get; set; }
    public int TotalWarehouses { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int RecentTransactions { get; set; }
    public int RecentProducts { get; set; }
}

public class RecentTransactionDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Date { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RecentProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ManufacturerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LowStockProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ManufacturerName { get; set; } = string.Empty;
    public string UnitOfMeasureSymbol { get; set; } = string.Empty;
    public List<LowStockKanbanDto> KanbanCards { get; set; } = new();
}

public class RecentActivityDto
{
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    public List<RecentProductDto> RecentProducts { get; set; } = new();
}
