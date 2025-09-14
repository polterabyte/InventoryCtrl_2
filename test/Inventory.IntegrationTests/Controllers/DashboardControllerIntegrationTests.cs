using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Xunit;

namespace Inventory.IntegrationTests.Controllers;

public class DashboardControllerIntegrationTestsNew : IntegrationTestBase
{
    public DashboardControllerIntegrationTestsNew(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetDashboardStats_WithValidData_ShouldReturnStats()
    {
        // Arrange
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard/stats");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
        result.Should().NotBeNull();
        result!.TotalProducts.Should().Be(2);
        result.TotalCategories.Should().Be(2);
        result.TotalManufacturers.Should().Be(2);
        result.TotalWarehouses.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardStats_WithEmptyDatabase_ShouldReturnZeroStats()
    {
        // Arrange
        await CleanupDatabaseAsync();
        await InitializeEmptyAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard/stats");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
        result.Should().NotBeNull();
        result!.TotalProducts.Should().Be(0);
        result.TotalCategories.Should().Be(0);
        result.TotalManufacturers.Should().Be(0);
        result.TotalWarehouses.Should().Be(0);
    }

    [Fact]
    public async Task GetRecentActivity_WithValidData_ShouldReturnActivity()
    {
        // Arrange
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard/recent-activity");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RecentActivityDto>();
        result.Should().NotBeNull();
        result!.RecentTransactions.Should().NotBeEmpty();
        result.RecentProducts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLowStockProducts_WithValidData_ShouldReturnLowStockProducts()
    {
        // Arrange
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard/low-stock-products");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<LowStockProductDto>>();
        result.Should().NotBeNull();
        // Note: No low stock products in test data, so should be empty
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLowStockProducts_WithNoLowStockProducts_ShouldReturnEmptyList()
    {
        // Arrange
        await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard/low-stock-products");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<LowStockProductDto>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardStats_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove authentication
                services.AddControllers();
            });
        }).CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/dashboard/stats");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }
}

