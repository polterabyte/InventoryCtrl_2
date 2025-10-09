using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using System.Linq;
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
        var actionResult = await _controller.GetDashboardStats();

        // Assert
        actionResult.Should().NotBeNull();
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var response = okResult.Value.Should().BeOfType<ApiResponse<DashboardStatsDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardStats_WithEmptyDatabase_ShouldReturnZeroStats()
    {
        // Act
        var actionResult = await _controller.GetDashboardStats();

        // Assert
        actionResult.Should().NotBeNull();
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var response = okResult.Value.Should().BeOfType<ApiResponse<DashboardStatsDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardStats_WithError_ShouldReturnErrorResponse()
    {
        // Arrange - Dispose context to simulate database error
        _context.Database.EnsureDeleted();
        _context.Dispose();

        // Act
        var actionResult = await _controller.GetDashboardStats();

        // Assert
        actionResult.Should().NotBeNull();
        var objectResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        
        var response = objectResult.Value.Should().BeOfType<ApiResponse<DashboardStatsDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetRecentActivity_WithValidData_ShouldReturnActivity()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var actionResult = await _controller.GetRecentActivity();

        // Assert
        actionResult.Should().NotBeNull();
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var response = okResult.Value.Should().BeOfType<ApiResponse<RecentActivityDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecentActivity_WithError_ShouldReturnErrorResponse()
    {
        // Arrange - Dispose context to simulate database error
        _context.Database.EnsureDeleted();
        _context.Dispose();

        // Act
        var actionResult = await _controller.GetRecentActivity();

        // Assert
        actionResult.Should().NotBeNull();
        var objectResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        
        var response = objectResult.Value.Should().BeOfType<ApiResponse<RecentActivityDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetLowStockProducts_WithValidData_ShouldReturnLowStockProducts()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var actionResult = await _controller.GetLowStockProducts();

        // Assert
        actionResult.Should().NotBeNull();
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<LowStockProductDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data.Should().HaveCount(2);

        var productWithTwoCards = response.Data.First(p => p.ProductId == 1);
        productWithTwoCards.KanbanCards.Should().HaveCount(2);
        var secondProduct = response.Data.First(p => p.ProductId == 2);
        secondProduct.KanbanCards.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLowStockProducts_WithNoLowStockProducts_ShouldReturnEmptyList()
    {
        var category = new Category { Id = 1, Name = "Test Category", IsActive = true };
        var manufacturer = new Manufacturer { Id = 1, Name = "Test Manufacturer" };
        var warehouse = new Warehouse { Id = 1, Name = "Test Warehouse", IsActive = true };
        var user = new User { Id = "1", UserName = "testuser", Email = "test@test.com" };
        var productGroup = new ProductGroup { Id = 1, Name = "Test Group", IsActive = true };
        var productModel = new ProductModel { Id = 1, Name = "Test Model" };
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
            CategoryId = 1,
            ProductGroupId = 1,
            ProductModelId = 1,
            UnitOfMeasureId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);

        _context.KanbanCards.Add(new KanbanCard
        {
            Id = 10,
            ProductId = 1,
            WarehouseId = 1,
            MinThreshold = 5,
            MaxThreshold = 40,
            CreatedAt = DateTime.UtcNow
        });

        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = 10,
            ProductId = 1,
            WarehouseId = 1,
            UserId = "1",
            Type = TransactionType.Income,
            Quantity = 20,
            Date = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var actionResult = await _controller.GetLowStockProducts();

        actionResult.Should().NotBeNull();
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<LowStockProductDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLowStockProducts_WithError_ShouldReturnErrorResponse()
    {
        // Arrange - Dispose context to simulate database error
        _context.Database.EnsureDeleted();
        _context.Dispose();

        // Act
        var actionResult = await _controller.GetLowStockProducts();

        // Assert
        actionResult.Should().NotBeNull();
        var objectResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        
        var response = objectResult.Value.Should().BeOfType<ApiResponse<List<LowStockProductDto>>>().Subject;
        response.Success.Should().BeFalse();
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
        var productModel1 = new ProductModel { Id = 1, Name = "Model 1" };
        var productModel2 = new ProductModel { Id = 2, Name = "Model 2" };
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
            // CurrentQuantity = 5, // Low stock
            CategoryId = 1,
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
            // CurrentQuantity = 0, // Out of stock
            CategoryId = 2,

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

            // CurrentQuantity = 50, // Normal stock
            CategoryId = 1,

            ProductModelId = 1,
            ProductGroupId = 1,
            UnitOfMeasureId = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        _context.Products.AddRange(product1, product2, product3);

        // Add Kanban cards
        _context.KanbanCards.AddRange(
            new KanbanCard { Id = 1, ProductId = 1, WarehouseId = 1, MinThreshold = 10, MaxThreshold = 80, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new KanbanCard { Id = 2, ProductId = 2, WarehouseId = 2, MinThreshold = 5, MaxThreshold = 40, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new KanbanCard { Id = 3, ProductId = 1, WarehouseId = 2, MinThreshold = 8, MaxThreshold = 60, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        );

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
