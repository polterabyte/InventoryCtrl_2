using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

public class WebWarehouseApiService : WebBaseApiService, IWarehouseService
{
    public WebWarehouseApiService(
        HttpClient httpClient, 
        IUrlBuilderService urlBuilderService, 
        IResilientApiService resilientApiService, 
        IApiErrorHandler errorHandler,        IRequestValidator requestValidator,
        ILogger<WebWarehouseApiService> logger)
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, logger)
    {
    }

    public async Task<List<WarehouseDto>> GetAllWarehousesAsync()
    {
        // Request all warehouses by setting a large page size
        var response = await GetPagedAsync<WarehouseDto>($"{ApiEndpoints.Warehouses}?page=1&pageSize=1000");
        return response.Data?.Items ?? new List<WarehouseDto>();
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(int id)
    {
        var endpoint = ApiEndpoints.WarehouseById.Replace("{id}", id.ToString());
        var response = await GetAsync<WarehouseDto>(endpoint);
        return response.Success ? response.Data : null;
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto createWarehouseDto)
    {
        var response = await PostAsync<WarehouseDto>(ApiEndpoints.Warehouses, createWarehouseDto);
        if (response.Success && response.Data != null)
        {
            return response.Data;
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "Failed to create warehouse");
    }

    public async Task<WarehouseDto?> UpdateWarehouseAsync(int id, UpdateWarehouseDto updateWarehouseDto)
    {
        var endpoint = ApiEndpoints.WarehouseById.Replace("{id}", id.ToString());
        var response = await PutAsync<WarehouseDto>(endpoint, updateWarehouseDto);
        return response.Success ? response.Data : null;
    }

    public async Task<bool> DeleteWarehouseAsync(int id)
    {
        var endpoint = ApiEndpoints.WarehouseById.Replace("{id}", id.ToString());
        var response = await DeleteAsync(endpoint);
        return response.Success;
    }
}
