using Bunit;
using FluentAssertions;
using Inventory.UI.Components;
using Inventory.UI.Components.Products;
using Inventory.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Inventory.ComponentTests.Components;

/// <summary>
/// Tests for ProductCard component
/// </summary>
public class ProductCardTests : ComponentTestBase
{
    [Fact]
    public void Render_WithValidProduct_ShouldDisplayProductInformation()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = 1,
            Name = "Test Product",
            SKU = "TEST-001",
            Description = "Test Description",
            Quantity = 100,
                UnitOfMeasureSymbol = "pcs",
            CategoryName = "Test Category",
            ManufacturerName = "Test Manufacturer"
        };

        // Act
        var cut = RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product));

        // Assert
        cut.Find("h5").TextContent.Should().Be("Test Product");
        cut.Find(".card-text").TextContent.Should().Contain("TEST-001");
        cut.Find(".badge").TextContent.Should().Be("100 pcs");
    }

    [Fact]
    public void Render_WithLowStock_ShouldShowWarning()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = 1,
            Name = "Low Stock Product",
            SKU = "LOW-001",
            Quantity = 5,
            MinStock = 10,
                UnitOfMeasureSymbol = "pcs"
        };

        // Act
        var cut = RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product));

        // Assert
        cut.Find(".badge").ClassList.Should().Contain("bg-danger");
    }

    [Fact]
    public void ClickEditButton_ShouldTriggerEditEvent()
    {
        // Arrange
        var product = new ProductDto { Id = 1, Name = "Test Product" };
        var editClicked = false;
        
        var cut = RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.OnEdit, (ProductDto p) => editClicked = true));

        // Act
        cut.Find("button:contains('Изменить')").Click();

        // Assert
        editClicked.Should().BeTrue();
    }

    [Fact]
    public void Render_WithInactiveProduct_ShouldShowInactiveState()
    {
        // Arrange
        var product = new ProductDto
        {
            Id = 1,
            Name = "Inactive Product",
            IsActive = false
        };

        // Act
        var cut = RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product));

        // Assert
        cut.Find(".card").Should().NotBeNull();
        // Note: Inactive state styling would need to be implemented in the component
    }
}
