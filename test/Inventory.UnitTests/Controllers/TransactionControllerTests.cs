using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Inventory.API.Controllers;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Xunit;
using FluentAssertions;
using InventoryTransactionDto = Inventory.Shared.DTOs.InventoryTransactionDto;
using CreateInventoryTransactionDto = Inventory.Shared.DTOs.CreateInventoryTransactionDto;

namespace Inventory.UnitTests.Controllers;

public class TransactionControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TransactionController _controller;
    private readonly Mock<ILogger<TransactionController>> _mockLogger;
    private readonly string _testDatabaseName;
    private readonly string _testUserId;

    public TransactionControllerTests()
    {
        // Create unique database name for this test
        _testDatabaseName = $"inventory_unit_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        // Create consistent test user ID
        _testUserId = Guid.NewGuid().ToString();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql($"Host=localhost;Database={_testDatabaseName};Username=postgres;Password=postgres")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        
        _mockLogger = new Mock<ILogger<TransactionController>>();
        _controller = new TransactionController(_context, _mockLogger.Object);

        // Setup authentication context for tests
        SetupAuthenticationContext();
    }

    private void SetupAuthenticationContext()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.NameIdentifier, _testUserId),
            new(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    [Fact]
    public async Task GetTransactions_WithValidData_ShouldReturnTransactions()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetTransactions();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<InventoryTransactionDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(2);
        response.Data.Items.Should().Contain(t => t.Type == "Income");
        response.Data.Items.Should().Contain(t => t.Type == "Outcome");
    }

    [Fact]
    public async Task GetTransactions_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        await CleanupDatabaseAsync();

        // Act
        var result = await _controller.GetTransactions();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactions_WithProductFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var product = _context.Products.First();

        // Act
        var result = await _controller.GetTransactions(productId: product.Id);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(2); // Ожидаем 2 транзакции для продукта
        response.Data.Items.Should().OnlyContain(t => t.ProductId == product.Id);
    }

    [Fact]
    public async Task GetTransactions_WithWarehouseFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var warehouse = _context.Warehouses.First();

        // Act
        var result = await _controller.GetTransactions(warehouseId: warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(2); // Ожидаем 2 транзакции для склада
        response.Data.Items.Should().OnlyContain(t => t.WarehouseId == warehouse.Id);
    }

    [Fact]
    public async Task GetTransactions_WithTypeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetTransactions(type: "Income");

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(1);
        response.Data.Items.Should().OnlyContain(t => t.Type == "Income");
    }

    [Fact]
    public async Task GetTransactions_WithDateFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = await _controller.GetTransactions(startDate: startDate, endDate: endDate);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactions_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetTransactions(page: 1, pageSize: 1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(1);
        response.Data.TotalCount.Should().Be(2);
        response.Data.PageNumber.Should().Be(1);
        response.Data.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task GetTransaction_WithValidId_ShouldReturnTransaction()
    {
        // Arrange
        await SeedTestDataAsync();
        var transaction = _context.InventoryTransactions.First();

        // Act
        var result = await _controller.GetTransaction(transaction.Id);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(transaction.Id);
        response.Data.ProductName.Should().NotBeNullOrEmpty();
        response.Data.WarehouseName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTransaction_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();

        // Act
        var result = await _controller.GetTransaction(999);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result as NotFoundObjectResult ?? result as ObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        
        var response = notFoundResult.Value as ApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Transaction not found");
    }

    [Fact]
    public async Task GetTransactionsByProduct_WithValidProductId_ShouldReturnTransactions()
    {
        // Arrange
        await SeedTestDataAsync();
        var product = _context.Products.First();

        // Act
        var result = await _controller.GetTransactionsByProduct(product.Id);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(2); // Ожидаем 2 транзакции для продукта
        response.Data.Items.Should().OnlyContain(t => t.ProductId == product.Id);
    }

    [Fact]
    public async Task GetTransactionsByProduct_WithInvalidProductId_ShouldReturnEmptyList()
    {
        // Arrange
        await CleanupDatabaseAsync();

        // Act
        var result = await _controller.GetTransactionsByProduct(999);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTransaction_WithValidIncomeTransaction_ShouldCreateTransaction()
    {
        // Arrange - создаем изолированные данные для этого теста
        await CleanupDatabaseAsync();
        await SeedSingleProductAsync();
        
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var request = new CreateInventoryTransactionDto
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Type = "Income",
            Quantity = 10,
            Description = "Test income transaction"
        };

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result as CreatedAtActionResult ?? result as ObjectResult;
        createdResult!.Value.Should().NotBeNull();
        
        var response = createdResult.Value as ApiResponse<InventoryTransactionDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Type.Should().Be("Income");
        response.Data.Quantity.Should().Be(10);
        
        // Verify transaction was created in database
        var transaction = await _context.InventoryTransactions.FirstOrDefaultAsync(t => t.Description == "Test income transaction");
        transaction.Should().NotBeNull();
        
        // Verify product quantity was updated
        var updatedProduct = await _context.Products.FindAsync(product.Id);
        updatedProduct!.Quantity.Should().Be(110); // 100 + 10
    }

    [Fact]
    public async Task CreateTransaction_WithValidOutcomeTransaction_ShouldCreateTransaction()
    {
        // Arrange - создаем изолированные данные для этого теста
        await CleanupDatabaseAsync();
        await SeedSingleProductAsync();
        
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var request = new CreateInventoryTransactionDto
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Type = "Outcome",
            Quantity = 5,
            Description = "Test outcome transaction"
        };

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result as CreatedAtActionResult ?? result as ObjectResult;
        createdResult!.Value.Should().NotBeNull();
        
        var response = createdResult.Value as ApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Type.Should().Be("Outcome");
        response.Data.Quantity.Should().Be(5);
        
        // Verify product quantity was updated
        var updatedProduct = await _context.Products.FindAsync(product.Id);
        updatedProduct!.Quantity.Should().Be(95); // 100 - 5
    }

    [Fact]
    public async Task CreateTransaction_WithInsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var request = new CreateInventoryTransactionDto
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Type = "Outcome",
            Quantity = product.Quantity + 100, // More than available
            Description = "Test insufficient stock transaction"
        };

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Insufficient stock for this transaction");
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidProductId_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var warehouse = _context.Warehouses.First();
        var request = new CreateInventoryTransactionDto
        {
            ProductId = 999, // Invalid product ID
            WarehouseId = warehouse.Id,
            Type = "Income",
            Quantity = 10,
            Description = "Test invalid product transaction"
        };

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Product not found");
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidWarehouseId_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var product = _context.Products.First();
        var request = new CreateInventoryTransactionDto
        {
            ProductId = product.Id,
            WarehouseId = 999, // Invalid warehouse ID
            Type = "Income",
            Quantity = 10,
            Description = "Test invalid warehouse transaction"
        };

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Warehouse not found");
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidLocationId_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var request = new CreateInventoryTransactionDto
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            LocationId = 999, // Invalid location ID
            Type = "Income",
            Quantity = 10,
            Description = "Test invalid location transaction"
        };

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Location not found");
    }

    [Fact]
    public async Task CreateTransaction_WithInstallType_ShouldNotChangeQuantity()
    {
        // Arrange
        await SeedTestDataAsync();
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var originalQuantity = product.Quantity;
        var request = new CreateInventoryTransactionDto
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Type = "Install",
            Quantity = 5,
            Description = "Test install transaction"
        };

        // Act
        var result = await _controller.CreateTransaction(request);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result as CreatedAtActionResult ?? result as ObjectResult;
        createdResult!.Value.Should().NotBeNull();
        
        var response = createdResult.Value as ApiResponse<InventoryTransactionDto>;
        response!.Success.Should().BeTrue();
        
        // Verify product quantity was not changed for Install type
        var updatedProduct = await _context.Products.FindAsync(product.Id);
        updatedProduct!.Quantity.Should().Be(originalQuantity);
    }

    private async Task SeedTestDataAsync()
    {
        await CleanupDatabaseAsync();
        
        // Create test data
        var category = new Category { Name = "Test Category", IsActive = true, CreatedAt = DateTime.UtcNow };
        var manufacturer = new Manufacturer { Name = "Test Manufacturer", CreatedAt = DateTime.UtcNow };
        var warehouse = new Warehouse { Name = "Test Warehouse", Location = "Test Location", IsActive = true, CreatedAt = DateTime.UtcNow };
        var user = new User { Id = _testUserId, UserName = "testuser", Email = "test@example.com", Role = "Admin" };
        var productGroup = new ProductGroup { Name = "Test Product Group", IsActive = true, CreatedAt = DateTime.UtcNow };
        
        _context.Categories.Add(category);
        _context.Manufacturers.Add(manufacturer);
        _context.Warehouses.Add(warehouse);
        _context.Users.Add(user);
        _context.ProductGroups.Add(productGroup);
        await _context.SaveChangesAsync();
        
        var productModel = new ProductModel { Name = "Test Model", ManufacturerId = manufacturer.Id };
        _context.ProductModels.Add(productModel);
        await _context.SaveChangesAsync();
        
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST001",
            Quantity = 100,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            ProductGroupId = productGroup.Id,
            ProductModelId = productModel.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        var transactions = new List<InventoryTransaction>
        {
            new()
            {
                ProductId = product.Id,
                WarehouseId = warehouse.Id,
                UserId = user.Id,
                Type = TransactionType.Income,
                Quantity = 10,
                Date = DateTime.UtcNow,
                Description = "Test income transaction"
            },
            new()
            {
                ProductId = product.Id,
                WarehouseId = warehouse.Id,
                UserId = user.Id,
                Type = TransactionType.Outcome,
                Quantity = 5,
                Date = DateTime.UtcNow,
                Description = "Test outcome transaction"
            }
        };
        
        _context.InventoryTransactions.AddRange(transactions);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSingleProductAsync()
    {
        await CleanupDatabaseAsync();
        
        // Create test data without transactions
        var category = new Category { Name = "Test Category", IsActive = true, CreatedAt = DateTime.UtcNow };
        var manufacturer = new Manufacturer { Name = "Test Manufacturer", CreatedAt = DateTime.UtcNow };
        var warehouse = new Warehouse { Name = "Test Warehouse", Location = "Test Location", IsActive = true, CreatedAt = DateTime.UtcNow };
        var user = new User { Id = _testUserId, UserName = "testuser", Email = "test@example.com", Role = "Admin" };
        var productGroup = new ProductGroup { Name = "Test Product Group", IsActive = true, CreatedAt = DateTime.UtcNow };
        
        _context.Categories.Add(category);
        _context.Manufacturers.Add(manufacturer);
        _context.Warehouses.Add(warehouse);
        _context.Users.Add(user);
        _context.ProductGroups.Add(productGroup);
        await _context.SaveChangesAsync();
        
        var productModel = new ProductModel { Name = "Test Model", ManufacturerId = manufacturer.Id };
        _context.ProductModels.Add(productModel);
        await _context.SaveChangesAsync();
        
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST001",
            Quantity = 100,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            ProductGroupId = productGroup.Id,
            ProductModelId = productModel.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    private async Task CleanupDatabaseAsync()
    {
        // Clean up in correct order to avoid foreign key constraints
        _context.InventoryTransactions.RemoveRange(_context.InventoryTransactions);
        _context.Products.RemoveRange(_context.Products);
        _context.Warehouses.RemoveRange(_context.Warehouses);
        _context.Categories.RemoveRange(_context.Categories);
        _context.Manufacturers.RemoveRange(_context.Manufacturers);
        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
