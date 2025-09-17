using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IWarehouseService
{
    Task<List<WarehouseDto>> GetAllWarehousesAsync();
    Task<WarehouseDto?> GetWarehouseByIdAsync(int id);
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto createWarehouseDto);
    Task<WarehouseDto?> UpdateWarehouseAsync(int id, UpdateWarehouseDto updateWarehouseDto);
    Task<bool> DeleteWarehouseAsync(int id);
}
