using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Inventory.API.Models;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Inventory.Shared.Models;
using Xunit;

namespace Inventory.UnitTests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    // private readonly Mock<INotificationRuleEngine> _ruleEngineMock; // Commented out - interface not found
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<NotificationService>>();
        // _ruleEngineMock = new Mock<INotificationRuleEngine>(); // Commented out - interface not found
        _service = new NotificationService(_context, _loggerMock.Object, null!, null!); // Pass null for rule engine and signalR service
    }

    [Fact]
    public async Task CreateNotificationAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateNotificationRequest
        {
            Title = "Test Notification",
            Message = "This is a test notification",
            Type = "INFO",
            Category = "SYSTEM",
            UserId = "test-user-id"
        };

        // Act
        var result = await _service.CreateNotificationAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Test Notification", result.Data.Title);
        Assert.Equal("This is a test notification", result.Data.Message);
        Assert.Equal("INFO", result.Data.Type);
        Assert.Equal("SYSTEM", result.Data.Category);
        Assert.Equal("test-user-id", result.Data.UserId);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ValidUserId_ReturnsNotifications()
    {
        // Arrange
        var userId = "test-user-id";
        var notification1 = new DbNotification
        {
            Title = "Notification 1",
            Message = "Message 1",
            Type = "INFO",
            Category = "SYSTEM",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        var notification2 = new DbNotification
        {
            Title = "Notification 2",
            Message = "Message 2",
            Type = "WARNING",
            Category = "STOCK",
            UserId = userId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _context.Notifications.AddRange(notification1, notification2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserNotificationsAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("Notification 1", result.Data[0].Title);
        Assert.Equal("Notification 2", result.Data[1].Title);
    }

    [Fact]
    public async Task MarkAsReadAsync_ValidNotification_ReturnsSuccess()
    {
        // Arrange
        var userId = "test-user-id";
        var notification = new DbNotification
        {
            Title = "Test Notification",
            Message = "Test Message",
            Type = "INFO",
            Category = "SYSTEM",
            UserId = userId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MarkAsReadAsync(notification.Id, userId);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        
        var updatedNotification = await _context.Notifications.FindAsync(notification.Id);
        Assert.True(updatedNotification!.IsRead);
        Assert.NotNull(updatedNotification.ReadAt);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ValidUserId_MarksAllAsRead()
    {
        // Arrange
        var userId = "test-user-id";
        var notification1 = new DbNotification
        {
            Title = "Notification 1",
            Message = "Message 1",
            Type = "INFO",
            Category = "SYSTEM",
            UserId = userId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        var notification2 = new DbNotification
        {
            Title = "Notification 2",
            Message = "Message 2",
            Type = "WARNING",
            Category = "STOCK",
            UserId = userId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _context.Notifications.AddRange(notification1, notification2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MarkAllAsReadAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync();
        
        Assert.All(notifications, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task GetNotificationStatsAsync_ValidUserId_ReturnsStats()
    {
        // Arrange
        var userId = "test-user-id";
        var notification1 = new DbNotification
        {
            Title = "Notification 1",
            Message = "Message 1",
            Type = "INFO",
            Category = "SYSTEM",
            UserId = userId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        var notification2 = new DbNotification
        {
            Title = "Notification 2",
            Message = "Message 2",
            Type = "WARNING",
            Category = "STOCK",
            UserId = userId,
            IsRead = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _context.Notifications.AddRange(notification1, notification2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetNotificationStatsAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalNotifications);
        Assert.Equal(1, result.Data.UnreadNotifications);
        Assert.Equal(0, result.Data.ArchivedNotifications);
    }

    [Fact]
    public async Task TriggerStockLowNotificationAsync_ValidProduct_TriggersNotification()
    {
        // Arrange
        var product = new Inventory.Shared.Models.Product
        {
            Id = 1,
            Name = "Test Product",
            SKU = "TEST-001",
            CurrentQuantity = 5,
            MinStock = 10,
            IsActive = true
        };

        var rule = new NotificationRule
        {
            Id = 1,
            Name = "Stock Low Rule",
            EventType = "STOCK_LOW",
            NotificationType = "WARNING",
            Category = "STOCK",
            Condition = """{"Product.Quantity": {"operator": "<=", "value": "{{Product.MinStock}}"}, "Product.IsActive": true}""",
            Template = "Product '{{Product.Name}}' is running low on stock",
            IsActive = true,
            Priority = 5
        };

        var preference = new NotificationPreference
        {
            Id = 1,
            UserId = "test-user-id",
            EventType = "STOCK_LOW",
            InAppEnabled = true,
            EmailEnabled = false,
            PushEnabled = false
        };

        _context.NotificationRules.Add(rule);
        _context.NotificationPreferences.Add(preference);
        await _context.SaveChangesAsync();

        // _ruleEngineMock.Setup(x => x.GetActiveRulesForEventAsync("STOCK_LOW"))
        //     .ReturnsAsync(new List<NotificationRule> { rule });
        // _ruleEngineMock.Setup(x => x.EvaluateConditionAsync(It.IsAny<string>(), It.IsAny<object>()))
        //     .ReturnsAsync(true);
        // _ruleEngineMock.Setup(x => x.ProcessTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
        //     .ReturnsAsync("Product 'Test Product' is running low on stock");
        // _ruleEngineMock.Setup(x => x.GetUserPreferencesForEventAsync("STOCK_LOW"))
        //     .ReturnsAsync(new List<NotificationPreference> { preference });

        // Act
        await _service.TriggerStockLowNotificationAsync(product);

        // Assert
        var notifications = await _context.Notifications
            .Where(n => n.UserId == "test-user-id")
            .ToListAsync();
        
        Assert.Single(notifications);
        Assert.Equal("Low Stock Alert: Test Product", notifications[0].Title);
        Assert.Equal("Product 'Test Product' is running low on stock", notifications[0].Message);
        Assert.Equal("WARNING", notifications[0].Type);
        Assert.Equal("STOCK", notifications[0].Category);
    }

    [Fact]
    public async Task UpdatePreferenceAsync_ValidRequest_UpdatesPreference()
    {
        // Arrange
        var userId = "test-user-id";
        var request = new UpdateNotificationPreferenceRequest
        {
            EventType = "STOCK_LOW",
            EmailEnabled = true,
            InAppEnabled = true,
            PushEnabled = false,
            MinPriority = 5
        };

        // Act
        var result = await _service.UpdatePreferenceAsync(userId, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("STOCK_LOW", result.Data.EventType);
        Assert.True(result.Data.EmailEnabled);
        Assert.True(result.Data.InAppEnabled);
        Assert.False(result.Data.PushEnabled);
        Assert.Equal(5, result.Data.MinPriority);
    }

    [Fact]
    public async Task CleanupExpiredNotificationsAsync_ExpiredNotifications_RemovesThem()
    {
        // Arrange
        var expiredNotification = new DbNotification
        {
            Title = "Expired Notification",
            Message = "This notification has expired",
            Type = "INFO",
            Category = "SYSTEM",
            UserId = "test-user-id",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var validNotification = new DbNotification
        {
            Title = "Valid Notification",
            Message = "This notification is still valid",
            Type = "INFO",
            Category = "SYSTEM",
            UserId = "test-user-id",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.AddRange(expiredNotification, validNotification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CleanupExpiredNotificationsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        
        var remainingNotifications = await _context.Notifications.ToListAsync();
        Assert.Single(remainingNotifications);
        Assert.Equal("Valid Notification", remainingNotifications[0].Title);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
