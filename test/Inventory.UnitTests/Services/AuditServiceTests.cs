using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Inventory.API.Models;
using Inventory.API.Services;
using System.Security.Claims;
using Xunit;

namespace Inventory.UnitTests.Services;

public class AuditServiceTests : TestBase
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<AuditService>> _loggerMock;
    private readonly AuditService _auditService;
    private readonly HttpContext _httpContext;

    public AuditServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<AuditService>>();
        
        // Setup HttpContext
        _httpContext = new DefaultHttpContext();
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "testuser")
        }));
        _httpContext.Request.Headers["User-Agent"] = "TestAgent/1.0";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);
        
        _auditService = new AuditService(Context, _httpContextAccessorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task LogEntityChangeAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var entityName = "Product";
        var entityId = "123";
        var action = "CREATE";
        var oldValues = new { Name = "Old Name" };
        var newValues = new { Name = "New Name" };
        var description = "Product created";

        // Act
        await _auditService.LogEntityChangeAsync(entityName, entityId, action, oldValues, newValues, description);

        // Assert
        var auditLog = await Context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(entityName, auditLog.EntityName);
        Assert.Equal(entityId, auditLog.EntityId);
        Assert.Equal(action, auditLog.Action);
        Assert.Equal("test-user-id", auditLog.UserId);
        Assert.Equal("testuser", auditLog.Username);
        Assert.NotNull(auditLog.OldValues);
        Assert.NotNull(auditLog.NewValues);
        Assert.Equal(description, auditLog.Description);
        Assert.Equal("127.0.0.1", auditLog.IpAddress);
        Assert.Equal("TestAgent/1.0", auditLog.UserAgent);
    }

    [Fact]
    public async Task LogUserActionAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var action = "LOGIN";
        var description = "User logged in";
        var metadata = new { LoginTime = DateTime.UtcNow };

        // Act
        await _auditService.LogUserActionAsync(action, description, metadata: metadata);

        // Assert
        var auditLog = await Context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("User", auditLog.EntityName);
        Assert.Equal("test-user-id", auditLog.EntityId);
        Assert.Equal(action, auditLog.Action);
        Assert.Equal(description, auditLog.Description);
        Assert.NotNull(auditLog.Metadata);
    }

    [Fact]
    public async Task LogHttpRequestAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var httpMethod = "GET";
        var url = "https://localhost:5001/api/products";
        var statusCode = 200;
        var duration = 150L;

        // Act
        await _auditService.LogHttpRequestAsync(httpMethod, url, statusCode, duration);

        // Assert
        var auditLog = await Context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("HTTP", auditLog.EntityName);
        Assert.Equal(httpMethod, auditLog.HttpMethod);
        Assert.Equal(url, auditLog.Url);
        Assert.Equal(statusCode, auditLog.StatusCode);
        Assert.Equal(duration, auditLog.Duration);
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldReturnFilteredLogs()
    {
        // Arrange
        await SeedAuditLogs();

        // Act
        var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
            entityName: "Product",
            action: "CREATE",
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.Equal(2, logs.Count);
        Assert.All(logs, log => Assert.Equal("Product", log.EntityName));
        Assert.All(logs, log => Assert.Equal("CREATE", log.Action));
    }

    [Fact]
    public async Task GetEntityAuditLogsAsync_ShouldReturnEntityLogs()
    {
        // Arrange
        await SeedAuditLogs();

        // Act
        var logs = await _auditService.GetEntityAuditLogsAsync("Product", "123");

        // Assert
        Assert.Equal(2, logs.Count);
        Assert.All(logs, log => Assert.Equal("Product", log.EntityName));
        Assert.All(logs, log => Assert.Equal("123", log.EntityId));
    }

    [Fact]
    public async Task GetUserAuditLogsAsync_ShouldReturnUserLogs()
    {
        // Arrange
        await SeedAuditLogs();

        // Act
        var logs = await _auditService.GetUserAuditLogsAsync("test-user-id", 30);

        // Assert
        Assert.Equal(3, logs.Count);
        Assert.All(logs, log => Assert.Equal("test-user-id", log.UserId));
    }

    [Fact]
    public async Task CleanupOldLogsAsync_ShouldRemoveOldLogs()
    {
        // Arrange
        var oldLog = new AuditLog
        {
            EntityName = "Test",
            EntityId = "1",
            Action = "TEST",
            UserId = "test-user-id",
            Timestamp = DateTime.UtcNow.AddDays(-100)
        };
        Context.AuditLogs.Add(oldLog);
        await Context.SaveChangesAsync();

        // Act
        var deletedCount = await _auditService.CleanupOldLogsAsync(90);

        // Assert
        Assert.Equal(1, deletedCount);
        var remainingLogs = await Context.AuditLogs.CountAsync();
        Assert.Equal(0, remainingLogs);
    }

    private async Task SeedAuditLogs()
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
                Timestamp = DateTime.UtcNow.AddDays(-1)
            },
            new AuditLog
            {
                EntityName = "Product",
                EntityId = "123",
                Action = "CREATE",
                UserId = "test-user-id",
                Username = "testuser",
                Timestamp = DateTime.UtcNow.AddDays(-2)
            },
            new AuditLog
            {
                EntityName = "User",
                EntityId = "test-user-id",
                Action = "LOGIN",
                UserId = "test-user-id",
                Username = "testuser",
                Timestamp = DateTime.UtcNow.AddDays(-3)
            }
        };

        Context.AuditLogs.AddRange(logs);
        await Context.SaveChangesAsync();
    }
}
