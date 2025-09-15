using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IManufacturerService
{
    Task<List<ManufacturerDto>> GetAllManufacturersAsync();
    Task<ManufacturerDto?> GetManufacturerByIdAsync(int id);
    Task<ManufacturerDto> CreateManufacturerAsync(CreateManufacturerDto createManufacturerDto);
    Task<ManufacturerDto?> UpdateManufacturerAsync(int id, UpdateManufacturerDto updateManufacturerDto);
    Task<bool> DeleteManufacturerAsync(int id);
}
