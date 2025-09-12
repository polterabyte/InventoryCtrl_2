using Inventory.Shared.DTOs;
using Inventory.Shared.Models;

namespace Inventory.Shared.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto?> GetProductBySkuAsync(string sku);
    Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
    Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> AdjustStockAsync(ProductStockAdjustmentDto adjustmentDto);
    Task<List<ProductDto>> GetProductsByCategoryAsync(int categoryId);
    Task<List<ProductDto>> GetLowStockProductsAsync();
    Task<List<ProductDto>> SearchProductsAsync(string searchTerm);
}
