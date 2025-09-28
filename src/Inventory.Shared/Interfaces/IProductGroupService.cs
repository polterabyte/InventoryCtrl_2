using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IProductGroupService
{
    Task<List<ProductGroupDto>> GetAllProductGroupsAsync();
    Task<PagedApiResponse<ProductGroupDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, bool? isActive = null);
    Task<ProductGroupDto?> GetProductGroupByIdAsync(int id);
    Task<ProductGroupDto> CreateProductGroupAsync(CreateProductGroupDto createProductGroupDto);
    Task<ProductGroupDto?> UpdateProductGroupAsync(int id, UpdateProductGroupDto updateProductGroupDto);
    Task<bool> DeleteProductGroupAsync(int id);
}
