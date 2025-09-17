using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IUnitOfMeasureApiService
{
    Task<ApiResponse<List<UnitOfMeasureDto>>> GetAllAsync();
    Task<PagedApiResponse<UnitOfMeasureDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, bool? isActive = null);
    Task<ApiResponse<UnitOfMeasureDto>> GetByIdAsync(int id);
    Task<ApiResponse<UnitOfMeasureDto>> CreateAsync(CreateUnitOfMeasureDto createDto);
    Task<ApiResponse<UnitOfMeasureDto>> UpdateAsync(int id, UpdateUnitOfMeasureDto updateDto);
    Task<ApiResponse<object>> DeleteAsync(int id);
    Task<ApiResponse<bool>> ExistsAsync(string symbol);
    Task<ApiResponse<int>> GetCountAsync(bool? isActive = null);
}
