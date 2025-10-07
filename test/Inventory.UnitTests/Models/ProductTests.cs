using FluentAssertions;
using Inventory.API.Models;
using Xunit;

namespace Inventory.UnitTests.Models;

public class ProductTests
{
    [Fact]
    public void Product_ShouldHaveCorrectDefaultValues()
    {
        var product = new Product();

        product.Name.Should().BeEmpty();
        product.SKU.Should().BeEmpty();
        product.UnitOfMeasureId.Should().Be(0);
        product.IsActive.Should().BeTrue();
        product.CurrentQuantity.Should().Be(0);
        product.Transactions.Should().NotBeNull();
        product.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Product_ShouldSetPropertiesCorrectly()
    {
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            Description = "Test Description",
            CurrentQuantity = 100,
            UnitOfMeasureId = 1,
            IsActive = true,
            Note = "Test Note"
        };

        product.Name.Should().Be("Test Product");
        product.SKU.Should().Be("TEST-001");
        product.Description.Should().Be("Test Description");
        product.CurrentQuantity.Should().Be(100);
        product.UnitOfMeasureId.Should().Be(1);
        product.IsActive.Should().BeTrue();
        product.Note.Should().Be("Test Note");
    }

    [Fact]
    public void Product_ShouldHaveTimestampFields()
    {
        var product = new Product();
        var now = DateTime.UtcNow;

        product.CreatedAt = now;
        product.UpdatedAt = now;

        product.CreatedAt.Should().Be(now);
        product.UpdatedAt.Should().Be(now);
    }
}
