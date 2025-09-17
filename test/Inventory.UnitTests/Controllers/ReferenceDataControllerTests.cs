using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Inventory.API.Controllers;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Xunit;
using FluentAssertions;

namespace Inventory.UnitTests.Controllers;

public class ReferenceDataControllerTests
{
    private readonly Mock<IReferenceDataService<UnitOfMeasureDto, CreateUnitOfMeasureDto, UpdateUnitOfMeasureDto>> _mockService;
    private readonly Mock<ILogger<ReferenceDataController<UnitOfMeasureDto, CreateUnitOfMeasureDto, UpdateUnitOfMeasureDto>>> _mockLogger;
    private readonly TestReferenceDataController _controller;

    public ReferenceDataControllerTests()
    {
        _mockService = new Mock<IReferenceDataService<UnitOfMeasureDto, CreateUnitOfMeasureDto, UpdateUnitOfMeasureDto>>();
        _mockLogger = new Mock<ILogger<ReferenceDataController<UnitOfMeasureDto, CreateUnitOfMeasureDto, UpdateUnitOfMeasureDto>>>();
        _controller = new TestReferenceDataController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var search = "test";
        var isActive = true;

        var expectedResponse = new PagedApiResponse<UnitOfMeasureDto>
        {
            Success = true,
            Data = new PagedResponse<UnitOfMeasureDto>
            {
                Items = new List<UnitOfMeasureDto>
                {
                    new() { Id = 1, Name = "Test Unit", Symbol = "TU", IsActive = true }
                },
                TotalCount = 1,
                PageNumber = page,
                PageSize = pageSize
            }
        };

        _mockService.Setup(s => s.GetAllAsync(page, pageSize, search, isActive))
                   .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAll(page, pageSize, search, isActive);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetAll_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool?>()))
                   .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().BeOfType<PagedApiResponse<UnitOfMeasureDto>>();
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var id = 1;
        var expectedResponse = new ApiResponse<UnitOfMeasureDto>
        {
            Success = true,
            Data = new UnitOfMeasureDto { Id = id, Name = "Test Unit", Symbol = "TU", IsActive = true }
        };

        _mockService.Setup(s => s.GetByIdAsync(id))
                   .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        var id = 999;
        var expectedResponse = new ApiResponse<UnitOfMeasureDto>
        {
            Success = false,
            ErrorMessage = "Not found"
        };

        _mockService.Setup(s => s.GetByIdAsync(id))
                   .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateUnitOfMeasureDto
        {
            Name = "New Unit",
            Symbol = "NU",
            Description = "New unit description"
        };

        var expectedResponse = new ApiResponse<UnitOfMeasureDto>
        {
            Success = true,
            Data = new UnitOfMeasureDto { Id = 1, Name = "New Unit", Symbol = "NU", IsActive = true }
        };

        _mockService.Setup(s => s.CreateAsync(createDto))
                   .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Create_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateUnitOfMeasureDto();
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ApiResponse<UnitOfMeasureDto>>();
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateUnitOfMeasureDto
        {
            Name = "Updated Unit",
            Symbol = "UU",
            Description = "Updated description",
            IsActive = true
        };

        var expectedResponse = new ApiResponse<UnitOfMeasureDto>
        {
            Success = true,
            Data = new UnitOfMeasureDto { Id = id, Name = "Updated Unit", Symbol = "UU", IsActive = true }
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto))
                   .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Update(id, updateDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdateUnitOfMeasureDto();
        var expectedResponse = new ApiResponse<UnitOfMeasureDto>
        {
            Success = false,
            ErrorMessage = "not found"
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto))
                   .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Update(id, updateDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var id = 1;
        var expectedResponse = new ApiResponse<object>
        {
            Success = true,
            Data = new { message = "Deleted successfully" }
        };

        _mockService.Setup(s => s.DeleteAsync(id))
                   .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        var id = 999;
        var expectedResponse = new ApiResponse<object>
        {
            Success = false,
            ErrorMessage = "not found"
        };

        _mockService.Setup(s => s.DeleteAsync(id))
                   .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Exists_WithValidIdentifier_ReturnsOkResult()
    {
        // Arrange
        var identifier = "test-symbol";
        var expectedResponse = new ApiResponse<bool>
        {
            Success = true,
            Data = true
        };

        _mockService.Setup(s => s.ExistsAsync(identifier))
                   .ReturnsAsync(true);

        // Act
        var result = await _controller.Exists(identifier);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetCount_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var isActive = true;
        var expectedCount = 5;
        var expectedResponse = new ApiResponse<int>
        {
            Success = true,
            Data = expectedCount
        };

        _mockService.Setup(s => s.GetCountAsync(isActive))
                   .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetCount(isActive);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }
}

/// <summary>
/// Concrete implementation of ReferenceDataController for testing
/// </summary>
public class TestReferenceDataController : ReferenceDataController<UnitOfMeasureDto, CreateUnitOfMeasureDto, UpdateUnitOfMeasureDto>
{
    public TestReferenceDataController(
        IReferenceDataService<UnitOfMeasureDto, CreateUnitOfMeasureDto, UpdateUnitOfMeasureDto> service,
        Microsoft.Extensions.Logging.ILogger logger) : base(service, logger)
    {
    }
}