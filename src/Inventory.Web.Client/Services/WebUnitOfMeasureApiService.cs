using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

public class WebUnitOfMeasureApiService : WebBaseApiService, IUnitOfMeasureApiService
{
    public WebUnitOfMeasureApiService(
        HttpClient httpClient, 
        IApiUrlService apiUrlService, 
        IResilientApiService resilientApiService, 
        ILogger<WebUnitOfMeasureApiService> logger,
        IJSRuntime jsRuntime) 
        : base(httpClient, apiUrlService, resilientApiService, logger, jsRuntime)
    {
    }

    public async Task<ApiResponse<List<UnitOfMeasureDto>>> GetAllAsync()
    {
        var response = await GetAsync<List<UnitOfMeasureDto>>(ApiEndpoints.UnitOfMeasures);
        return response;
    }

    public async Task<PagedApiResponse<UnitOfMeasureDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, bool? isActive = null)
    {
        var queryParams = new List<string>();
        if (page > 1) queryParams.Add($"page={page}");
        if (pageSize != 10) queryParams.Add($"pageSize={pageSize}");
        if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (isActive.HasValue) queryParams.Add($"isActive={isActive.Value}");
        
        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var response = await GetPagedAsync<UnitOfMeasureDto>($"{ApiEndpoints.UnitOfMeasures}{queryString}");
        return response;
    }

    public async Task<ApiResponse<UnitOfMeasureDto>> GetByIdAsync(int id)
    {
        var response = await GetAsync<UnitOfMeasureDto>($"{ApiEndpoints.UnitOfMeasures}/{id}");
        return response;
    }

    public async Task<ApiResponse<UnitOfMeasureDto>> CreateAsync(CreateUnitOfMeasureDto createDto)
    {
        var response = await PostAsync<UnitOfMeasureDto>(ApiEndpoints.UnitOfMeasures, createDto);
        return response;
    }

    public async Task<ApiResponse<UnitOfMeasureDto>> UpdateAsync(int id, UpdateUnitOfMeasureDto updateDto)
    {
        var response = await PutAsync<UnitOfMeasureDto>($"{ApiEndpoints.UnitOfMeasures}/{id}", updateDto);
        return response;
    }

    public async Task<ApiResponse<object>> DeleteAsync(int id)
    {
        var response = await DeleteAsync($"{ApiEndpoints.UnitOfMeasures}/{id}");
        return new ApiResponse<object> { Success = response.Success, ErrorMessage = response.ErrorMessage };
    }

    public async Task<ApiResponse<bool>> ExistsAsync(string symbol)
    {
        var response = await GetAsync<bool>($"{ApiEndpoints.UnitOfMeasures}/exists?symbol={Uri.EscapeDataString(symbol)}");
        return response;
    }

    public async Task<ApiResponse<int>> GetCountAsync(bool? isActive = null)
    {
        var queryString = isActive.HasValue ? $"?isActive={isActive.Value}" : "";
        var response = await GetAsync<int>($"{ApiEndpoints.UnitOfMeasures}/count{queryString}");
        return response;
    }
}
