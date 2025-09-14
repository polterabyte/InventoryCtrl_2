using Bunit;
using FluentAssertions;
using Inventory.UI.Components;
using Xunit;

namespace Inventory.ComponentTests.Components;

public class LoadingSpinnerTests : TestContext
{
    [Fact]
    public void LoadingSpinner_DefaultParameters_ShouldRenderCorrectly()
    {
        // Act
        var component = RenderComponent<LoadingSpinner>();

        // Assert
        var spinner = component.Find(".spinner-border");
        spinner.Should().NotBeNull();
        spinner.Attributes["role"]?.Value.Should().Be("status");
        
        var visuallyHidden = component.Find(".visually-hidden");
        visuallyHidden.TextContent.Should().Be("Loading...");
    }

    [Fact]
    public void LoadingSpinner_WithCustomMessage_ShouldDisplayMessage()
    {
        // Arrange
        var customMessage = "Please wait...";

        // Act
        var component = RenderComponent<LoadingSpinner>(parameters => 
            parameters.Add(p => p.Message, customMessage));

        // Assert
        var messageElement = component.Find(".loading-message p");
        messageElement.TextContent.Should().Be(customMessage);
    }

    [Fact]
    public void LoadingSpinner_WithCustomLoadingText_ShouldUseCustomText()
    {
        // Arrange
        var customLoadingText = "Processing...";

        // Act
        var component = RenderComponent<LoadingSpinner>(parameters => 
            parameters.Add(p => p.LoadingText, customLoadingText));

        // Assert
        var visuallyHidden = component.Find(".visually-hidden");
        visuallyHidden.TextContent.Should().Be(customLoadingText);
    }

    [Theory]
    [InlineData(SpinnerSize.Small, "spinner-border-sm")]
    [InlineData(SpinnerSize.Medium, "")]
    [InlineData(SpinnerSize.Large, "spinner-border-lg")]
    public void LoadingSpinner_DifferentSizes_ShouldApplyCorrectClasses(SpinnerSize size, string expectedClass)
    {
        // Act
        var component = RenderComponent<LoadingSpinner>(parameters => 
            parameters.Add(p => p.Size, size));

        // Assert
        var spinner = component.Find(".spinner-border");
        if (string.IsNullOrEmpty(expectedClass))
        {
            spinner.ClassList.Should().NotContain("spinner-border-sm");
            spinner.ClassList.Should().NotContain("spinner-border-lg");
        }
        else
        {
            spinner.ClassList.Should().Contain(expectedClass);
        }
    }

    [Theory]
    [InlineData(SpinnerColor.Primary, "text-primary")]
    [InlineData(SpinnerColor.Secondary, "text-secondary")]
    [InlineData(SpinnerColor.Success, "text-success")]
    [InlineData(SpinnerColor.Danger, "text-danger")]
    [InlineData(SpinnerColor.Warning, "text-warning")]
    [InlineData(SpinnerColor.Info, "text-info")]
    [InlineData(SpinnerColor.Light, "text-light")]
    [InlineData(SpinnerColor.Dark, "text-dark")]
    public void LoadingSpinner_DifferentColors_ShouldApplyCorrectClasses(SpinnerColor color, string expectedClass)
    {
        // Act
        var component = RenderComponent<LoadingSpinner>(parameters => 
            parameters.Add(p => p.Color, color));

        // Assert
        var spinner = component.Find(".spinner-border");
        spinner.ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void LoadingSpinner_Centered_ShouldHaveCenteredClass()
    {
        // Act
        var component = RenderComponent<LoadingSpinner>(parameters => 
            parameters.Add(p => p.Centered, true));

        // Assert
        var container = component.Find(".loading-spinner");
        container.ClassList.Should().Contain("text-center");
    }

    [Fact]
    public void LoadingSpinner_NotCentered_ShouldNotHaveCenteredClass()
    {
        // Act
        var component = RenderComponent<LoadingSpinner>(parameters => 
            parameters.Add(p => p.Centered, false));

        // Assert
        var container = component.Find(".loading-spinner");
        container.ClassList.Should().NotContain("text-center");
    }

    [Fact]
    public void LoadingSpinner_FullScreen_ShouldHaveFullScreenClass()
    {
        // Act
        var component = RenderComponent<LoadingSpinner>(parameters => 
            parameters.Add(p => p.FullScreen, true));

        // Assert
        var container = component.Find(".loading-spinner");
        container.ClassList.Should().Contain("loading-fullscreen");
    }

    [Fact]
    public void LoadingSpinner_WithAdditionalAttributes_ShouldApplyAttributes()
    {
        // Arrange
        var additionalAttributes = new Dictionary<string, object>
        {
            { "data-testid", "loading-spinner" },
            { "id", "custom-spinner" }
        };

        // Act
        var component = RenderComponent<LoadingSpinner>(parameters => 
            parameters.Add(p => p.AdditionalAttributes, additionalAttributes));

        // Assert
        var spinner = component.Find(".spinner-border");
        spinner.Attributes["data-testid"]?.Value.Should().Be("loading-spinner");
        spinner.Attributes["id"]?.Value.Should().Be("custom-spinner");
    }

    [Fact]
    public void LoadingSpinner_WithoutMessage_ShouldNotShowMessageContainer()
    {
        // Act
        var component = RenderComponent<LoadingSpinner>(parameters => 
            parameters.Add(p => p.Message, string.Empty));

        // Assert
        component.FindAll(".loading-message").Should().BeEmpty();
    }
}
