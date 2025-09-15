using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class ManufacturerApiService : BaseApiService, IManufacturerService
{
    private const string BaseEndpoint = "/api/manufacturers";

    public ManufacturerApiService(HttpClient httpClient, ILogger<ManufacturerApiService> logger) 
        : base(httpClient, BaseEndpoint, logger)
    {
    }

    public async Task<List<ManufacturerDto>> GetAllManufacturersAsync()
    {
        var response = await GetAsync<List<ManufacturerDto>>(BaseEndpoint);
        return response?.Data ?? new List<ManufacturerDto>();
    }

    public async Task<ManufacturerDto?> GetManufacturerByIdAsync(int id)
    {
        var response = await GetAsync<ManufacturerDto>($"{BaseEndpoint}/{id}");
        return response?.Data;
    }

    public async Task<ManufacturerDto> CreateManufacturerAsync(CreateManufacturerDto createManufacturerDto)
    {
        var response = await PostAsync<ManufacturerDto>(BaseEndpoint, createManufacturerDto);
        if (response?.Data == null)
        {
            throw new InvalidOperationException("Failed to create manufacturer");
        }
        return response.Data;
    }

    public async Task<ManufacturerDto?> UpdateManufacturerAsync(int id, UpdateManufacturerDto updateManufacturerDto)
    {
        var response = await PutAsync<ManufacturerDto>($"{BaseEndpoint}/{id}", updateManufacturerDto);
        return response?.Data;
    }

    public async Task<bool> DeleteManufacturerAsync(int id)
    {
        var response = await DeleteAsync($"{BaseEndpoint}/{id}");
        return response?.Data ?? false;
    }
}
