using Bunit;
using FluentAssertions;
using Inventory.UI.Components.Dashboard;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Moq;
using Xunit;

namespace Inventory.ComponentTests.Components.Dashboard;

public class LowStockAlertTests : ComponentTestBase
{
    [Fact]
    public void LowStockAlert_WithValidData_ShouldRenderCorrectly()
    {
        // Arrange
        var lowStockProducts = new List<LowStockProductDto>
        {
            new LowStockProductDto
            {
                Id = 1,
                Name = "Low Stock Product 1",
                SKU = "LOW001",
                CurrentQuantity = 5,
                MinStock = 10,
                MaxStock = 100,
                CategoryName = "Electronics",
                ManufacturerName = "Test Manufacturer",
                Unit = "pcs"
            },
            new LowStockProductDto
            {
                Id = 2,
                Name = "Low Stock Product 2",
                SKU = "LOW002",
                CurrentQuantity = 2,
                MinStock = 5,
                MaxStock = 50,
                CategoryName = "Accessories",
                ManufacturerName = "Test Manufacturer 2",
                Unit = "units"
            }
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync(lowStockProducts!);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<LowStockAlert>();

        // Assert
        component.Should().NotBeNull();
        component.Find(".card").Should().NotBeNull();
        
        // Check if products are displayed
        component.Markup.Should().Contain("Low Stock Product 1");
        component.Markup.Should().Contain("LOW001");
        component.Markup.Should().Contain("5");
        component.Markup.Should().Contain("10");
        
        component.Markup.Should().Contain("Low Stock Product 2");
        component.Markup.Should().Contain("LOW002");
        component.Markup.Should().Contain("2");
        component.Markup.Should().Contain("5");
    }

    [Fact]
    public void LowStockAlert_WithEmptyList_ShouldDisplayEmptyState()
    {
        // Arrange
        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync(new List<LowStockProductDto>());

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<LowStockAlert>();

        // Assert
        component.Should().NotBeNull();
        component.Find(".card").Should().NotBeNull();
        component.Markup.Should().Contain("Все товары в норме");
    }

    [Fact]
    public void LowStockAlert_WithNullData_ShouldHandleGracefully()
    {
        // Arrange
        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync((List<LowStockProductDto>?)null);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<LowStockAlert>();

        // Assert
        component.Should().NotBeNull();
        component.Find(".card").Should().NotBeNull();
    }

    [Fact]
    public void LowStockAlert_ShouldHaveCorrectCssClasses()
    {
        // Arrange
        var lowStockProducts = new List<LowStockProductDto>
        {
            new LowStockProductDto
            {
                Id = 1,
                Name = "Test Product",
                SKU = "TEST001",
                CurrentQuantity = 3,
                MinStock = 10,
                MaxStock = 100,
                CategoryName = "Electronics",
                ManufacturerName = "Test Manufacturer",
                Unit = "pcs"
            }
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync(lowStockProducts!);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<LowStockAlert>();

        // Assert
        component.Find(".card").Should().NotBeNull();
        component.Find(".card-header").Should().NotBeNull();
        component.Find(".card-body").Should().NotBeNull();
        component.Markup.Should().Contain("border-warning");
    }

    [Fact]
    public void LowStockAlert_ShouldShowCorrectQuantityInfo()
    {
        // Arrange
        var lowStockProducts = new List<LowStockProductDto>
        {
            new LowStockProductDto
            {
                Id = 1,
                Name = "Test Product",
                SKU = "TEST001",
                CurrentQuantity = 3,
                MinStock = 10,
                MaxStock = 100,
                CategoryName = "Electronics",
                ManufacturerName = "Test Manufacturer",
                Unit = "pcs"
            }
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync(lowStockProducts!);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<LowStockAlert>();

        // Assert
        component.Markup.Should().Contain("3"); // Current quantity
        component.Markup.Should().Contain("10"); // Min stock
        component.Markup.Should().Contain("pcs"); // Unit
    }

    [Fact]
    public void LowStockAlert_ShouldShowWarningForCriticalStock()
    {
        // Arrange
        var lowStockProducts = new List<LowStockProductDto>
        {
            new LowStockProductDto
            {
                Id = 1,
                Name = "Critical Product",
                SKU = "CRIT001",
                CurrentQuantity = 1,
                MinStock = 10,
                MaxStock = 100,
                CategoryName = "Electronics",
                ManufacturerName = "Test Manufacturer",
                Unit = "pcs"
            }
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync(lowStockProducts!);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<LowStockAlert>();

        // Assert
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Critical Product");
        component.Markup.Should().Contain("1");
    }

    [Fact]
    public void LowStockAlert_ShouldSortByQuantity()
    {
        // Arrange
        var lowStockProducts = new List<LowStockProductDto>
        {
            new LowStockProductDto
            {
                Id = 1,
                Name = "Product 1",
                SKU = "TEST001",
                CurrentQuantity = 5,
                MinStock = 10,
                MaxStock = 100,
                CategoryName = "Electronics",
                ManufacturerName = "Test Manufacturer",
                Unit = "pcs"
            },
            new LowStockProductDto
            {
                Id = 2,
                Name = "Product 2",
                SKU = "TEST002",
                CurrentQuantity = 2,
                MinStock = 10,
                MaxStock = 100,
                CategoryName = "Electronics",
                ManufacturerName = "Test Manufacturer",
                Unit = "pcs"
            }
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync(lowStockProducts!);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<LowStockAlert>();

        // Assert
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Product 1");
        component.Markup.Should().Contain("Product 2");
    }
}
