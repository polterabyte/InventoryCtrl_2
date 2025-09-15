using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.Shared.DTOs;
using Inventory.API.Models;
using Xunit;

namespace Inventory.IntegrationTests.Controllers;

public class WarehouseControllerIntegrationTests : IntegrationTestBase
{
    public WarehouseControllerIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetWarehouses_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/warehouse");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedApiResponse<WarehouseDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWarehouses_WithValidData_ShouldReturnWarehouses()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/warehouse");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedApiResponse<WarehouseDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Should().Contain(w => w.Name == "Main Warehouse");
        result.Data.Items.Should().Contain(w => w.Name == "Secondary Warehouse");
    }

    [Fact]
    public async Task GetWarehouses_WithSearchFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/warehouse?search=Main");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedApiResponse<WarehouseDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items.Should().Contain(w => w.Name == "Main Warehouse");
    }

    [Fact]
    public async Task GetWarehouses_WithIsActiveFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/warehouse?isActive=true");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedApiResponse<WarehouseDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Should().OnlyContain(w => w.IsActive);
    }

    [Fact]
    public async Task GetWarehouses_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/warehouse?page=1&pageSize=1");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedApiResponse<WarehouseDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(2);
        result.Data.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task GetWarehouse_WithValidId_ShouldReturnWarehouse()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();
        
        var warehouse = Context.Warehouses.First();

        // Act
        var response = await Client.GetAsync($"/api/warehouse/{warehouse.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(warehouse.Name);
    }

    [Fact]
    public async Task GetWarehouse_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/warehouse/999");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Warehouse not found");
    }

    [Fact]
    public async Task CreateWarehouse_WithValidData_ShouldCreateWarehouse()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();
        
        var request = new CreateWarehouseDto 
        { 
            Name = "New Warehouse", 
            Location = "New Location" 
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/warehouse", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("New Warehouse");
        result.Data.Location.Should().Be("New Location");
        result.Data.IsActive.Should().BeTrue();
        
        // Verify warehouse was created in database
        var warehouse = await Context.Warehouses.FirstOrDefaultAsync(w => w.Name == "New Warehouse");
        warehouse.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateWarehouse_WithDuplicateName_ShouldReturnBadRequest()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();
        
        var request = new CreateWarehouseDto 
        { 
            Name = "Main Warehouse", // Duplicate name
            Location = "New Location" 
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/warehouse", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Warehouse with this name already exists");
    }

    [Fact]
    public async Task UpdateWarehouse_WithValidData_ShouldUpdateWarehouse()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();
        
        var warehouse = Context.Warehouses.First();
        var request = new UpdateWarehouseDto 
        { 
            Name = "Updated Warehouse", 
            Location = "Updated Location",
            IsActive = true
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/warehouse/{warehouse.Id}", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Updated Warehouse");
        result.Data.Location.Should().Be("Updated Location");
        
        // Verify warehouse was updated in database
        Context.ChangeTracker.Clear(); // Clear EF Core change tracker
        var updatedWarehouse = await Context.Warehouses.FindAsync(warehouse.Id);
        updatedWarehouse!.Name.Should().Be("Updated Warehouse");
        updatedWarehouse.Location.Should().Be("Updated Location");
    }

    [Fact]
    public async Task UpdateWarehouse_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();
        
        var request = new UpdateWarehouseDto 
        { 
            Name = "Updated Name", 
            Location = "Updated Location",
            IsActive = true
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/warehouse/999", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Warehouse not found");
    }

    [Fact]
    public async Task DeleteWarehouse_WithValidId_ShouldSoftDeleteWarehouse()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();
        
        var warehouse = Context.Warehouses.First();

        // Act
        var response = await Client.DeleteAsync($"/api/warehouse/{warehouse.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeTrue();
        
        // Verify warehouse was soft deleted (IsActive = false)
        Context.ChangeTracker.Clear(); // Clear EF Core change tracker
        var deletedWarehouse = await Context.Warehouses.FindAsync(warehouse.Id);
        deletedWarehouse!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteWarehouse_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.DeleteAsync("/api/warehouse/999");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Warehouse not found");
    }

    [Fact]
    public async Task DeleteWarehouse_WithTransactions_ShouldReturnBadRequest()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();
        
        var warehouse = Context.Warehouses.First();
        
        // Add a transaction for this warehouse
        var category = new Category { Name = "Test Category", IsActive = true, CreatedAt = DateTime.UtcNow };
        var manufacturer = new Manufacturer { Name = "Test Manufacturer", CreatedAt = DateTime.UtcNow };
        Context.Categories.Add(category);
        Context.Manufacturers.Add(manufacturer);
        await Context.SaveChangesAsync();
        
        // Create test product group
        var productGroup = new ProductGroup
        {
            Name = "Test Group"
        };
        Context.ProductGroups.Add(productGroup);
        await Context.SaveChangesAsync();

        // Create test product model
        var productModel = new ProductModel
        {
            Name = "Test Model",
            ManufacturerId = manufacturer.Id
        };
        Context.ProductModels.Add(productModel);
        await Context.SaveChangesAsync();

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
        Context.Products.Add(product);
        await Context.SaveChangesAsync();
        
        var transaction = new InventoryTransaction
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Type = TransactionType.Income,
            Quantity = 5,
            Date = DateTime.UtcNow,
            Description = "Test transaction",
            UserId = "test-admin-1"
        };
        Context.InventoryTransactions.Add(transaction);
        await Context.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/warehouse/{warehouse.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot delete warehouse with transactions");
    }

    private new async Task SeedTestDataAsync()
    {
        var warehouses = new List<Warehouse>
        {
            new() 
            { 
                Name = "Main Warehouse", 
                Location = "Main Street 1", 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            },
            new() 
            { 
                Name = "Secondary Warehouse", 
                Location = "Secondary Street 2", 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            }
        };

        Context.Warehouses.AddRange(warehouses);
        await Context.SaveChangesAsync();
    }
}
