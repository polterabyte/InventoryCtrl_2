using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<RecentActivityDto> GetRecentActivityAsync();
    Task<List<LowStockProductDto>> GetLowStockProductsAsync();
}
