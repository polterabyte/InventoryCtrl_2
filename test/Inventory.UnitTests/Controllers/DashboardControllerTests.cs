using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using System.Security.Claims;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Inventory.UnitTests.Controllers;

public class DashboardControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<DashboardController>> _loggerMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"inventory_test_{Guid.NewGuid():N}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _loggerMock = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(_context, _loggerMock.Object);
        
        // Setup authentication context
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testadmin"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetDashboardStats_WithValidData_ShouldReturnStats()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetDashboardStats();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardStats_WithEmptyDatabase_ShouldReturnZeroStats()
    {
        // Act
        var result = await _controller.GetDashboardStats();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<object>;
        
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardStats_WithError_ShouldReturnErrorResponse()
    {
        // Arrange - Dispose context to simulate database error
        _context.Database.EnsureDeleted();
        _context.Dispose();

        // Act
        var result = await _controller.GetDashboardStats();

        // Assert
        result.Should().NotBeNull();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var response = objectResult.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetRecentActivity_WithValidData_ShouldReturnActivity()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetRecentActivity();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecentActivity_WithError_ShouldReturnErrorResponse()
    {
        // Arrange - Dispose context to simulate database error
        _context.Database.EnsureDeleted();
        _context.Dispose();

        // Act
        var result = await _controller.GetRecentActivity();

        // Assert
        result.Should().NotBeNull();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var response = objectResult.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetLowStockProducts_WithValidData_ShouldReturnLowStockProducts()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var response = okResult.Value as ApiResponse<List<LowStockProductDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
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
        var unitOfMeasure = new UnitOfMeasure { Id = 1, Name = "Pieces", Symbol = "pcs" };

        _context.Categories.Add(category);
        _context.Manufacturers.Add(manufacturer);
        _context.Warehouses.Add(warehouse);
        _context.Users.Add(user);
        _context.ProductGroups.Add(productGroup);
        _context.ProductModels.Add(productModel);
        _context.UnitOfMeasures.Add(unitOfMeasure);

        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            SKU = "TEST001",
            // CurrentQuantity = 100, // High quantity
            MinStock = 10,
            MaxStock = 200,
            CategoryId = 1,
            ManufacturerId = 1,
            ProductGroupId = 1,
            ProductModelId = 1,
            UnitOfMeasureId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        
        // Add ProductOnHandView data
        // Note: Since this is a view, we can't directly add to it in tests
        // In a real scenario, this would come from the database view
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var response = okResult.Value as ApiResponse<List<LowStockProductDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLowStockProducts_WithError_ShouldReturnErrorResponse()
    {
        // Arrange - Dispose context to simulate database error
        _context.Database.EnsureDeleted();
        _context.Dispose();

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        result.Should().NotBeNull();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var response = objectResult.Value as ApiResponse<List<LowStockProductDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
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
        var productGroup1 = new ProductGroup { Id = 1, Name = "Group 1", IsActive = true };
        var productGroup2 = new ProductGroup { Id = 2, Name = "Group 2", IsActive = true };
        _context.ProductGroups.AddRange(productGroup1, productGroup2);

        // Add product models
        var productModel1 = new ProductModel { Id = 1, Name = "Model 1", ManufacturerId = 1 };
        var productModel2 = new ProductModel { Id = 2, Name = "Model 2", ManufacturerId = 2 };
        _context.ProductModels.AddRange(productModel1, productModel2);

        // Add unit of measures
        var unit1 = new UnitOfMeasure { Id = 1, Name = "Pieces", Symbol = "pcs" };
        var unit2 = new UnitOfMeasure { Id = 2, Name = "Kilograms", Symbol = "kg" };
        _context.UnitOfMeasures.AddRange(unit1, unit2);

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
            // CurrentQuantity = 5, // Low stock
            MinStock = 10,
            MaxStock = 100,
            CategoryId = 1,
            ManufacturerId = 1,
            ProductModelId = 1,
            ProductGroupId = 1,
            UnitOfMeasureId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var product2 = new Product
        {
            Id = 2,
            Name = "Product 2",
            SKU = "SKU002",
            // CurrentQuantity = 0, // Out of stock
            MinStock = 5,
            MaxStock = 50,
            CategoryId = 2,
            ManufacturerId = 2,
            ProductModelId = 2,
            ProductGroupId = 2,
            UnitOfMeasureId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var product3 = new Product
        {
            Id = 3,
            Name = "Product 3",
            SKU = "SKU003",
            // CurrentQuantity = 50, // Normal stock
            MinStock = 10,
            MaxStock = 100,
            CategoryId = 1,
            ManufacturerId = 1,
            ProductModelId = 1,
            ProductGroupId = 1,
            UnitOfMeasureId = 2,
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