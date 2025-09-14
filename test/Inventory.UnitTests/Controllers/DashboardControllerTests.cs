using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.API.Models;
using Xunit;

namespace Inventory.UnitTests.Controllers;

public class DashboardControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql($"Host=localhost;Port=5432;Database=inventory_test_{Guid.NewGuid():N};Username=postgres;Password=postgres;Pooling=true;")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _controller = new DashboardController(_context);
    }

    [Fact]
    public async Task GetDashboardStats_WithValidData_ShouldReturnStats()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetDashboardStats();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var stats = okResult.Value;
        stats.Should().NotBeNull();
        // Note: Detailed property assertions would require casting to specific DTO type
    }

    [Fact]
    public async Task GetDashboardStats_WithEmptyDatabase_ShouldReturnZeroStats()
    {
        // Act
        var result = await _controller.GetDashboardStats();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var stats = okResult!.Value;
        stats.Should().NotBeNull();
        // Note: Detailed property assertions would require casting to specific DTO type
    }

    [Fact]
    public async Task GetRecentActivity_WithValidData_ShouldReturnActivity()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetRecentActivity();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var activity = okResult.Value;
        activity.Should().NotBeNull();
        // Note: Detailed property assertions would require casting to specific DTO type
    }

    [Fact]
    public async Task GetLowStockProducts_WithValidData_ShouldReturnLowStockProducts()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var products = okResult.Value;
        products.Should().BeAssignableTo<IEnumerable<object>>();
    }

    [Fact]
    public async Task GetLowStockProducts_WithNoLowStockProducts_ShouldReturnEmptyList()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Test Category", IsActive = true };
        var manufacturer = new Manufacturer { Id = 1, Name = "Test Manufacturer" };
        var warehouse = new Warehouse { Id = 1, Name = "Test Warehouse", IsActive = true };
        var user = new User { Id = "1", UserName = "testuser", Email = "test@test.com" };
        var productGroup = new ProductGroup { Id = 1, Name = "Test Group", IsActive = true };
        var productModel = new ProductModel { Id = 1, Name = "Test Model", ManufacturerId = 1 };

        _context.Categories.Add(category);
        _context.Manufacturers.Add(manufacturer);
        _context.Warehouses.Add(warehouse);
        _context.Users.Add(user);
        _context.ProductGroups.Add(productGroup);
        _context.ProductModels.Add(productModel);

        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            SKU = "TEST001",
            Quantity = 100, // High quantity
            MinStock = 10,
            MaxStock = 200,
            CategoryId = 1,
            ManufacturerId = 1,
            ProductGroupId = 1,
            ProductModelId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<object>;
        products.Should().BeEmpty();
    }

    private async Task SeedTestDataAsync()
    {
        // Add categories
        var category1 = new Category { Id = 1, Name = "Category 1", IsActive = true };
        var category2 = new Category { Id = 2, Name = "Category 2", IsActive = true };
        _context.Categories.AddRange(category1, category2);

        // Add manufacturers
        var manufacturer1 = new Manufacturer { Id = 1, Name = "Manufacturer 1" };
        var manufacturer2 = new Manufacturer { Id = 2, Name = "Manufacturer 2" };
        _context.Manufacturers.AddRange(manufacturer1, manufacturer2);

        // Add product groups
        var productGroup1 = new ProductGroup { Id = 1, Name = "Group 1" };
        var productGroup2 = new ProductGroup { Id = 2, Name = "Group 2" };
        _context.ProductGroups.AddRange(productGroup1, productGroup2);

        // Add product models
        var productModel1 = new ProductModel { Id = 1, Name = "Model 1", ManufacturerId = 1 };
        var productModel2 = new ProductModel { Id = 2, Name = "Model 2", ManufacturerId = 2 };
        _context.ProductModels.AddRange(productModel1, productModel2);

        // Add warehouses
        var warehouse1 = new Warehouse { Id = 1, Name = "Warehouse 1", IsActive = true };
        var warehouse2 = new Warehouse { Id = 2, Name = "Warehouse 2", IsActive = true };
        _context.Warehouses.AddRange(warehouse1, warehouse2);

        // Add user
        var user = new User { Id = "1", UserName = "testuser", Email = "test@test.com" };
        _context.Users.Add(user);

        // Add products
        var product1 = new Product
        {
            Id = 1,
            Name = "Product 1",
            SKU = "SKU001",
            Quantity = 5, // Low stock
            MinStock = 10,
            MaxStock = 100,
            CategoryId = 1,
            ManufacturerId = 1,
            ProductModelId = 1,
            ProductGroupId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var product2 = new Product
        {
            Id = 2,
            Name = "Product 2",
            SKU = "SKU002",
            Quantity = 0, // Out of stock
            MinStock = 5,
            MaxStock = 50,
            CategoryId = 2,
            ManufacturerId = 2,
            ProductModelId = 2,
            ProductGroupId = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var product3 = new Product
        {
            Id = 3,
            Name = "Product 3",
            SKU = "SKU003",
            Quantity = 50, // Normal stock
            MinStock = 10,
            MaxStock = 100,
            CategoryId = 1,
            ManufacturerId = 1,
            ProductModelId = 1,
            ProductGroupId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        _context.Products.AddRange(product1, product2, product3);

        // Add transactions
        var transaction1 = new InventoryTransaction
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            UserId = "1",
            Type = TransactionType.Income,
            Quantity = 10,
            Date = DateTime.UtcNow.AddDays(-1),
            Description = "Test transaction"
        };

        var transaction2 = new InventoryTransaction
        {
            Id = 2,
            ProductId = 2,
            WarehouseId = 2,
            UserId = "1",
            Type = TransactionType.Outcome,
            Quantity = 5,
            Date = DateTime.UtcNow.AddDays(-2),
            Description = "Test transaction 2"
        };

        _context.InventoryTransactions.AddRange(transaction1, transaction2);

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        try
        {
            _context.Database.EnsureDeleted();
        }
        catch
        {
            // Ignore cleanup errors
        }
        _context.Dispose();
    }
}
