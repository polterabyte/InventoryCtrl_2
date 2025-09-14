using Bunit;
using FluentAssertions;
using Inventory.UI.Components.Dashboard;
using Inventory.Shared.DTOs;
using Xunit;

namespace Inventory.ComponentTests.Components.Dashboard;

public class StatsWidgetTests : ComponentTestBase
{
    [Fact]
    public void StatsWidget_WithValidData_ShouldRenderCorrectly()
    {
        // Arrange
        const string label = "Total Products";
        const int value = 150;
        const string iconClass = "bi bi-box";
        const string cardClass = "primary";

        // Act
        var component = RenderComponent<StatsWidget>(parameters => parameters
            .Add(p => p.Label, label)
            .Add(p => p.Value, value)
            .Add(p => p.IconClass, iconClass)
            .Add(p => p.CardClass, cardClass)
        );

        // Assert
        component.Should().NotBeNull();
        component.Find(".stats-widget").Should().NotBeNull();
        
        // Check if stats are displayed
        component.Markup.Should().Contain("150"); // Value
        component.Markup.Should().Contain("Total Products"); // Label
        component.Markup.Should().Contain("bi bi-box"); // IconClass
    }

    [Fact]
    public void StatsWidget_WithDefaultValues_ShouldHandleGracefully()
    {
        // Act
        var component = RenderComponent<StatsWidget>();

        // Assert
        component.Should().NotBeNull();
        component.Find(".stats-widget").Should().NotBeNull();
        component.Markup.Should().Contain("0"); // Default value
    }

    [Fact]
    public void StatsWidget_WithZeroValue_ShouldDisplayZero()
    {
        // Arrange
        const string label = "Empty Stats";
        const int value = 0;

        // Act
        var component = RenderComponent<StatsWidget>(parameters => parameters
            .Add(p => p.Label, label)
            .Add(p => p.Value, value)
        );

        // Assert
        component.Should().NotBeNull();
        component.Markup.Should().Contain("0");
        component.Markup.Should().Contain("Empty Stats");
    }

    [Fact]
    public void StatsWidget_ShouldHaveCorrectCssClasses()
    {
        // Arrange
        const string label = "Test Widget";
        const int value = 100;
        const string cardClass = "success";

        // Act
        var component = RenderComponent<StatsWidget>(parameters => parameters
            .Add(p => p.Label, label)
            .Add(p => p.Value, value)
            .Add(p => p.CardClass, cardClass)
        );

        // Assert
        component.Find(".stats-widget").Should().NotBeNull();
        component.Find(".card").Should().NotBeNull();
        component.Find(".card-body").Should().NotBeNull();
        component.Markup.Should().Contain("border-success");
    }
}
