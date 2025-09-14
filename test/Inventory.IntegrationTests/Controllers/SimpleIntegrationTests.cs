using FluentAssertions;
using Xunit;

namespace Inventory.IntegrationTests.Controllers;

public class SimpleIntegrationTests
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var expected = "Hello World";

        // Act
        var actual = "Hello World";

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void MathTest_ShouldCalculateCorrectly()
    {
        // Arrange
        var a = 5;
        var b = 3;

        // Act
        var result = a + b;

        // Assert
        result.Should().Be(8);
    }

    [Fact]
    public void StringTest_ShouldWork()
    {
        // Arrange
        var input = "test";

        // Act
        var result = input.ToUpper();

        // Assert
        result.Should().Be("TEST");
    }
}
