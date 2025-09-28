using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using Inventory.API.Hubs;
using Inventory.API.Models;
using Microsoft.AspNetCore.Http;

namespace Inventory.UnitTests.Hubs;

public class NotificationHubTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<NotificationHub>> _loggerMock;
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ConnectionInfo> _connectionInfoMock;
    private readonly Mock<IGroupManager> _groupManagerMock;

    public NotificationHubTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"notification_test_{Guid.NewGuid():N}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        
        _loggerMock = new Mock<ILogger<NotificationHub>>();
        _clientsMock = new Mock<IHubCallerClients>();
        _contextMock = new Mock<HubCallerContext>();
        _httpContextMock = new Mock<HttpContext>();
        _connectionInfoMock = new Mock<ConnectionInfo>();
        _groupManagerMock = new Mock<IGroupManager>();
        
        // Setup user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "testuser"),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        _contextMock.Setup(c => c.User).Returns(principal);
        _contextMock.Setup(c => c.ConnectionId).Returns("test_connection_id");
        
        // Setup HTTP context
        _httpContextMock.Setup(h => h.User).Returns(principal);
        _httpContextMock.Setup(h => h.Connection).Returns(_connectionInfoMock.Object);
        
        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = "testuser",
            UserName = "Test User",
            Email = "test@example.com"
        };
        
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task OnConnectedAsync_WithValidUser_ShouldAddToUserGroup()
    {
        // Arrange
        var hub = new NotificationHub(_loggerMock.Object, _context);
        hub.Clients = _clientsMock.Object;
        hub.Context = _contextMock.Object;
        hub.Groups = _groupManagerMock.Object;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await hub.OnConnectedAsync());
        exception.Should().BeNull(); // No exception should be thrown

        // Verify group operations
        _groupManagerMock.Verify(g => g.AddToGroupAsync("test_connection_id", "User_testuser", default), Times.Once);
        _groupManagerMock.Verify(g => g.AddToGroupAsync("test_connection_id", "AllUsers", default), Times.Once);
        
        // Verify connection was saved to database
        var connection = await _context.SignalRConnections
            .FirstOrDefaultAsync(c => c.ConnectionId == "test_connection_id");
        connection.Should().NotBeNull();
        connection!.UserId.Should().Be("testuser");
        connection.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task OnConnectedAsync_WithInvalidUser_ShouldNotAddToGroups()
    {
        // Arrange
        var invalidClaims = new List<Claim>
        {
            new(ClaimTypes.Name, "Test User")
            // Missing NameIdentifier claim
        };
        var invalidIdentity = new ClaimsIdentity(invalidClaims, "TestAuth");
        var invalidPrincipal = new ClaimsPrincipal(invalidIdentity);
        
        _contextMock.Setup(c => c.User).Returns(invalidPrincipal);
        
        var hub = new NotificationHub(_loggerMock.Object, _context);
        hub.Clients = _clientsMock.Object;
        hub.Context = _contextMock.Object;
        hub.Groups = _groupManagerMock.Object;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await hub.OnConnectedAsync());
        exception.Should().BeNull(); // No exception should be thrown

        // Verify no group operations occurred
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithValidUser_ShouldRemoveFromConnections()
    {
        // Arrange
        // First establish connection
        var hub = new NotificationHub(_loggerMock.Object, _context);
        hub.Clients = _clientsMock.Object;
        hub.Context = _contextMock.Object;
        hub.Groups = _groupManagerMock.Object;

        // Connect first
        await hub.OnConnectedAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await hub.OnDisconnectedAsync(null));
        exception.Should().BeNull(); // No exception should be thrown

        // Verify connection was marked as inactive in database
        var connection = await _context.SignalRConnections
            .FirstOrDefaultAsync(c => c.ConnectionId == "test_connection_id");
        connection.Should().NotBeNull();
        connection!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task JoinGroup_WithValidGroupName_ShouldAddToGroup()
    {
        // Arrange
        var hub = new NotificationHub(_loggerMock.Object, _context);
        hub.Clients = _clientsMock.Object;
        hub.Context = _contextMock.Object;
        hub.Groups = _groupManagerMock.Object;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await hub.JoinGroup("test_group"));
        exception.Should().BeNull(); // No exception should be thrown

        // Verify group operation
        _groupManagerMock.Verify(g => g.AddToGroupAsync("test_connection_id", "test_group", default), Times.Once);
    }

    [Fact]
    public async Task LeaveGroup_WithValidGroupName_ShouldRemoveFromGroup()
    {
        // Arrange
        var hub = new NotificationHub(_loggerMock.Object, _context);
        hub.Clients = _clientsMock.Object;
        hub.Context = _contextMock.Object;
        hub.Groups = _groupManagerMock.Object;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await hub.LeaveGroup("test_group"));
        exception.Should().BeNull(); // No exception should be thrown

        // Verify group operation
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync("test_connection_id", "test_group", default), Times.Once);
    }

    [Fact]
    public async Task SubscribeToNotifications_WithValidType_ShouldAddToNotificationGroup()
    {
        // Arrange
        var hub = new NotificationHub(_loggerMock.Object, _context);
        hub.Clients = _clientsMock.Object;
        hub.Context = _contextMock.Object;
        hub.Groups = _groupManagerMock.Object;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await hub.SubscribeToNotifications("alert"));
        exception.Should().BeNull(); // No exception should be thrown

        // Verify group operation
        _groupManagerMock.Verify(g => g.AddToGroupAsync("test_connection_id", "Notifications_alert", default), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromNotifications_WithValidType_ShouldRemoveFromNotificationGroup()
    {
        // Arrange
        var hub = new NotificationHub(_loggerMock.Object, _context);
        hub.Clients = _clientsMock.Object;
        hub.Context = _contextMock.Object;
        hub.Groups = _groupManagerMock.Object;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await hub.UnsubscribeFromNotifications("alert"));
        exception.Should().BeNull(); // No exception should be thrown

        // Verify group operation
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync("test_connection_id", "Notifications_alert", default), Times.Once);
    }

    [Fact]
    public async Task CleanupInactiveConnections_ShouldRemoveOldConnections()
    {
        // Arrange
        // Add an inactive connection that's older than 24 hours
        var oldConnection = new SignalRConnection
        {
            ConnectionId = "old_connection",
            UserId = "testuser",
            ConnectedAt = DateTime.UtcNow.AddDays(-2),
            LastActivityAt = DateTime.UtcNow.AddDays(-1),
            IsActive = false
        };
        
        _context.SignalRConnections.Add(oldConnection);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await NotificationHub.CleanupInactiveConnections(_context, _loggerMock.Object));
        exception.Should().BeNull(); // No exception should be thrown

        // Verify connection was removed
        var connection = await _context.SignalRConnections
            .FirstOrDefaultAsync(c => c.ConnectionId == "old_connection");
        connection.Should().BeNull();
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