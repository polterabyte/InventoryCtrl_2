using FluentAssertions;
using Inventory.API.Models;
using Xunit;

namespace Inventory.UnitTests.Models;

public class ProductTests
{
    [Fact]
    public void Product_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.Name.Should().BeEmpty();
        product.SKU.Should().BeEmpty();
        product.Unit.Should().BeEmpty();
        product.IsActive.Should().BeTrue();
        product.Quantity.Should().Be(0);
        product.MinStock.Should().Be(0);
        product.MaxStock.Should().Be(0);
        product.Transactions.Should().NotBeNull();
        product.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Product_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            Description = "Test Description",
            Quantity = 100,
            Unit = "pcs",
            IsActive = true,
            MinStock = 10,
            MaxStock = 1000,
            Note = "Test Note"
        };

        // Act & Assert
        product.Name.Should().Be("Test Product");
        product.SKU.Should().Be("TEST-001");
        product.Description.Should().Be("Test Description");
        product.Quantity.Should().Be(100);
        product.Unit.Should().Be("pcs");
        product.IsActive.Should().BeTrue();
        product.MinStock.Should().Be(10);
        product.MaxStock.Should().Be(1000);
        product.Note.Should().Be("Test Note");
    }

    [Fact]
    public void Product_ShouldHaveTimestampFields()
    {
        // Arrange
        var product = new Product();
        var now = DateTime.UtcNow;

        // Act
        product.CreatedAt = now;
        product.UpdatedAt = now;

        // Assert
        product.CreatedAt.Should().Be(now);
        product.UpdatedAt.Should().Be(now);
    }
}
