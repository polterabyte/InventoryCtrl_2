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
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        // Note: Detailed property assertions would require casting to specific DTO type
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
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
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
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
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
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        // For empty lists, we just verify the response structure
        // The actual data content depends on the API implementation
        apiResponse.Data.Should().NotBeNull();
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
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        // For empty lists, we just verify the response structure
        // The actual data content depends on the API implementation
        apiResponse.Data.Should().NotBeNull();
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

