using Bunit;
using FluentAssertions;
using Inventory.UI.Components.Dashboard;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Moq;
using Xunit;
#pragma warning disable CS8620

namespace Inventory.ComponentTests.Components.Dashboard;

public class LowStockAlertTests : ComponentTestBase
{
    [Fact]
    public void LowStockAlert_WithValidData_ShouldRenderKanbanItems()
    {
        var items = new List<LowStockKanbanDto>
        {
            new LowStockKanbanDto
            {
                KanbanCardId = 1,
                ProductId = 10,
                ProductName = "Low Stock Product 1",
                SKU = "LOW001",
                CategoryName = "Electronics",
                ManufacturerName = "Test Manufacturer",
                WarehouseId = 100,
                WarehouseName = "Main WH",
                CurrentQuantity = 5,
                MinThreshold = 10,
                MaxThreshold = 80,
                UnitOfMeasureSymbol = "pcs"
            },
            new LowStockKanbanDto
            {
                KanbanCardId = 2,
                ProductId = 11,
                ProductName = "Low Stock Product 2",
                SKU = "LOW002",
                CategoryName = "Accessories",
                ManufacturerName = "Test Manufacturer 2",
                WarehouseId = 200,
                WarehouseName = "Reserve WH",
                CurrentQuantity = 2,
                MinThreshold = 5,
                MaxThreshold = 40,
                UnitOfMeasureSymbol = "units"
            }
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockKanbanAsync())
            .ReturnsAsync(items);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        var component = RenderComponent<LowStockAlert>();

        component.Markup.Should().Contain("Low Stock Product 1 (Main WH)");
        component.Markup.Should().Contain("LOW001");
        component.Markup.Should().Contain("5 pcs");
        component.Markup.Should().Contain("\u041c\u0438\u043d: 10");

        component.Markup.Should().Contain("Low Stock Product 2 (Reserve WH)");
        component.Markup.Should().Contain("LOW002");
        component.Markup.Should().Contain("2 units");
        component.Markup.Should().Contain("\u041c\u0438\u043d: 5");
    }

    [Fact]
    public void LowStockAlert_WithEmptyList_ShouldDisplayEmptyState()
    {
        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockKanbanAsync())
            .ReturnsAsync(new List<LowStockKanbanDto>());

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        var component = RenderComponent<LowStockAlert>();

        component.Markup.Should().Contain("\u041d\u0435\u0442 \u043f\u0440\u0435\u0434\u0443\u043f\u0440\u0435\u0436\u0434\u0435\u043d\u0438\u0439");
    }

    [Fact]
    public void LowStockAlert_WithNullData_ShouldHandleGracefully()
    {
        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockKanbanAsync())
            .ReturnsAsync((List<LowStockKanbanDto>?)null);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        var component = RenderComponent<LowStockAlert>();

        component.Find(".card").Should().NotBeNull();
    }

    [Fact]
    public void LowStockAlert_ShouldShowInfoAlertWhenNoKanbanConfigured()
    {
        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetLowStockKanbanAsync())
            .ReturnsAsync(new List<LowStockKanbanDto>());

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        var component = RenderComponent<LowStockAlert>();

        component.Markup.Should().Contain("Configure minimum and maximum stock per warehouse via Kanban cards");
    }
}
