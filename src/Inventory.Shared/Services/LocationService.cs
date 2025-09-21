using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Inventory.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class LocationApiService(HttpClient httpClient, ILogger<LocationApiService> logger) 
    : BaseApiService(httpClient, ApiEndpoints.Locations, logger), ILocationService
{

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
