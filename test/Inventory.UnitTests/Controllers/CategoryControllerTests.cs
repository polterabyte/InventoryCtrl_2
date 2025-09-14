using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Inventory.UnitTests.Controllers;

public class CategoryControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CategoryController _controller;

    public CategoryControllerTests()
    {
        // Create unique database name for this test
        var testDatabaseName = $"inventory_unit_test_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql($"Host=localhost;Database={testDatabaseName};Username=postgres;Password=postgres")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _controller = new CategoryController(_context, Mock.Of<ILogger<CategoryController>>());
        
        // Setup authentication context for tests
        SetupAuthenticationContext();
    }

    [Fact]
    public async Task GetCategories_WithValidData_ShouldReturnCategories()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetCategories();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var response = okResult.Value as PagedApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().HaveCount(3); // Admin sees all categories (active + inactive)
    }

    [Fact]
    public async Task GetCategories_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var result = await _controller.GetCategories();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        var response = okResult!.Value as PagedApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategory_WithValidId_ShouldReturnCategory()
    {
        // Arrange
        await SeedTestDataAsync();
        var categoryId = 1;

        // Act
        var result = await _controller.GetCategory(categoryId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        var response = okResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(categoryId);
    }

    [Fact]
    public async Task GetCategory_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var categoryId = 999;

        // Act
        var result = await _controller.GetCategory(categoryId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRootCategories_WithValidData_ShouldReturnRootCategories()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _controller.GetRootCategories();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        var response = okResult!.Value as ApiResponse<List<CategoryDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data.Should().HaveCount(1); // Only root category
    }

    [Fact]
    public async Task GetSubCategories_WithValidParentId_ShouldReturnSubcategories()
    {
        // Arrange
        await SeedTestDataAsync();
        var parentId = 1;

        // Act
        var result = await _controller.GetSubCategories(parentId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        var response = okResult!.Value as ApiResponse<List<CategoryDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data.Should().HaveCount(1); // One subcategory
    }

    [Fact]
    public async Task GetSubCategories_WithInvalidParentId_ShouldReturnEmptyList()
    {
        // Arrange
        var parentId = 999;

        // Act
        var result = await _controller.GetSubCategories(parentId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        var response = okResult!.Value as ApiResponse<List<CategoryDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCategory_WithValidData_ShouldCreateCategory()
    {
        // Arrange
        var createRequest = new CreateCategoryDto
        {
            Name = "Test Category",
            Description = "Test Description",
            ParentCategoryId = null
        };

        // Act
        var result = await _controller.CreateCategory(createRequest);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var response = createdResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(createRequest.Name);
    }

    [Fact]
    public async Task CreateCategory_WithInvalidParentId_ShouldReturnBadRequest()
    {
        // Arrange
        var createRequest = new CreateCategoryDto
        {
            Name = "Test Category",
            Description = "Test Description",
            ParentCategoryId = 999 // Invalid: non-existent parent
        };

        // Act
        var result = await _controller.CreateCategory(createRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateCategory_WithValidData_ShouldUpdateCategory()
    {
        // Arrange
        await SeedTestDataAsync();
        var categoryId = 1;
        var updateRequest = new UpdateCategoryDto
        {
            Name = "Updated Category",
            Description = "Updated Description"
        };

        // Act
        var result = await _controller.UpdateCategory(categoryId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        var response = okResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(updateRequest.Name);
    }

    [Fact]
    public async Task UpdateCategory_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var categoryId = 999;
        var updateRequest = new UpdateCategoryDto
        {
            Name = "Updated Category",
            Description = "Updated Description"
        };

        // Act
        var result = await _controller.UpdateCategory(categoryId, updateRequest);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteCategory_WithValidId_ShouldDeleteCategory()
    {
        // Arrange
        // Create a category without subcategories
        var category = new Category
        {
            Id = 4,
            Name = "Test Category",
            Description = "Test Description",
            IsActive = true,
            ParentCategoryId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var categoryId = 4;

        // Act
        var result = await _controller.DeleteCategory(categoryId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult ?? result as ObjectResult;
        var response = okResult!.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCategory_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var categoryId = 999;

        // Act
        var result = await _controller.DeleteCategory(categoryId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    private void SetupAuthenticationContext()
    {
        // Create a mock HttpContext with authenticated user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Role, "Admin") // Set as Admin to see all categories
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private async Task SeedTestDataAsync()
    {
        var rootCategory = new Category
        {
            Id = 1,
            Name = "Electronics",
            Description = "Electronic devices",
            IsActive = true,
            ParentCategoryId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var subCategory = new Category
        {
            Id = 2,
            Name = "Smartphones",
            Description = "Mobile phones",
            IsActive = true,
            ParentCategoryId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveCategory = new Category
        {
            Id = 3,
            Name = "Old Electronics",
            Description = "Outdated devices",
            IsActive = false,
            ParentCategoryId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Categories.AddRange(rootCategory, subCategory, inactiveCategory);
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
