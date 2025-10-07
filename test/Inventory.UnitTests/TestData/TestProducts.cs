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
        // Quantity = 100, // Removed - using CurrentQuantity computed property
        CurrentQuantity = 100,
                UnitOfMeasureId = 1,
        IsActive = true,
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
        // Quantity = 0, // Removed - using CurrentQuantity computed property
        CurrentQuantity = 0,
                UnitOfMeasureId = 1,
        IsActive = false,
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
        // Quantity = 5, // Removed - using CurrentQuantity computed property
        CurrentQuantity = 5,
                UnitOfMeasureId = 1,
        IsActive = true,
        Note = "Low Stock Note",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
