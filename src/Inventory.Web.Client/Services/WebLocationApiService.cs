using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

public class WebLocationApiService : WebBaseApiService, ILocationService
{
    public WebLocationApiService(
        HttpClient httpClient,
        IApiUrlService apiUrlService,
        IResilientApiService resilientApiService,
        ILogger<WebLocationApiService> logger,
        IJSRuntime jsRuntime)
        : base(httpClient, apiUrlService, resilientApiService, logger, jsRuntime)
    {
    }

    public async Task<IEnumerable<LocationDto>> GetAllLocationsAsync()
    {
        var response = await GetAsync<IEnumerable<LocationDto>>(ApiEndpoints.AllLocations);
        return response.Success ? response.Data ?? Enumerable.Empty<LocationDto>() : Enumerable.Empty<LocationDto>();
    }

    public async Task<LocationDto?> GetLocationByIdAsync(int id)
    {
        var endpoint = ApiEndpoints.LocationById.Replace("{id}", id.ToString());
        var response = await GetAsync<LocationDto>(endpoint);
        return response.Success ? response.Data : null;
    }

    public async Task<LocationDto?> CreateLocationAsync(CreateLocationDto createLocationDto)
    {
        var response = await PostAsync<LocationDto>(ApiEndpoints.Locations, createLocationDto);
        return response.Success ? response.Data : null;
    }

    public async Task<LocationDto?> UpdateLocationAsync(int id, UpdateLocationDto updateLocationDto)
    {
        var endpoint = ApiEndpoints.LocationById.Replace("{id}", id.ToString());
        var response = await PutAsync<LocationDto>(endpoint, updateLocationDto);
        return response.Success ? response.Data : null;
    }

    public async Task<bool> DeleteLocationAsync(int id)
    {
        var endpoint = ApiEndpoints.LocationById.Replace("{id}", id.ToString());
        var response = await DeleteAsync(endpoint);
        return response.Success;
    }

    public async Task<IEnumerable<LocationDto>> GetLocationsByParentIdAsync(int? parentId)
    {
        var endpoint = parentId.HasValue 
            ? ApiEndpoints.LocationByParentId.Replace("{parentId}", parentId.Value.ToString())
            : ApiEndpoints.RootLocations;
        
        var response = await GetAsync<IEnumerable<LocationDto>>(endpoint);
        return response.Success ? response.Data ?? Enumerable.Empty<LocationDto>() : Enumerable.Empty<LocationDto>();
    }
}
