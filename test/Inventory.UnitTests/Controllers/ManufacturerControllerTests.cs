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

namespace Inventory.UnitTests.Controllers;

public class ManufacturerControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ManufacturerController _controller;
    private readonly Mock<ILogger<ManufacturerController>> _mockLogger;
    private readonly string _testDatabaseName;

    public ManufacturerControllerTests()
    {
        // Create unique database name for this test
        _testDatabaseName = $"inventory_unit_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql($"Host=localhost;Database={_testDatabaseName};Username=postgres;Password=postgres")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        
        _mockLogger = new Mock<ILogger<ManufacturerController>>();
        _controller = new ManufacturerController(_context, _mockLogger.Object);

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
    public async Task GetManufacturers_WithValidData_ShouldReturnManufacturers()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetManufacturers();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<List<ManufacturerDto>>;
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
        response.Data.Should().Contain(m => m.Name == "Apple");
        response.Data.Should().Contain(m => m.Name == "Samsung");
    }

    [Fact]
    public async Task GetManufacturers_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        await CleanupDatabaseAsync();

        // Act
        var result = await _controller.GetManufacturers();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<List<ManufacturerDto>>;
        response!.Success.Should().BeTrue();
        response.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetManufacturer_WithValidId_ShouldReturnManufacturer()
    {
        // Arrange
        await SeedTestDataAsync();
        var manufacturer = _context.Manufacturers.First();

        // Act
        var result = await _controller.GetManufacturer(manufacturer.Id);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<ManufacturerDto>;
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(manufacturer.Name);
    }

    [Fact]
    public async Task GetManufacturer_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();

        // Act
        var result = await _controller.GetManufacturer(999);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result as NotFoundObjectResult ?? result as ObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        
        var response = notFoundResult.Value as ApiResponse<ManufacturerDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Manufacturer not found");
    }

    [Fact]
    public async Task CreateManufacturer_WithValidData_ShouldCreateManufacturer()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var request = new CreateManufacturerDto { Name = "Microsoft" };

        // Act
        var result = await _controller.CreateManufacturer(request);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result as CreatedAtActionResult ?? result as ObjectResult;
        createdResult!.Value.Should().NotBeNull();
        
        var response = createdResult.Value as ApiResponse<ManufacturerDto>;
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("Microsoft");
        
        // Verify manufacturer was created in database
        var manufacturer = await _context.Manufacturers.FirstOrDefaultAsync(m => m.Name == "Microsoft");
        manufacturer.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateManufacturer_WithDuplicateName_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new CreateManufacturerDto { Name = "Apple" }; // Duplicate name

        // Act
        var result = await _controller.CreateManufacturer(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<ManufacturerDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Manufacturer with this name already exists");
    }

    [Fact]
    public async Task CreateManufacturer_WithInvalidModel_ShouldReturnBadRequest()
    {
        // Arrange
        await CleanupDatabaseAsync();
        _controller.ModelState.AddModelError("Name", "Name is required");
        var request = new CreateManufacturerDto { Name = "" };

        // Act
        var result = await _controller.CreateManufacturer(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<ManufacturerDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Invalid model state");
    }

    [Fact]
    public async Task UpdateManufacturer_WithValidData_ShouldUpdateManufacturer()
    {
        // Arrange
        await SeedTestDataAsync();
        var manufacturer = _context.Manufacturers.First();
        var request = new UpdateManufacturerDto { Name = "Updated Apple" };

        // Act
        var result = await _controller.UpdateManufacturer(manufacturer.Id, request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<ManufacturerDto>;
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("Updated Apple");
        
        // Verify manufacturer was updated in database
        var updatedManufacturer = await _context.Manufacturers.FindAsync(manufacturer.Id);
        updatedManufacturer!.Name.Should().Be("Updated Apple");
    }

    [Fact]
    public async Task UpdateManufacturer_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var request = new UpdateManufacturerDto { Name = "Updated Name" };

        // Act
        var result = await _controller.UpdateManufacturer(999, request);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result as NotFoundObjectResult ?? result as ObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        
        var response = notFoundResult.Value as ApiResponse<ManufacturerDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Manufacturer not found");
    }

    [Fact]
    public async Task UpdateManufacturer_WithDuplicateName_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var manufacturers = _context.Manufacturers.ToList();
        var request = new UpdateManufacturerDto { Name = manufacturers[1].Name }; // Duplicate name

        // Act
        var result = await _controller.UpdateManufacturer(manufacturers[0].Id, request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<ManufacturerDto>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Manufacturer with this name already exists");
    }

    [Fact]
    public async Task DeleteManufacturer_WithValidId_ShouldDeleteManufacturer()
    {
        // Arrange
        await SeedTestDataAsync();
        var manufacturer = _context.Manufacturers.First();

        // Act
        var result = await _controller.DeleteManufacturer(manufacturer.Id);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as ApiResponse<object>;
        response!.Success.Should().BeTrue();
        
        // Verify manufacturer was deleted from database
        var deletedManufacturer = await _context.Manufacturers.FindAsync(manufacturer.Id);
        deletedManufacturer.Should().BeNull();
    }

    [Fact]
    public async Task DeleteManufacturer_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();

        // Act
        var result = await _controller.DeleteManufacturer(999);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result as NotFoundObjectResult ?? result as ObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
        
        var response = notFoundResult.Value as ApiResponse<object>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Manufacturer not found");
    }

    [Fact]
    public async Task DeleteManufacturer_WithProducts_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var manufacturer = _context.Manufacturers.First();
        
        // Add a product for this manufacturer
        var category = new Category { Name = "Test Category", IsActive = true, CreatedAt = DateTime.UtcNow };
        var productGroup = new ProductGroup { Name = "Test Product Group", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(category);
        _context.ProductGroups.Add(productGroup);
        await _context.SaveChangesAsync();
        
        var productModel = new ProductModel { Name = "Test Model", ManufacturerId = manufacturer.Id };
        _context.ProductModels.Add(productModel);
        await _context.SaveChangesAsync();
        
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST001",
            Quantity = 10,
            CategoryId = category.Id,
            ManufacturerId = manufacturer.Id,
            ProductGroupId = productGroup.Id,
            ProductModelId = productModel.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteManufacturer(manufacturer.Id);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as BadRequestObjectResult ?? result as ObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        var response = badRequestResult.Value as ApiResponse<object>;
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Cannot delete manufacturer with products");
    }

    private async Task SeedTestDataAsync()
    {
        await CleanupDatabaseAsync();
        
        var manufacturers = new List<Manufacturer>
        {
            new() { Name = "Apple", CreatedAt = DateTime.UtcNow },
            new() { Name = "Samsung", CreatedAt = DateTime.UtcNow }
        };

        _context.Manufacturers.AddRange(manufacturers);
        await _context.SaveChangesAsync();
    }

    private async Task CleanupDatabaseAsync()
    {
        // Clean up in correct order to avoid foreign key constraints
        _context.Products.RemoveRange(_context.Products);
        _context.Manufacturers.RemoveRange(_context.Manufacturers);
        _context.Categories.RemoveRange(_context.Categories);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
