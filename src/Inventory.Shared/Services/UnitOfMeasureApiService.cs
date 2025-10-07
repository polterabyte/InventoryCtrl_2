using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Inventory.Shared.Constants;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Inventory.Shared.Services;

public class UnitOfMeasureApiService(HttpClient httpClient, ILogger<UnitOfMeasureApiService> logger) 
    : BaseApiService(httpClient, ApiEndpoints.UnitOfMeasures, logger), IUnitOfMeasureApiService
{
    public async Task<ApiResponse<List<UnitOfMeasureDto>>> GetAllAsync()
    {
        return await GetAsync<List<UnitOfMeasureDto>>(ApiEndpoints.UnitOfMeasureAll);
    }

    public async Task<PagedApiResponse<UnitOfMeasureDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, bool? isActive = null)
    {
        var queryParams = new List<string>();
        
        if (page > 1) queryParams.Add($"page={page}");
        if (pageSize != 10) queryParams.Add($"pageSize={pageSize}");
        if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (isActive.HasValue) queryParams.Add($"isActive={isActive.Value}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        return await GetPagedAsync<UnitOfMeasureDto>($"{ApiEndpoints.UnitOfMeasures}{queryString}");
    }

    public async Task<ApiResponse<UnitOfMeasureDto>> GetByIdAsync(int id)
    {
        var endpoint = ApiEndpoints.UnitOfMeasureById.Replace("{id}", id.ToString(CultureInfo.InvariantCulture));
        return await GetAsync<UnitOfMeasureDto>(endpoint);
    }

    public async Task<ApiResponse<UnitOfMeasureDto>> CreateAsync(CreateUnitOfMeasureDto createDto)
    {
        return await PostAsync<UnitOfMeasureDto>(ApiEndpoints.UnitOfMeasures, createDto);
    }

    public async Task<ApiResponse<UnitOfMeasureDto>> UpdateAsync(int id, UpdateUnitOfMeasureDto updateDto)
    {
        var endpoint = ApiEndpoints.UnitOfMeasureById.Replace("{id}", id.ToString(CultureInfo.InvariantCulture));
        return await PutAsync<UnitOfMeasureDto>(endpoint, updateDto);
    }

    public async Task<ApiResponse<object>> DeleteAsync(int id)
    {
        var endpoint = ApiEndpoints.UnitOfMeasureById.Replace("{id}", id.ToString(CultureInfo.InvariantCulture));
        var response = await base.DeleteAsync(endpoint);
        return new ApiResponse<object>
        {
            Success = response.Success,
            ErrorMessage = response.ErrorMessage,
            Data = response.Success ? new { message = "Deleted successfully" } : null
        };
    }

    public async Task<ApiResponse<bool>> ExistsAsync(string symbol)
    {
        return await GetAsync<bool>($"{ApiEndpoints.UnitOfMeasureExists}?identifier={Uri.EscapeDataString(symbol)}");
    }

    public async Task<ApiResponse<int>> GetCountAsync(bool? isActive = null)
    {
        var queryString = isActive.HasValue ? $"?isActive={isActive.Value}" : "";
        return await GetAsync<int>($"{ApiEndpoints.UnitOfMeasureCount}{queryString}");
    }
}
