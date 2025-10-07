using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace Inventory.Shared.Services;

#pragma warning disable CS9113 // Parameter is not read
public class WarehouseApiService(HttpClient httpClient, ILogger<WarehouseApiService> logger, IRetryService? retryService = null, INotificationService? notificationService = null) : BaseApiService(httpClient, ApiEndpoints.Warehouses, logger), IWarehouseService
{
    public async Task<List<WarehouseDto>> GetAllWarehousesAsync()
    {
        // Request all warehouses by setting a large page size
        var response = await GetPagedAsync<WarehouseDto>($"{BaseUrl}?page=1&pageSize=1000");
        return response.Data?.Items ?? new List<WarehouseDto>();
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(int id)
    {
        var endpoint = ApiEndpoints.WarehouseById.Replace("{id}", id.ToString(CultureInfo.InvariantCulture));
        var response = await GetAsync<WarehouseDto>(endpoint);
        return response.Success ? response.Data : null;
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto createWarehouseDto)
    {
        var response = await PostAsync<WarehouseDto>(BaseUrl, createWarehouseDto);
        if (response.Success && response.Data != null)
        {
            return response.Data;
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "Failed to create warehouse");
    }

    public async Task<WarehouseDto?> UpdateWarehouseAsync(int id, UpdateWarehouseDto updateWarehouseDto)
    {
        var endpoint = ApiEndpoints.WarehouseById.Replace("{id}", id.ToString(CultureInfo.InvariantCulture));
        var response = await PutAsync<WarehouseDto>(endpoint, updateWarehouseDto);
        return response.Success ? response.Data : null;
    }

    public async Task<bool> DeleteWarehouseAsync(int id)
    {
        var endpoint = ApiEndpoints.WarehouseById.Replace("{id}", id.ToString(CultureInfo.InvariantCulture));
        var response = await DeleteAsync(endpoint);
        return response.Success;
    }
}
#pragma warning restore CS9113 // Parameter is not read
