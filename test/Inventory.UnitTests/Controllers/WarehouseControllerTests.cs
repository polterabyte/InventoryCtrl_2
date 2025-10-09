using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Inventory.API.Controllers;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Inventory.API.Services;
using Xunit;
#pragma warning disable CS8602 // Dereference of a possibly null reference
using FluentAssertions;

namespace Inventory.UnitTests.Controllers;

public class WarehouseControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly WarehouseController _controller;
    private readonly Mock<ILogger<WarehouseController>> _mockLogger;
    private readonly string _testDatabaseName;

    public WarehouseControllerTests()
    {
        // Create unique database name for this test
        _testDatabaseName = $"inventory_unit_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_testDatabaseName)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        
        _mockLogger = new Mock<ILogger<WarehouseController>>();
        var mockUserWarehouseService = new Mock<IUserWarehouseService>();
        _controller = new WarehouseController(_context, mockUserWarehouseService.Object, _mockLogger.Object);

        // Setup authentication context for tests
        SetupAuthenticationContext();
    }

    private void SetupAuthenticationContext()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.NameIdentifier, "testuser"),
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
    public async Task GetWarehouses_WithValidData_ShouldReturnWarehouses()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetWarehouses();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<WarehouseDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(2);
        response.Data.Items.Should().Contain(w => w.Name == "Main Warehouse");
        response.Data.Items.Should().Contain(w => w.Name == "Secondary Warehouse");
    }

    [Fact]
    public async Task GetWarehouses_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        await CleanupDatabaseAsync();

        // Act
        var result = await _controller.GetWarehouses();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<WarehouseDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWarehouses_WithSearchFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetWarehouses(search: "Main");

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<WarehouseDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(1);
        response.Data.Items.Should().Contain(w => w.Name == "Main Warehouse");
    }

    [Fact]
    public async Task GetWarehouses_WithIsActiveFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetWarehouses(isActive: true);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<WarehouseDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(2);
        response.Data.Items.Should().OnlyContain(w => w.IsActive);
    }

    [Fact]
    public async Task GetWarehouses_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetWarehouses(page: 1, pageSize: 1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<WarehouseDto>;
        response!.Success.Should().BeTrue();
        response.Data.Items.Should().HaveCount(1);
        response.Data.total.Should().Be(2);
        response.Data.page.Should().Be(1);
        response.Data.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task GetWarehouse_WithValidId_ShouldReturnWarehouse()
    {
        // Arrange
        await SeedTestDataAsync();
        var warehouse = _context.Warehouses.First();

        // Act
        var result = await _controller.GetWarehouse(warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<WarehouseDto>;
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(warehouse.Name);
    }

    [Fact]
    public async Task GetWarehouse_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();

        // Act
        var result = await _controller.GetWarehouse(999);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result as NotFoundObjectResult ?? result as ObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        
        var response = notFoundResult.Value as ApiResponse<WarehouseDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Warehouse not found");
    }

    [Fact]
    public async Task CreateWarehouse_WithValidData_ShouldCreateWarehouse()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var loc = new Location { Name = "New Location", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Locations.Add(loc);
        await _context.SaveChangesAsync();
        var request = new CreateWarehouseDto 
        { 
            Name = "New Warehouse", 
            LocationId = loc.Id 
        };

        // Act
        var result = await _controller.CreateWarehouse(request);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result as CreatedAtActionResult ?? result as ObjectResult;
        createdResult!.Value.Should().NotBeNull();
        
        var response = createdResult.Value as ApiResponse<WarehouseDto>;
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("New Warehouse");
        response.Data.LocationId.Should().Be(loc.Id);
        response.Data.IsActive.Should().BeTrue();
        
        // Verify warehouse was created in database
        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "New Warehouse");
        warehouse.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateWarehouse_WithDuplicateName_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var locDup = new Location { Name = "New Location", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Locations.Add(locDup);
        await _context.SaveChangesAsync();
        var request = new CreateWarehouseDto 
        { 
            Name = "Main Warehouse", // Duplicate name
            LocationId = locDup.Id 
        };

        // Act
        var result = await _controller.CreateWarehouse(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<WarehouseDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Warehouse with this name already exists");
    }

    [Fact]
    public async Task CreateWarehouse_WithInvalidModel_ShouldReturnBadRequest()
    {
        // Arrange
        await CleanupDatabaseAsync();
        _controller.ModelState.AddModelError("Name", "Name is required");
        var request = new CreateWarehouseDto { Name = "" };

        // Act
        var result = await _controller.CreateWarehouse(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<WarehouseDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Invalid model state");
    }

    [Fact]
    public async Task UpdateWarehouse_WithValidData_ShouldUpdateWarehouse()
    {
        // Arrange
        await SeedTestDataAsync();
        var warehouse = _context.Warehouses.First();
        var updLoc = new Location { Name = "Updated Location", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Locations.Add(updLoc);
        await _context.SaveChangesAsync();
        var request = new UpdateWarehouseDto 
        { 
            Name = "Updated Warehouse", 
            LocationId = updLoc.Id,
            IsActive = true
        };

        // Act
        var result = await _controller.UpdateWarehouse(warehouse.Id, request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<WarehouseDto>;
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("Updated Warehouse");
        response.Data.LocationId.Should().Be(updLoc.Id);
        
        // Verify warehouse was updated in database
        var updatedWarehouse = await _context.Warehouses.FindAsync(warehouse.Id);
        updatedWarehouse!.Name.Should().Be("Updated Warehouse");
        updatedWarehouse.LocationId.Should().Be(updLoc.Id);
    }

    [Fact]
    public async Task UpdateWarehouse_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var request = new UpdateWarehouseDto 
        { 
            Name = "Updated Name", 
            IsActive = true
        };

        // Act
        var result = await _controller.UpdateWarehouse(999, request);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result as NotFoundObjectResult ?? result as ObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        
        var response = notFoundResult.Value as ApiResponse<WarehouseDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Warehouse not found");
    }

    [Fact]
    public async Task UpdateWarehouse_WithDuplicateName_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var warehouses = _context.Warehouses.ToList();
        var updLoc2 = new Location { Name = "Updated Location", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Locations.Add(updLoc2);
        await _context.SaveChangesAsync();
        var request = new UpdateWarehouseDto 
        { 
            Name = warehouses[1].Name, // Duplicate name
            LocationId = updLoc2.Id,
            IsActive = true
        };

        // Act
        var result = await _controller.UpdateWarehouse(warehouses[0].Id, request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<WarehouseDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Warehouse with this name already exists");
    }

    [Fact]
    public async Task DeleteWarehouse_WithValidId_ShouldSoftDeleteWarehouse()
    {
        // Arrange
        await SeedTestDataAsync();
        var warehouse = _context.Warehouses.First();

        // Act
        var result = await _controller.DeleteWarehouse(warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<object>;
        response!.Success.Should().BeTrue();
        
        // Verify warehouse was soft deleted (IsActive = false)
        var deletedWarehouse = await _context.Warehouses.FindAsync(warehouse.Id);
        deletedWarehouse!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteWarehouse_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();

        // Act
        var result = await _controller.DeleteWarehouse(999);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result as NotFoundObjectResult ?? result as ObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        
        var response = notFoundResult.Value as ApiResponse<object>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Warehouse not found");
    }

    [Fact]
    public async Task DeleteWarehouse_WithTransactions_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var warehouse = _context.Warehouses.First();
        
        // Add a transaction for this warehouse
        var category = new Category { Name = "Test Category", IsActive = true, CreatedAt = DateTime.UtcNow };
        var manufacturer = new Manufacturer { Name = "Test Manufacturer", CreatedAt = DateTime.UtcNow };
        var productGroup = new ProductGroup { Name = "Test Product Group", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(category);
        _context.Manufacturers.Add(manufacturer);
        _context.ProductGroups.Add(productGroup);
        await _context.SaveChangesAsync();
        
        var productModel = new ProductModel { Name = "Test Model" };
        _context.ProductModels.Add(productModel);
        await _context.SaveChangesAsync();
        
        var product = new Product
        {
            Name = "Test Product",
            CurrentQuantity = 10,
            CategoryId = category.Id,
            ProductGroupId = productGroup.Id,
            ProductModelId = productModel.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        var user = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com", Role = "Admin" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        var transaction = new InventoryTransaction
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Type = TransactionType.Income,
            Quantity = 5,
            Date = DateTime.UtcNow,
            Description = "Test transaction",
            UserId = user.Id
        };
        _context.InventoryTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteWarehouse(warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<object>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Cannot delete warehouse with transactions");
    }

    private async Task SeedTestDataAsync()
    {
        await CleanupDatabaseAsync();
        
        var warehouses = new List<Warehouse>
        {
            new() 
            { 
                Name = "Main Warehouse", 
                LocationId = null, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            },
            new() 
            { 
                Name = "Secondary Warehouse", 
                LocationId = null, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            }
        };

        _context.Warehouses.AddRange(warehouses);
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
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
