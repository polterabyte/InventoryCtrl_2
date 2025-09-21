using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface ILocationService
{
    Task<IEnumerable<LocationDto>> GetAllLocationsAsync();
    Task<LocationDto?> GetLocationByIdAsync(int id);
    Task<LocationDto?> CreateLocationAsync(CreateLocationDto createLocationDto);
    Task<LocationDto?> UpdateLocationAsync(int id, UpdateLocationDto updateLocationDto);
    Task<bool> DeleteLocationAsync(int id);
    Task<IEnumerable<LocationDto>> GetLocationsByParentIdAsync(int? parentId);
}
