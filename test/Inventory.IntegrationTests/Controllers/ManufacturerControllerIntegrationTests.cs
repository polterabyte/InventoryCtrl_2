using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.Shared.DTOs;
using Inventory.API.Models;
using Xunit;

namespace Inventory.IntegrationTests.Controllers;

public class ManufacturerControllerIntegrationTests : IntegrationTestBase
{
    public ManufacturerControllerIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetManufacturers_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/manufacturer");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ManufacturerDto>>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetManufacturers_WithValidData_ShouldReturnManufacturers()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/manufacturer");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ManufacturerDto>>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().Contain(m => m.Name == "Apple");
        result.Data.Should().Contain(m => m.Name == "Samsung");
    }

    [Fact]
    public async Task GetManufacturer_WithValidId_ShouldReturnManufacturer()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();
        
        var manufacturer = await Context.Manufacturers.FirstAsync();

        // Act
        var response = await Client.GetAsync($"/api/manufacturer/{manufacturer.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ManufacturerDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(manufacturer.Name);
    }

    [Fact]
    public async Task GetManufacturer_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/manufacturer/999");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ManufacturerDto>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Manufacturer not found");
    }

    [Fact]
    public async Task CreateManufacturer_WithValidData_ShouldCreateManufacturer()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();
        
        var request = new CreateManufacturerDto { Name = "Microsoft" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/manufacturer", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ManufacturerDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Microsoft");
        
        // Verify manufacturer was created in database
        var manufacturer = await Context.Manufacturers.FirstOrDefaultAsync(m => m.Name == "Microsoft");
        manufacturer.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateManufacturer_WithDuplicateName_ShouldReturnBadRequest()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();
        
        var request = new CreateManufacturerDto { Name = "Apple" }; // Duplicate name

        // Act
        var response = await Client.PostAsJsonAsync("/api/manufacturer", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ManufacturerDto>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Manufacturer with this name already exists");
    }

    [Fact]
    public async Task UpdateManufacturer_WithValidData_ShouldUpdateManufacturer()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();
        
        var manufacturer = await Context.Manufacturers.FirstAsync();
        var request = new UpdateManufacturerDto { Name = "Updated Apple" };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/manufacturer/{manufacturer.Id}", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ManufacturerDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Updated Apple");
        
        // Verify manufacturer was updated in database
        Context.ChangeTracker.Clear(); // Clear EF Core change tracker
        var updatedManufacturer = await Context.Manufacturers.FindAsync(manufacturer.Id);
        updatedManufacturer!.Name.Should().Be("Updated Apple");
    }

    [Fact]
    public async Task UpdateManufacturer_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();
        
        var request = new UpdateManufacturerDto { Name = "Updated Name" };

        // Act
        var response = await Client.PutAsJsonAsync("/api/manufacturer/999", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ManufacturerDto>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Manufacturer not found");
    }

    [Fact]
    public async Task DeleteManufacturer_WithValidId_ShouldDeleteManufacturer()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();
        
        // Find a manufacturer that doesn't have products
        var manufacturer = await Context.Manufacturers.FirstAsync(m => m.Name == "Samsung");

        // Act
        var response = await Client.DeleteAsync($"/api/manufacturer/{manufacturer.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeTrue();
        
        // Verify manufacturer was deleted from database
        Context.ChangeTracker.Clear(); // Clear EF Core change tracker
        var deletedManufacturer = await Context.Manufacturers.FindAsync(manufacturer.Id);
        deletedManufacturer.Should().BeNull();
    }

    [Fact]
    public async Task DeleteManufacturer_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.DeleteAsync("/api/manufacturer/999");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Manufacturer not found");
    }

    [Fact]
    public async Task DeleteManufacturer_WithProducts_ShouldReturnBadRequest()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();
        
        var manufacturerWithProduct = await Context.Manufacturers.FirstAsync(m => m.Name == "Apple");
        
        // Act
        var response = await Client.DeleteAsync($"/api/manufacturer/{manufacturerWithProduct.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot delete. Manufacturer is associated with 1 products.");
    }
}
