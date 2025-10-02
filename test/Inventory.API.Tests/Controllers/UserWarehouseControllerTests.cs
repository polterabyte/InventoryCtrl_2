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

namespace Inventory.API.Tests.Controllers;

public class UserWarehouseControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public UserWarehouseControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task<string> GetJwtTokenAsync()
    {
        // This is a simplified token generation for testing
        // In a real scenario, you'd use your authentication system
        var loginRequest = new
        {
            Username = "admin@test.com",
            Password = "TestPassword123!"
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
        context.Warehouses.RemoveRange(context.Warehouses);
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        // Add test users
        var users = new List<User>
        {
            new() { Id = "user1", UserName = "testuser1", Email = "user1@test.com", Role = "User", EmailConfirmed = true, FirstName = "Test", LastName = "User1" },
            new() { Id = "user2", UserName = "testuser2", Email = "user2@test.com", Role = "Manager", EmailConfirmed = true, FirstName = "Test", LastName = "User2" },
            new() { Id = "admin1", UserName = "admin1", Email = "admin@test.com", Role = "Admin", EmailConfirmed = true, FirstName = "Admin", LastName = "User" }
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
            new() { Id = 3, Name = "Warehouse C", IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        context.Warehouses.AddRange(warehouses);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AssignWarehouseToUser_ValidRequest_ShouldSucceed()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var assignmentDto = new AssignWarehouseDto
        {
            WarehouseId = 1,
            AccessLevel = "Full",
            IsDefault = true
        };

        var content = new StringContent(JsonSerializer.Serialize(assignmentDto, _jsonOptions), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/userwarehouse/users/user1/warehouses", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<UserWarehouseDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("user1", result.Data.UserId);
        Assert.Equal(1, result.Data.WarehouseId);
        Assert.Equal("Full", result.Data.AccessLevel);
        Assert.True(result.Data.IsDefault);
    }

    [Fact]
    public async Task AssignWarehouseToUser_DuplicateAssignment_ShouldFail()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create existing assignment
        var existingAssignment = new UserWarehouse
        {
            UserId = "user1",
            WarehouseId = 1,
            AccessLevel = "ReadOnly",
            IsDefault = false,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.UserWarehouses.Add(existingAssignment);
        await context.SaveChangesAsync();

        var assignmentDto = new AssignWarehouseDto
        {
            WarehouseId = 1,
            AccessLevel = "Full",
            IsDefault = false
        };

        var content = new StringContent(JsonSerializer.Serialize(assignmentDto, _jsonOptions), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/userwarehouse/users/user1/warehouses", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("already assigned", result.ErrorMessage);
    }

    [Fact]
    public async Task GetUserWarehouses_ValidUser_ShouldReturnAssignments()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create test assignments
        var assignments = new List<UserWarehouse>
        {
            new() { UserId = "user1", WarehouseId = 1, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = "user1", WarehouseId = 2, AccessLevel = "ReadOnly", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        context.UserWarehouses.AddRange(assignments);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/userwarehouse/users/user1/warehouses");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<UserWarehouseDto>>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, x => x.WarehouseId == 1 && x.IsDefault);
        Assert.Contains(result.Data, x => x.WarehouseId == 2 && !x.IsDefault);
    }

    [Fact]
    public async Task RemoveWarehouseAssignment_ValidRequest_ShouldSucceed()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create test assignments (multiple so removal doesn't fail due to last assignment rule)
        var assignments = new List<UserWarehouse>
        {
            new() { UserId = "user1", WarehouseId = 1, AccessLevel = "Full", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = "user1", WarehouseId = 2, AccessLevel = "ReadOnly", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        context.UserWarehouses.AddRange(assignments);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync("/api/userwarehouse/users/user1/warehouses/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);

        // Verify assignment is removed from database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var removedAssignment = await verifyContext.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == "user1" && uw.WarehouseId == 1);
        Assert.Null(removedAssignment);
    }

    [Fact]
    public async Task UpdateWarehouseAssignment_ValidRequest_ShouldSucceed()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create test assignment
        var assignment = new UserWarehouse
        {
            UserId = "user1",
            WarehouseId = 1,
            AccessLevel = "ReadOnly",
            IsDefault = false,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.UserWarehouses.Add(assignment);
        await context.SaveChangesAsync();

        var updateDto = new UpdateWarehouseAssignmentDto
        {
            AccessLevel = "Full",
            IsDefault = true
        };

        var content = new StringContent(JsonSerializer.Serialize(updateDto, _jsonOptions), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/api/userwarehouse/users/user1/warehouses/1", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<UserWarehouseDto>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Full", result.Data.AccessLevel);
        Assert.True(result.Data.IsDefault);
    }

    [Fact]
    public async Task SetDefaultWarehouse_ValidRequest_ShouldSucceed()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create test assignments
        var assignments = new List<UserWarehouse>
        {
            new() { UserId = "user1", WarehouseId = 1, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = "user1", WarehouseId = 2, AccessLevel = "ReadOnly", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        context.UserWarehouses.AddRange(assignments);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.PutAsync("/api/userwarehouse/users/user1/warehouses/2/default", new StringContent("", Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);

        // Verify in database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var oldDefault = await verifyContext.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == "user1" && uw.WarehouseId == 1);
        Assert.NotNull(oldDefault);
        Assert.False(oldDefault.IsDefault);

        var newDefault = await verifyContext.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == "user1" && uw.WarehouseId == 2);
        Assert.NotNull(newDefault);
        Assert.True(newDefault.IsDefault);
    }

    [Fact]
    public async Task GetWarehouseUsers_ValidWarehouse_ShouldReturnUsers()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create test assignments
        var assignments = new List<UserWarehouse>
        {
            new() { UserId = "user1", WarehouseId = 1, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = "user2", WarehouseId = 1, AccessLevel = "ReadOnly", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        context.UserWarehouses.AddRange(assignments);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/userwarehouse/warehouses/1/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<UserWarehouseDto>>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, x => x.UserId == "user1");
        Assert.Contains(result.Data, x => x.UserId == "user2");
    }

    [Fact]
    public async Task BulkAssignUsersToWarehouse_ValidRequest_ShouldSucceed()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var bulkAssignDto = new BulkAssignWarehousesDto
        {
            UserIds = new List<string> { "user1", "user2" },
            WarehouseId = 1,
            AccessLevel = "Full",
            SetAsDefault = false
        };

        var content = new StringContent(JsonSerializer.Serialize(bulkAssignDto, _jsonOptions), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/userwarehouse/warehouses/bulk-assign", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);

        // Verify in database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var assignments = await verifyContext.UserWarehouses
            .Where(uw => uw.WarehouseId == 1)
            .ToListAsync();
        
        Assert.Equal(2, assignments.Count);
        Assert.Contains(assignments, a => a.UserId == "user1");
        Assert.Contains(assignments, a => a.UserId == "user2");
    }

    [Fact]
    public async Task CheckWarehouseAccess_ValidRequest_ShouldReturnAccess()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create test assignment
        var assignment = new UserWarehouse
        {
            UserId = "user1",
            WarehouseId = 1,
            AccessLevel = "ReadOnly",
            IsDefault = false,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.UserWarehouses.Add(assignment);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/userwarehouse/users/user1/warehouses/1/access");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetAccessibleWarehouses_ValidUser_ShouldReturnWarehouseIds()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create test assignments
        var assignments = new List<UserWarehouse>
        {
            new() { UserId = "user1", WarehouseId = 1, AccessLevel = "Full", IsDefault = true, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { UserId = "user1", WarehouseId = 2, AccessLevel = "ReadOnly", IsDefault = false, AssignedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        context.UserWarehouses.AddRange(assignments);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/userwarehouse/users/user1/accessible-warehouses");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<int>>>(responseContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(1, result.Data);
        Assert.Contains(2, result.Data);
    }

    [Fact]
    public async Task UnauthorizedRequest_ShouldReturn401()
    {
        // Arrange
        await SeedTestDataAsync();
        // Don't set authorization header

        // Act
        var response = await _client.GetAsync("/api/userwarehouse/users/user1/warehouses");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private class AuthResponse
    {
        public string Token { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}