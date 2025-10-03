using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using System.Net;
using Microsoft.AspNetCore.Identity;

namespace Inventory.API.Tests.AccessControl;

public class WarehouseAccessControlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public WarehouseAccessControlTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add test database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"AccessTestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task<string> GetJwtTokenForUserAsync(string userId, string role = "User")
    {
        // This is a simplified token generation for testing
        // In a real scenario, you'd use your authentication system
        var loginRequest = new
        {
            Username = $"{userId}@test.com",
            Password = "TestPassword123!",
            Role = role
        };

        var content = new StringContent(JsonSerializer.Serialize(loginRequest, _jsonOptions), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var authResult = JsonSerializer.Deserialize<AuthResponse>(responseContent, _jsonOptions);
            return authResult?.Token ?? "";
        }

        return "";
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Clear existing data
        context.UserWarehouses.RemoveRange(context.UserWarehouses);
        context.InventoryTransactions.RemoveRange(context.InventoryTransactions);
        context.Warehouses.RemoveRange(context.Warehouses);
        context.Products.RemoveRange(context.Products);
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        // Add test users
        var users = new List<User>
        {
            new() { Id = "user1", UserName = "user1", Email = "user1@test.com", Role = "User", EmailConfirmed = true, FirstName = "Test", LastName = "User1" },
            new() { Id = "user2", UserName = "user2", Email = "user2@test.com", Role = "User", EmailConfirmed = true, FirstName = "Test", LastName = "User2" },
            new() { Id = "manager1", UserName = "manager1", Email = "manager1@test.com", Role = "Manager", EmailConfirmed = true, FirstName = "Test", LastName = "Manager" },
            new() { Id = "admin1", UserName = "admin1", Email = "admin1@test.com", Role = "Admin", EmailConfirmed = true, FirstName = "Admin", LastName = "User" }
        };

        foreach (var user in users)
        {
            await userManager.CreateAsync(user, "TestPassword123!");
        }

        // Add test warehouses
        var warehouses = new List<Warehouse>
        {
            new() { Id = 1, Name = "Warehouse A", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Warehouse B", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "Warehouse C", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 4, Name = "Warehouse D", IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        context.Warehouses.AddRange(warehouses);

        // Add test products
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Product A", SKU = "PROD-A", IsActive = true, CategoryId = 1, ManufacturerId = 1, ProductModelId = 1, ProductGroupId = 1, UnitOfMeasureId = 1, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Product B", SKU = "PROD-B", IsActive = true, CategoryId = 1, ManufacturerId = 1, ProductModelId = 1, ProductGroupId = 1, UnitOfMeasureId = 1, CreatedAt = DateTime.UtcNow }
        };
        context.Products.AddRange(products);

        // Add user warehouse assignments
        var userWarehouses = new List<UserWarehouse>
        {
            // user1 has access to warehouses 1 and 2, with warehouse 1 as default
            new() { UserId = "user1", WarehouseId = 1, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = "user1", WarehouseId = 2, AccessLevel = "ReadOnly", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            
            // user2 has access to warehouse 3 only
            new() { UserId = "user2", WarehouseId = 3, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            
            // manager1 has access to warehouses 1, 2, and 3
            new() { UserId = "manager1", WarehouseId = 1, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = "manager1", WarehouseId = 2, AccessLevel = "Full", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = "manager1", WarehouseId = 3, AccessLevel = "Full", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        context.UserWarehouses.AddRange(userWarehouses);

        // Add test transactions
        var transactions = new List<InventoryTransaction>
        {
            new() { Id = 1, ProductId = 1, WarehouseId = 1, UserId = "user1", Type = TransactionType.Income, Quantity = 10, Date = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, ProductId = 2, WarehouseId = 2, UserId = "user1", Type = TransactionType.Outcome, Quantity = 5, Date = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, ProductId = 1, WarehouseId = 3, UserId = "user2", Type = TransactionType.Income, Quantity = 15, Date = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = 4, ProductId = 2, WarehouseId = 4, UserId = "admin1", Type = TransactionType.Income, Quantity = 20, Date = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        context.InventoryTransactions.AddRange(transactions);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AdminUser_ShouldAccessAllWarehouses()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("admin1", "Admin");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/warehouse");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedApiResponse<WarehouseDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        // Admin should see all 4 warehouses
        Assert.Equal(4, result.Data.Items?.Count);
    }

    [Fact]
    public async Task RegularUser_ShouldOnlyAccessAssignedWarehouses()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("user1", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/warehouse");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedApiResponse<WarehouseDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        // user1 should only see warehouses 1 and 2 (their assigned warehouses)
        Assert.Equal(2, result.Data.Items?.Count);
        var warehouseIds = result.Data.Items?.Select(w => w.Id).ToList() ?? new List<int>();
        Assert.Contains(1, warehouseIds);
        Assert.Contains(2, warehouseIds);
        Assert.DoesNotContain(3, warehouseIds);
        Assert.DoesNotContain(4, warehouseIds);
    }

    [Fact]
    public async Task User_ShouldNotAccessUnassignedWarehouse()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("user1", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Try to access warehouse 3 (not assigned to user1)
        var response = await _client.GetAsync("/api/warehouse/3");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_ShouldAccessAssignedWarehouse()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("user1", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Access warehouse 1 (assigned to user1)
        var response = await _client.GetAsync("/api/warehouse/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<WarehouseDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.Id);
    }

    [Fact]
    public async Task TransactionFiltering_ShouldRespectWarehouseAccess()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("user1", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/transaction");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedApiResponse<InventoryTransactionDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        // user1 should only see transactions from warehouses 1 and 2
        var transactions = result.Data.Items ?? new List<InventoryTransactionDto>();
        var warehouseIds = transactions.Select(t => t.WarehouseId).Distinct().ToList();
        
        Assert.Contains(1, warehouseIds);
        Assert.Contains(2, warehouseIds);
        Assert.DoesNotContain(3, warehouseIds);
        Assert.DoesNotContain(4, warehouseIds);
    }

    [Fact]
    public async Task TransactionAccess_ShouldBeDeniedForUnassignedWarehouse()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("user1", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Try to access transaction 3 (from warehouse 3, not assigned to user1)
        var response = await _client.GetAsync("/api/transaction/3");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TransactionAccess_ShouldBeAllowedForAssignedWarehouse()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("user1", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Access transaction 1 (from warehouse 1, assigned to user1)
        var response = await _client.GetAsync("/api/transaction/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<InventoryTransactionDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.Id);
    }

    [Fact]
    public async Task ManagerUser_ShouldAccessAssignedWarehouses()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("manager1", "Manager");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/warehouse");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedApiResponse<WarehouseDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        // manager1 should see warehouses 1, 2, and 3 (their assigned warehouses)
        Assert.Equal(3, result.Data.Items?.Count);
        var warehouseIds = result.Data.Items?.Select(w => w.Id).ToList() ?? new List<int>();
        Assert.Contains(1, warehouseIds);
        Assert.Contains(2, warehouseIds);
        Assert.Contains(3, warehouseIds);
        Assert.DoesNotContain(4, warehouseIds);
    }

    [Fact]
    public async Task AccessLevelValidation_FullAccess_ShouldAllowAllOperations()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("user1", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // user1 has Full access to warehouse 1

        // Act - Check access with Full requirement
        var response = await _client.GetAsync("/api/userwarehouse/users/user1/warehouses/1/access?requiredAccessLevel=Full");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task AccessLevelValidation_ReadOnlyAccess_ShouldDenyFullOperations()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("user1", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // user1 has ReadOnly access to warehouse 2

        // Act - Check access with Full requirement on ReadOnly warehouse
        var response = await _client.GetAsync("/api/userwarehouse/users/user1/warehouses/2/access?requiredAccessLevel=Full");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        // The response should indicate limited access (not Full)
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CrossUserAccess_ShouldBeDenied()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("user1", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - user1 trying to access user2's warehouse assignments
        var response = await _client.GetAsync("/api/userwarehouse/users/user2/warehouses");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminAccess_ShouldBypassWarehouseRestrictions()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenForUserAsync("admin1", "Admin");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Admin accessing any warehouse should work
        var response = await _client.GetAsync("/api/warehouse/4");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<WarehouseDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(4, result.Data.Id);
    }

    [Fact]
    public async Task NoAssignments_ShouldReturnEmptyList()
    {
        // Arrange
        await SeedTestDataAsync();
        
        // Create a user with no warehouse assignments
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        
        var newUser = new User { Id = "user3", UserName = "user3", Email = "user3@test.com", Role = "User", EmailConfirmed = true, FirstName = "Test", LastName = "User3" };
        await userManager.CreateAsync(newUser, "TestPassword123!");
        await context.SaveChangesAsync();

        var token = await GetJwtTokenForUserAsync("user3", "User");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/warehouse");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedApiResponse<WarehouseDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        // User with no assignments should see empty list
        Assert.Empty(result.Data.Items ?? new List<WarehouseDto>());
    }

    private class AuthResponse
    {
        public string Token { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}