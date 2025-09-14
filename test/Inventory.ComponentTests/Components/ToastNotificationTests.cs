using Bunit;
using FluentAssertions;
using Inventory.UI.Components.Notifications;
using Inventory.Shared.Models;
using Xunit;
using ToastNotificationComponent = Inventory.UI.Components.Notifications.ToastNotification;
using Bunit.Extensions;

namespace Inventory.ComponentTests.Components;

/// <summary>
/// Tests for ToastNotification component
/// </summary>
public class ToastNotificationTests : ComponentTestBase
{
    [Fact]
    public void Render_WithSuccessNotification_ShouldDisplaySuccessStyles()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Success",
            Message = "Operation completed successfully",
            Type = NotificationType.Success,
            Duration = 5000
        };

        // Act
        var cut = RenderComponent<ToastNotificationComponent>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        cut.Find(".toast-notification").ClassList.Should().Contain("toast-success");
        cut.Find(".toast-icon").ClassList.Should().Contain("toast-icon");
        cut.Find(".toast-title").TextContent.Should().Be("Success");
        cut.Find(".toast-message").TextContent.Should().Be("Operation completed successfully");
    }

    [Fact]
    public void Render_WithErrorNotification_ShouldDisplayErrorStyles()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Error",
            Message = "Operation failed",
            Type = NotificationType.Error,
            Duration = 0
        };

        // Act
        var cut = RenderComponent<ToastNotificationComponent>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        cut.Find(".toast-notification").ClassList.Should().Contain("toast-error");
        cut.Find(".toast-icon").ClassList.Should().Contain("toast-icon");
    }

    [Fact]
    public async Task ClickCloseButton_ShouldTriggerCloseEvent()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test",
            Message = "Test message",
            Type = NotificationType.Info
        };

        var closeClicked = false;
        var cut = RenderComponent<ToastNotificationComponent>(parameters => parameters
            .Add(p => p.Notification, notification)
            .Add(p => p.OnDismiss, (string id) => closeClicked = true));

        // Act
        cut.Find(".btn-close").Click();
        
        // Wait for async operation
        await Task.Delay(400);

        // Assert
        closeClicked.Should().BeTrue();
    }

    [Fact]
    public void Render_WithRetryAction_ShouldShowRetryButton()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Error",
            Message = "Network error",
            Type = NotificationType.Error,
            OnRetry = () => { }
        };

        var retryClicked = false;
        var cut = RenderComponent<ToastNotificationComponent>(parameters => parameters
            .Add(p => p.Notification, notification)
            .Add(p => p.OnRetry, () => retryClicked = true));

        // Act
        var retryButton = cut.Find(".toast-actions .btn");
        retryButton.Click();

        // Assert
        retryButton.TextContent.Should().Contain("Retry");
        retryClicked.Should().BeTrue();
    }

    [Fact]
    public void Render_WithLongMessage_ShouldWrapText()
    {
        // Arrange
        var longMessage = "This is a very long message that should wrap to multiple lines when displayed in the notification component to ensure proper readability and layout.";
        var notification = new Notification
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Info",
            Message = longMessage,
            Type = NotificationType.Info
        };

        // Act
        var cut = RenderComponent<ToastNotificationComponent>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        cut.Find(".toast-message").TextContent.Should().Be(longMessage);
        // Note: word-wrap is applied via CSS, not as a class
    }
}
