using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

public class WebDashboardApiService : WebBaseApiService, IDashboardService
{
    public WebDashboardApiService(
        HttpClient httpClient, 
        IUrlBuilderService urlBuilderService, 
        IResilientApiService resilientApiService,
        IApiErrorHandler errorHandler,
        IRequestValidator requestValidator, 
        IAutoTokenRefreshService autoTokenRefreshService,
        ILogger<WebDashboardApiService> logger) 
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, autoTokenRefreshService, logger)
    {
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var response = await GetAsync<DashboardStatsDto>(ApiEndpoints.DashboardStats);
        return response.Data ?? new DashboardStatsDto();
    }

    public async Task<RecentActivityDto> GetRecentActivityAsync()
    {
        var response = await GetAsync<RecentActivityDto>(ApiEndpoints.DashboardRecentActivity);
        return response.Data ?? new RecentActivityDto();
    }

    public async Task<List<LowStockProductDto>> GetLowStockProductsAsync()
    {
        var response = await GetAsync<List<LowStockProductDto>>(ApiEndpoints.DashboardLowStockProducts);
        return response.Data ?? new List<LowStockProductDto>();
    }
}
