using Inventory.API.Models;
using Xunit;
using FluentAssertions;

namespace Inventory.UnitTests.Models;

public class ManufacturerTests
{
    [Fact]
    public void Manufacturer_DefaultConstructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var manufacturer = new Manufacturer();

        // Assert
        manufacturer.Should().NotBeNull();
        manufacturer.Name.Should().Be(string.Empty);
        manufacturer.Products.Should().NotBeNull();
        manufacturer.Products.Should().BeEmpty();
        manufacturer.CreatedAt.Should().Be(default(DateTime));
        manufacturer.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Manufacturer_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var manufacturer = new Manufacturer
        {
            Id = 1,
            Name = "Apple",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        manufacturer.Id.Should().Be(1);
        manufacturer.Name.Should().Be("Apple");
        manufacturer.CreatedAt.Should().Be(now);
        manufacturer.UpdatedAt.Should().Be(now);
    }

    // Test removed - ManufacturerId relationship was removed from Product model
    // [Fact]
    // public void Manufacturer_WithProducts_ShouldMaintainProductRelationship()
    // {
    //     // This test is no longer valid since ManufacturerId was removed from Product model
    // }

    [Fact]
    public void Manufacturer_CanHaveEmptyName()
    {
        // Act
        var manufacturer = new Manufacturer { Name = string.Empty };

        // Assert
        manufacturer.Name.Should().Be(string.Empty);
    }

    [Fact]
    public void Manufacturer_CanHaveNullUpdatedAt()
    {
        // Act
        var manufacturer = new Manufacturer { UpdatedAt = null };

        // Assert
        manufacturer.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Manufacturer_CanHaveUpdatedAt()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var manufacturer = new Manufacturer { UpdatedAt = now };

        // Assert
        manufacturer.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Manufacturer_CanHaveCreatedAt()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var manufacturer = new Manufacturer { CreatedAt = now };

        // Assert
        manufacturer.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Manufacturer_CanHaveLongName()
    {
        // Arrange
        var longName = new string('A', 1000);

        // Act
        var manufacturer = new Manufacturer { Name = longName };

        // Assert
        manufacturer.Name.Should().Be(longName);
        manufacturer.Name.Should().HaveLength(1000);
    }

    [Fact]
    public void Manufacturer_CanHaveSpecialCharactersInName()
    {
        // Arrange
        var specialName = "Apple Inc. & Co. (USA)";

        // Act
        var manufacturer = new Manufacturer { Name = specialName };

        // Assert
        manufacturer.Name.Should().Be(specialName);
    }
}
