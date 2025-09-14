using FluentAssertions;
using Xunit;

namespace Inventory.ComponentTests.Components;

public class SimpleComponentTests
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var expected = "Component Test";

        // Act
        var actual = "Component Test";

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void MathTest_ShouldCalculateCorrectly()
    {
        // Arrange
        var a = 10;
        var b = 5;

        // Act
        var result = a * b;

        // Assert
        result.Should().Be(50);
    }

    [Fact]
    public void BooleanTest_ShouldWork()
    {
        // Arrange
        var isTrue = true;

        // Act
        var result = !isTrue;

        // Assert
        result.Should().BeFalse();
    }
}
