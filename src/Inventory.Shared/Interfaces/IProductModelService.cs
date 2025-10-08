using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IProductModelService
{
    Task<List<ProductModelDto>> GetAllProductModelsAsync();
    Task<ProductModelDto?> GetProductModelByIdAsync(int id);
    Task<ProductModelDto> CreateProductModelAsync(CreateProductModelDto createProductModelDto);
    Task<ProductModelDto?> UpdateProductModelAsync(int id, UpdateProductModelDto updateProductModelDto);
    Task<bool> DeleteProductModelAsync(int id);
}
