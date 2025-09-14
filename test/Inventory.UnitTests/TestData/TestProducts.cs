using Inventory.API.Models;

namespace Inventory.UnitTests.TestData;

public static class TestProducts
{
    public static Product SampleProduct => new()
    {
        Id = 1,
        Name = "Test Product",
        SKU = "TEST-001",
        Description = "Test Description",
        Quantity = 100,
        Unit = "pcs",
        IsActive = true,
        MinStock = 10,
        MaxStock = 1000,
        Note = "Test Note",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static Product InactiveProduct => new()
    {
        Id = 2,
        Name = "Inactive Product",
        SKU = "INACTIVE-001",
        Description = "Inactive Description",
        Quantity = 0,
        Unit = "pcs",
        IsActive = false,
        MinStock = 0,
        MaxStock = 100,
        Note = "Inactive Note",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static Product LowStockProduct => new()
    {
        Id = 3,
        Name = "Low Stock Product",
        SKU = "LOW-001",
        Description = "Low Stock Description",
        Quantity = 5,
        Unit = "pcs",
        IsActive = true,
        MinStock = 10,
        MaxStock = 100,
        Note = "Low Stock Note",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
