using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

public class WebManufacturerApiService : WebBaseApiService, IManufacturerService
{
    public WebManufacturerApiService(
        HttpClient httpClient, 
        IApiUrlService apiUrlService, 
        IResilientApiService resilientApiService, 
        ILogger<WebManufacturerApiService> logger,
        IJSRuntime jsRuntime) 
        : base(httpClient, apiUrlService, resilientApiService, logger, jsRuntime)
    {
    }

    public async Task<List<ManufacturerDto>> GetAllManufacturersAsync()
    {
        var response = await GetAsync<List<ManufacturerDto>>(ApiEndpoints.Manufacturers);
        return response?.Data ?? new List<ManufacturerDto>();
    }

    public async Task<ManufacturerDto?> GetManufacturerByIdAsync(int id)
    {
        var response = await GetAsync<ManufacturerDto>($"{ApiEndpoints.Manufacturers}/{id}");
        return response?.Data;
    }

    public async Task<ManufacturerDto> CreateManufacturerAsync(CreateManufacturerDto createManufacturerDto)
    {
        var response = await PostAsync<ManufacturerDto>(ApiEndpoints.Manufacturers, createManufacturerDto);
        if (response?.Data == null)
        {
            throw new InvalidOperationException("Failed to create manufacturer");
        }
        return response.Data;
    }

    public async Task<ManufacturerDto?> UpdateManufacturerAsync(int id, UpdateManufacturerDto updateManufacturerDto)
    {
        var response = await PutAsync<ManufacturerDto>($"{ApiEndpoints.Manufacturers}/{id}", updateManufacturerDto);
        return response?.Data;
    }

    public async Task<bool> DeleteManufacturerAsync(int id)
    {
        var response = await DeleteAsync($"{ApiEndpoints.Manufacturers}/{id}");
        return response?.Data ?? false;
    }
}
