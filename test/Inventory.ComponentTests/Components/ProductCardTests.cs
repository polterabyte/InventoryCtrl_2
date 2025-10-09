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
        var product = new ProductDto
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Quantity = 100,
            UnitOfMeasureSymbol = "pcs",
            CategoryName = "Test Category"
        };

        var cut = RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product));

        cut.Find("h5").TextContent.Should().Be("Test Product");
        cut.Find(".badge").TextContent.Should().Be("100 pcs");
    }

    [Fact]
    public void Render_WithZeroQuantity_ShouldShowDangerBadge()
    {
        var product = new ProductDto
        {
            Id = 1,
            Name = "Low Stock Product",
            Quantity = 0,
            UnitOfMeasureSymbol = "pcs"
        };

        var cut = RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product));

        cut.Find(".badge").ClassList.Should().Contain("bg-danger");
    }

    [Fact]
    public void ClickEditButton_ShouldTriggerEditEvent()
    {
        var product = new ProductDto { Id = 1, Name = "Test Product" };
        var editClicked = false;
        
        var cut = RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product)
            .Add(p => p.OnEdit, (ProductDto _) => editClicked = true));

        var editButton = cut.FindAll("button").FirstOrDefault(b => b.Attributes.Any(a => a.Name == "title"));
        editButton?.Click();

        editClicked.Should().BeTrue();
    }

    [Fact]
    public void Render_WithInactiveProduct_ShouldShowInactiveState()
    {
        var product = new ProductDto
        {
            Id = 1,
            Name = "Inactive Product",
            IsActive = false
        };

        var cut = RenderComponent<ProductCard>(parameters => parameters
            .Add(p => p.Product, product));

        cut.Find(".card").Should().NotBeNull();
    }
}
