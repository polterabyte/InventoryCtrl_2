using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Inventory.API.Controllers;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Xunit;
using FluentAssertions;

namespace Inventory.UnitTests.Controllers;

public class ManufacturerControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<ManufacturerController>> _mockLogger;
    private readonly ManufacturerController _controller;
    private readonly string _testDatabaseName;

    public ManufacturerControllerTests()
    {
        // Create unique database name for this test
        _testDatabaseName = $"inventory_unit_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_testDatabaseName)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        
        _mockLogger = new Mock<ILogger<ManufacturerController>>();
        _controller = new ManufacturerController(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetManufacturers_ReturnsOkResult()
    {
        // Arrange
        var location = new Location { Id = 1, Name = "Test Location" };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        var manufacturers = new List<Manufacturer>
        {
            new() { Id = 1, Name = "Test Manufacturer 1", CreatedAt = DateTime.UtcNow, LocationId = 1 },
            new() { Id = 2, Name = "Test Manufacturer 2", CreatedAt = DateTime.UtcNow, LocationId = 1 }
        };

        _context.Manufacturers.AddRange(manufacturers);
        await _context.SaveChangesAsync();

        // Act
        var actionResult = await _controller.GetManufacturers();

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<List<ManufacturerDto>>>();
        var apiResponse = okResult.Value as ApiResponse<List<ManufacturerDto>>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetManufacturer_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var location = new Location { Id = 1, Name = "Test Location" };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();
        
        var id = 1;
        var manufacturer = new Manufacturer { Id = id, Name = "Test Manufacturer", CreatedAt = DateTime.UtcNow, LocationId = 1 };

        _context.Manufacturers.Add(manufacturer);
        await _context.SaveChangesAsync();

        // Act
        var actionResult = await _controller.GetManufacturer(id);

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<ManufacturerDto>>();
        var apiResponse = okResult.Value as ApiResponse<ManufacturerDto>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetManufacturer_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;

        // Act
        var actionResult = await _controller.GetManufacturer(id);

        // Assert
        actionResult.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = actionResult.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeOfType<ApiResponse<ManufacturerDto>>();
        var apiResponse = notFoundResult.Value as ApiResponse<ManufacturerDto>;
        apiResponse!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateManufacturer_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var location = new Location { Id = 1, Name = "Test Location", IsActive = true };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        var createRequest = new CreateManufacturerDto
        {
            Name = "New Manufacturer",
            Description = "Test Description",
            LocationId = 1
        };

        // Act
        var actionResult = await _controller.CreateManufacturer(createRequest);

        // Assert
        actionResult.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeOfType<ApiResponse<ManufacturerDto>>();
        var apiResponse = createdResult.Value as ApiResponse<ManufacturerDto>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Name.Should().Be(createRequest.Name);
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