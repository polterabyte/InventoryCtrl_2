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
        var manufacturers = new List<Manufacturer>
        {
            new() { Id = 1, Name = "Test Manufacturer 1", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Test Manufacturer 2", CreatedAt = DateTime.UtcNow }
        };

        _context.Manufacturers.AddRange(manufacturers);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetManufacturers();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<List<ManufacturerDto>>>();
    }

    [Fact]
    public async Task GetManufacturer_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var id = 1;
        var manufacturer = new Manufacturer { Id = id, Name = "Test Manufacturer", CreatedAt = DateTime.UtcNow };

        _context.Manufacturers.Add(manufacturer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetManufacturer(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<ManufacturerDto>>();
    }

    [Fact]
    public async Task GetManufacturer_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;

        // Act
        var result = await _controller.GetManufacturer(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateManufacturer_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createRequest = new CreateManufacturerDto
        {
            Name = "New Manufacturer",
            Description = "Test Description"
        };

        // Act
        var result = await _controller.CreateManufacturer(createRequest);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.Value.Should().BeOfType<ApiResponse<ManufacturerDto>>();
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