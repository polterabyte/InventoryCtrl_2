using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Inventory.Shared.Services;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Xunit;

namespace Inventory.UnitTests.Services;

public class DashboardApiServiceTests
{
    private readonly Mock<HttpClient> _httpClientMock = new();
    private readonly Mock<ILogger<DashboardApiService>> _loggerMock = new();
    private readonly Mock<IRetryService> _retryServiceMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();

    [Fact]
    public async Task GetDashboardStatsAsync_WithRetryService_ShouldUseRetryService()
    {
        var expectedStats = new DashboardStatsDto
        {
            TotalProducts = 10,
            TotalCategories = 5,
            TotalManufacturers = 3,
            TotalWarehouses = 2,
            LowStockProducts = 2,
            OutOfStockProducts = 1,
            RecentTransactions = 15,
            RecentProducts = 3
        };

        _retryServiceMock
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<DashboardStatsDto>>>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(expectedStats);

        var service = new DashboardApiService(
            _httpClientMock.Object,
            _loggerMock.Object,
            _retryServiceMock.Object,
            _notificationServiceMock.Object
        );

        var result = await service.GetDashboardStatsAsync();

        result.Should().BeEquivalentTo(expectedStats);
        _retryServiceMock.Verify(
            x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<DashboardStatsDto>>>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_WithRetryService_ShouldReturnAggregatedProducts()
    {
        var expectedProducts = new List<LowStockProductDto>
        {
            new LowStockProductDto
            {
                ProductId = 1,
                ProductName = "Low Stock Product",
                CategoryName = "Test Category",
                UnitOfMeasureSymbol = "pcs",
                KanbanCards = new List<LowStockKanbanDto>
                {
                    new LowStockKanbanDto
                    {
                        KanbanCardId = 10,
                        ProductId = 1,
                        ProductName = "Low Stock Product",
                        CategoryName = "Test Category",
                        WarehouseId = 100,
                        WarehouseName = "Main WH",
                        CurrentQuantity = 5,
                        MinThreshold = 10,
                        MaxThreshold = 80,
                        UnitOfMeasureSymbol = "pcs"
                    }
                }
            }
        };

        _retryServiceMock
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<List<LowStockProductDto>>>>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(expectedProducts);

        var service = new DashboardApiService(
            _httpClientMock.Object,
            _loggerMock.Object,
            _retryServiceMock.Object,
            _notificationServiceMock.Object
        );

        var result = await service.GetLowStockProductsAsync();

        result.Should().BeEquivalentTo(expectedProducts);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_WithoutRetryService_ShouldReturnList()
    {
        var service = new DashboardApiService(
            _httpClientMock.Object,
            _loggerMock.Object,
            null,
            null
        );

        var result = await service.GetLowStockProductsAsync();

        result.Should().NotBeNull();
        result.Should().BeOfType<List<LowStockProductDto>>();
    }

    [Fact]
    public async Task GetRecentActivityAsync_WithRetryService_ShouldUseRetryService()
    {
        var expectedActivity = new RecentActivityDto
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
                    WarehouseName = "Test Warehouse",
                    Description = "Test transaction"
                }
            },
            RecentProducts = new List<RecentProductDto>
            {
                new RecentProductDto
                {
                    Id = 1,
                    Name = "Test Product",
                    Quantity = 50,
                    CategoryName = "Test Category",
                    ManufacturerName = "Test Manufacturer",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _retryServiceMock
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<RecentActivityDto>>>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(expectedActivity);

        var service = new DashboardApiService(
            _httpClientMock.Object,
            _loggerMock.Object,
            _retryServiceMock.Object,
            _notificationServiceMock.Object
        );

        var result = await service.GetRecentActivityAsync();

        result.Should().BeEquivalentTo(expectedActivity);
    }
}
