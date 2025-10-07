using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IKanbanCardService
{
    Task<List<KanbanCardDto>> GetAllAsync(int? productId = null, int? warehouseId = null);
    Task<KanbanCardDto?> GetByIdAsync(int id);
    Task<KanbanCardDto> CreateAsync(CreateKanbanCardDto dto);
    Task<KanbanCardDto?> UpdateAsync(int id, UpdateKanbanCardDto dto);
    Task<bool> DeleteAsync(int id);
}

