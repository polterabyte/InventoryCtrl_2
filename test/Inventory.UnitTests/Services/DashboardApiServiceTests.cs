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
    private readonly Mock<HttpClient> _httpClientMock;
    private readonly Mock<ILogger<DashboardApiService>> _loggerMock;
    private readonly Mock<IRetryService> _retryServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;

    public DashboardApiServiceTests()
    {
        _httpClientMock = new Mock<HttpClient>();
        _loggerMock = new Mock<ILogger<DashboardApiService>>();
        _retryServiceMock = new Mock<IRetryService>();
        _notificationServiceMock = new Mock<INotificationService>();
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithRetryService_ShouldUseRetryService()
    {
        // Arrange
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

        // Act
        var result = await service.GetDashboardStatsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedStats);
        _retryServiceMock.Verify(
            x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<DashboardStatsDto>>>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithoutRetryService_ShouldCallBaseApiService()
    {
        // Arrange
        var service = new DashboardApiService(
            _httpClientMock.Object,
            _loggerMock.Object,
            null,
            null
        );

        // Act
        var result = await service.GetDashboardStatsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<DashboardStatsDto>();
    }

    [Fact]
    public async Task GetRecentActivityAsync_WithRetryService_ShouldUseRetryService()
    {
        // Arrange
        var expectedActivity = new RecentActivityDto
        {
            RecentTransactions = new List<RecentTransactionDto>
            {
                new RecentTransactionDto
                {
                    Id = 1,
                    ProductName = "Test Product",
                    ProductSku = "TEST001",
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
                    SKU = "TEST001",
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

        // Act
        var result = await service.GetRecentActivityAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedActivity);
        _retryServiceMock.Verify(
            x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<RecentActivityDto>>>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetLowStockProductsAsync_WithRetryService_ShouldUseRetryService()
    {
        // Arrange
        var expectedProducts = new List<LowStockProductDto>
        {
            new LowStockProductDto
            {
                Id = 1,
                Name = "Low Stock Product",
                SKU = "LOW001",
                CurrentQuantity = 5,
                MinStock = 10,
                MaxStock = 100,
                CategoryName = "Test Category",
                ManufacturerName = "Test Manufacturer",
                Unit = "pcs"
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

        // Act
        var result = await service.GetLowStockProductsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedProducts);
        _retryServiceMock.Verify(
            x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<List<LowStockProductDto>>>>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetLowStockProductsAsync_WithoutRetryService_ShouldCallBaseApiService()
    {
        // Arrange
        var service = new DashboardApiService(
            _httpClientMock.Object,
            _loggerMock.Object,
            null,
            null
        );

        // Act
        var result = await service.GetLowStockProductsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<LowStockProductDto>>();
    }

    [Fact]
    public async Task GetRecentActivityAsync_WithoutRetryService_ShouldCallBaseApiService()
    {
        // Arrange
        var service = new DashboardApiService(
            _httpClientMock.Object,
            _loggerMock.Object,
            null,
            null
        );

        // Act
        var result = await service.GetRecentActivityAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<RecentActivityDto>();
    }
}
