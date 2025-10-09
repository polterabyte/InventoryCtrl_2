using Bunit;
using FluentAssertions;
using Inventory.UI.Components.Dashboard;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Moq;
using Xunit;
#pragma warning disable CS8620 // Argument of type cannot be used for parameter due to differences in the nullability of reference types

namespace Inventory.ComponentTests.Components.Dashboard;

public class RecentActivityTests : ComponentTestBase
{
    [Fact]
    public void RecentActivity_WithValidData_ShouldRenderCorrectly()
    {
        // Arrange
        var activity = new RecentActivityDto
        {
            RecentTransactions = new List<RecentTransactionDto>
            {
                new RecentTransactionDto
                {
                    Id = 1,
                    ProductName = "Test Product 1",
                    Type = "Income",
                    Quantity = 10,
                    Date = DateTime.UtcNow.AddDays(-1),
                    UserName = "testuser",
                    WarehouseName = "Main Warehouse",
                    Description = "Test transaction"
                },
                new RecentTransactionDto
                {
                    Id = 2,
                    ProductName = "Test Product 2",
                    Type = "Outcome",
                    Quantity = 5,
                    Date = DateTime.UtcNow.AddDays(-2),
                    UserName = "testuser",
                    WarehouseName = "Secondary Warehouse",
                    Description = "Test transaction 2"
                }
            },
            RecentProducts = new List<RecentProductDto>
            {
                new RecentProductDto
                {
                    Id = 1,
                    Name = "New Product 1",
                    Quantity = 50,
                    CategoryName = "Electronics",
                    ManufacturerName = "Test Manufacturer",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new RecentProductDto
                {
                    Id = 2,
                    Name = "New Product 2",
                    Quantity = 25,
                    CategoryName = "Accessories",
                    ManufacturerName = "Test Manufacturer 2",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetRecentActivityAsync())
            .ReturnsAsync(activity!);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<RecentActivity>();

        // Assert
        component.Should().NotBeNull();
        component.Find(".card").Should().NotBeNull();
        
        // Check if transactions are displayed
        component.Markup.Should().Contain("Test Product 1");
        component.Markup.Should().Contain("TEST001");
        component.Markup.Should().Contain("Приход");
        component.Markup.Should().Contain("10");
    }

    [Fact]
    public void RecentActivity_WithEmptyData_ShouldDisplayEmptyState()
    {
        // Arrange
        var activity = new RecentActivityDto
        {
            RecentTransactions = new List<RecentTransactionDto>(),
            RecentProducts = new List<RecentProductDto>()
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetRecentActivityAsync())
            .ReturnsAsync(activity!);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<RecentActivity>();

        // Assert
        component.Should().NotBeNull();
        component.Find(".card").Should().NotBeNull();
        component.Markup.Should().Contain("Нет недавней активности");
    }

    [Fact]
    public void RecentActivity_WithNullData_ShouldHandleGracefully()
    {
        // Arrange
        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetRecentActivityAsync())
            .ReturnsAsync((RecentActivityDto?)null);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<RecentActivity>();

        // Assert
        component.Should().NotBeNull();
        component.Find(".card").Should().NotBeNull();
    }

    [Fact]
    public void RecentActivity_ShouldHaveCorrectCssClasses()
    {
        // Arrange
        var activity = new RecentActivityDto
        {
            RecentTransactions = new List<RecentTransactionDto>
            {
                new RecentTransactionDto
                {
                    Id = 1,
                    ProductName = "Test Product",
                    Type = "Income",
                    Quantity = 10,
                    Date = DateTime.UtcNow,
                    UserName = "testuser",
                    WarehouseName = "Main Warehouse",
                    Description = "Test"
                }
            },
            RecentProducts = new List<RecentProductDto>()
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetRecentActivityAsync())
            .ReturnsAsync(activity!);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<RecentActivity>();

        // Assert
        component.Find(".card").Should().NotBeNull();
        component.Find(".card-header").Should().NotBeNull();
        component.Find(".card-body").Should().NotBeNull();
        component.Find(".activity-list").Should().NotBeNull();
    }

    [Fact]
    public void RecentActivity_ShouldFormatDatesCorrectly()
    {
        // Arrange
        var testDate = DateTime.UtcNow.AddDays(-1);
        var activity = new RecentActivityDto
        {
            RecentTransactions = new List<RecentTransactionDto>
            {
                new RecentTransactionDto
                {
                    Id = 1,
                    ProductName = "Test Product",
                    Type = "Income",
                    Quantity = 10,
                    Date = testDate,
                    UserName = "testuser",
                    WarehouseName = "Main Warehouse",
                    Description = "Test"
                }
            },
            RecentProducts = new List<RecentProductDto>()
        };

        var mockDashboardService = new Mock<IDashboardService>();
        mockDashboardService.Setup(x => x.GetRecentActivityAsync())
            .ReturnsAsync(activity!);

        AddSingletonService<IDashboardService>(mockDashboardService.Object);

        // Act
        var component = RenderComponent<RecentActivity>();

        // Assert
        component.Should().NotBeNull();
        // The exact date format depends on the component implementation
        component.Markup.Should().Contain(testDate.ToString("dd.MM.yyyy"));
    }
}
