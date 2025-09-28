using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Inventory.API.Models;
using Inventory.API.Services;
using Xunit;

namespace Inventory.IntegrationTests.Controllers;

public class AuditControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly AppDbContext _context;

    public AuditControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Add in-memory database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("AuditTestDb");
                });
            });
        });

        _client = _factory.CreateClient();
        _context = _factory.Services.GetRequiredService<AppDbContext>();
        
        // Ensure database is created
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetAuditLogs_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/audit");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithAuthentication_ShouldReturnAuditLogs()
    {
        // Arrange
        await SeedTestData();
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLogResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(result);
        Assert.True(result.Logs.Count > 0);
    }

    [Fact]
    public async Task GetAuditLogs_WithFilters_ShouldReturnFilteredLogs()
    {
        // Arrange
        await SeedTestData();
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit?entityName=Product&action=CREATE");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLogResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(result);
        Assert.All(result.Logs, log => Assert.Equal("Product", log.EntityName));
        Assert.All(result.Logs, log => Assert.Equal("CREATE", log.Action));
    }

    [Fact]
    public async Task GetEntityAuditLogs_ShouldReturnEntityLogs()
    {
        // Arrange
        await SeedTestData();
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit/entity/Product/123");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AuditLogDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, log => Assert.Equal("Product", log.EntityName));
        Assert.All(result, log => Assert.Equal("123", log.EntityId));
    }

    [Fact]
    public async Task GetUserAuditLogs_ShouldReturnUserLogs()
    {
        // Arrange
        await SeedTestData();
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit/user/test-user-id");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AuditLogDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, log => Assert.Equal("test-user-id", log.UserId));
    }

    [Fact]
    public async Task GetAuditStatistics_ShouldReturnStatistics()
    {
        // Arrange
        await SeedTestData();
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit/statistics");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditStatisticsDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(result);
        Assert.True(result.TotalLogs > 0);
        Assert.True(result.SuccessfulLogs >= 0);
        Assert.True(result.FailedLogs >= 0);
    }

    [Fact]
    public async Task CleanupOldLogs_AsAdmin_ShouldReturnCleanupResult()
    {
        // Arrange
        await SeedTestData();
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/api/audit/cleanup?daysToKeep=30");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CleanupResultDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(result);
        Assert.True(result.DeletedCount >= 0);
        Assert.Equal(30, result.DaysToKeep);
    }

    private async Task SeedTestData()
    {
        var logs = new[]
        {
            new AuditLog
            {
                EntityName = "Product",
                EntityId = "123",
                Action = "CREATE",
                UserId = "test-user-id",
                Username = "testuser",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                IsSuccess = true
            },
            new AuditLog
            {
                EntityName = "Product",
                EntityId = "123",
                Action = "UPDATE",
                UserId = "test-user-id",
                Username = "testuser",
                Timestamp = DateTime.UtcNow.AddDays(-2),
                IsSuccess = true
            },
            new AuditLog
            {
                EntityName = "User",
                EntityId = "test-user-id",
                Action = "LOGIN",
                UserId = "test-user-id",
                Username = "testuser",
                Timestamp = DateTime.UtcNow.AddDays(-3),
                IsSuccess = true
            },
            new AuditLog
            {
                EntityName = "Product",
                EntityId = "456",
                Action = "DELETE",
                UserId = "test-user-id",
                Username = "testuser",
                Timestamp = DateTime.UtcNow.AddDays(-100), // Old log
                IsSuccess = false,
                ErrorMessage = "Product not found"
            }
        };

        _context.AuditLogs.AddRange(logs);
        await _context.SaveChangesAsync();
    }

    private Task<string> GetAdminToken()
    {
        // This is a simplified token generation for testing
        // In a real scenario, you would use the actual authentication endpoint
        return Task.FromResult("test-admin-token");
    }
}

// DTOs for testing
public class AuditLogResponse
{
    public List<AuditLogDto> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AuditLogDto
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public int? StatusCode { get; set; }
    public long? Duration { get; set; }
    public string? Metadata { get; set; }
    public string Severity { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AuditStatisticsDto
{
    public int TotalLogs { get; set; }
    public int SuccessfulLogs { get; set; }
    public int FailedLogs { get; set; }
    public Dictionary<string, int> LogsByAction { get; set; } = new();
    public Dictionary<string, int> LogsByEntity { get; set; } = new();
    public Dictionary<string, int> LogsBySeverity { get; set; } = new();
    public Dictionary<string, int> LogsByUser { get; set; } = new();
    public double AverageResponseTime { get; set; }
    public Dictionary<string, int> TopErrors { get; set; } = new();
}

public class CleanupResultDto
{
    public int DeletedCount { get; set; }
    public int DaysToKeep { get; set; }
    public DateTime CleanupDate { get; set; }
}
