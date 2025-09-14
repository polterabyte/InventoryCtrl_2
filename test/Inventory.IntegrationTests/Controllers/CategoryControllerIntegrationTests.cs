using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.Shared.DTOs;
using Inventory.API.Models;
using Xunit;

namespace Inventory.IntegrationTests.Controllers;

public class CategoryControllerIntegrationTestsNew : IntegrationTestBase
{
    public CategoryControllerIntegrationTestsNew(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCategories_WithValidData_ShouldReturnCategories()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/category");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2); // Only active categories
    }

    [Fact]
    public async Task GetCategories_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/category");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategory_WithValidId_ShouldReturnCategory()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var categoryId = 1;

        // Act
        var response = await Client.GetAsync($"/api/category/{categoryId}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(categoryId);
    }

    [Fact]
    public async Task GetCategory_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();
        var categoryId = 999;

        // Act
        var response = await Client.GetAsync($"/api/category/{categoryId}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRootCategories_WithValidData_ShouldReturnRootCategories()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/category/root");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1); // Only root category
    }

    [Fact]
    public async Task GetSubCategories_WithValidParentId_ShouldReturnSubcategories()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var parentId = 1;

        // Act
        var response = await Client.GetAsync($"/api/category/{parentId}/sub");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1); // One subcategory
    }

    [Fact]
    public async Task GetSubCategories_WithInvalidParentId_ShouldReturnEmptyList()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();
        var parentId = 999;

        // Act
        var response = await Client.GetAsync($"/api/category/{parentId}/sub");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCategory_WithValidData_ShouldCreateCategory()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();
        var createRequest = new CreateCategoryDto
        {
            Name = "Test Category",
            Description = "Test Description",
            ParentCategoryId = null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/category", createRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(createRequest.Name);
    }

    [Fact]
    public async Task CreateCategory_WithInvalidParentId_ShouldReturnBadRequest()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();
        var createRequest = new CreateCategoryDto
        {
            Name = "Test Category",
            Description = "Test Description",
            ParentCategoryId = 999 // Invalid: non-existent parent
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/category", createRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateCategory_WithValidData_ShouldUpdateCategory()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var categoryId = 1;
        var updateRequest = new UpdateCategoryDto
        {
            Name = "Updated Category",
            Description = "Updated Description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/category/{categoryId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(updateRequest.Name);
    }

    [Fact]
    public async Task UpdateCategory_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();
        var categoryId = 999;
        var updateRequest = new UpdateCategoryDto
        {
            Name = "Updated Category",
            Description = "Updated Description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/category/{categoryId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteCategory_WithValidId_ShouldDeleteCategory()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();
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
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var categoryId = 4;

        // Act
        var response = await Client.DeleteAsync($"/api/category/{categoryId}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCategory_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();
        var categoryId = 999;

        // Act
        var response = await Client.DeleteAsync($"/api/category/{categoryId}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCategories_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove authentication
                services.AddControllers();
            });
        }).CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/category");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }
}

