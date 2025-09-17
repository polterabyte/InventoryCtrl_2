using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class ManufacturerApiService(HttpClient httpClient, ILogger<ManufacturerApiService> logger) 
    : BaseApiService(httpClient, ApiEndpoints.Manufacturers, logger), IManufacturerService
{

    public async Task<List<ManufacturerDto>> GetAllManufacturersAsync()
    {
        var response = await GetAsync<List<ManufacturerDto>>(BaseUrl);
        return response?.Data ?? new List<ManufacturerDto>();
    }

    public async Task<ManufacturerDto?> GetManufacturerByIdAsync(int id)
    {
        var response = await GetAsync<ManufacturerDto>($"{BaseUrl}/{id}");
        return response?.Data;
    }

    public async Task<ManufacturerDto> CreateManufacturerAsync(CreateManufacturerDto createManufacturerDto)
    {
        var response = await PostAsync<ManufacturerDto>(BaseUrl, createManufacturerDto);
        if (response?.Data == null)
        {
            throw new InvalidOperationException("Failed to create manufacturer");
        }
        return response.Data;
    }

    public async Task<ManufacturerDto?> UpdateManufacturerAsync(int id, UpdateManufacturerDto updateManufacturerDto)
    {
        var response = await PutAsync<ManufacturerDto>($"{BaseUrl}/{id}", updateManufacturerDto);
        return response?.Data;
    }

    public async Task<bool> DeleteManufacturerAsync(int id)
    {
        var response = await DeleteAsync($"{BaseUrl}/{id}");
        return response?.Data ?? false;
    }
}
