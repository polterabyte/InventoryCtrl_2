using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class DashboardApiService(HttpClient httpClient, ILogger<DashboardApiService> logger, IRetryService? retryService = null, INotificationService? notificationService = null) 
    : BaseApiService(httpClient, ApiEndpoints.Dashboard, logger), IDashboardService
{
    private readonly IRetryService? _retryService = retryService;
    private readonly INotificationService? _notificationService = notificationService;

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await GetAsync<DashboardStatsDto>(ApiEndpoints.DashboardStats);
                    return response.Data ?? new DashboardStatsDto();
                },
                "GetDashboardStats"
            );
        }
        
        var response = await GetAsync<DashboardStatsDto>(ApiEndpoints.DashboardStats);
        return response.Data ?? new DashboardStatsDto();
    }

    public async Task<RecentActivityDto> GetRecentActivityAsync()
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await GetAsync<RecentActivityDto>(ApiEndpoints.DashboardRecentActivity);
                    return response.Data ?? new RecentActivityDto();
                },
                "GetRecentActivity"
            );
        }
        
        var response = await GetAsync<RecentActivityDto>(ApiEndpoints.DashboardRecentActivity);
        return response.Data ?? new RecentActivityDto();
    }

    public async Task<List<LowStockProductDto>> GetLowStockProductsAsync()
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await GetAsync<List<LowStockProductDto>>(ApiEndpoints.DashboardLowStockProducts);
                    return response.Data ?? new List<LowStockProductDto>();
                },
                "GetLowStockProducts"
            );
        }
        
        var response = await GetAsync<List<LowStockProductDto>>(ApiEndpoints.DashboardLowStockProducts);
        return response.Data ?? new List<LowStockProductDto>();
    }

    public async Task<List<LowStockKanbanDto>> GetLowStockKanbanAsync()
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await GetAsync<List<LowStockKanbanDto>>(ApiEndpoints.DashboardLowStockKanban);
                    return response.Data ?? new List<LowStockKanbanDto>();
                },
                "GetLowStockKanban"
            );
        }

        var resp = await GetAsync<List<LowStockKanbanDto>>(ApiEndpoints.DashboardLowStockKanban);
        return resp.Data ?? new List<LowStockKanbanDto>();
    }
}
