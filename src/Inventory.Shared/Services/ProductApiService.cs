using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class ProductApiService : BaseApiService, IProductService
{
    public ProductApiService(HttpClient httpClient, ILogger<ProductApiService> logger) 
        : base(httpClient, ApiEndpoints.Products, logger)
    {
    }

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        var response = await GetAsync<List<ProductDto>>(BaseUrl);
        return response.Data ?? new List<ProductDto>();
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var endpoint = ApiEndpoints.ProductById.Replace("{id}", id.ToString());
        var response = await GetAsync<ProductDto>(endpoint);
        return response.Data;
    }

    public async Task<ProductDto?> GetProductBySkuAsync(string sku)
    {
        var endpoint = ApiEndpoints.ProductBySku.Replace("{sku}", sku);
        var response = await GetAsync<ProductDto>(endpoint);
        return response.Data;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
    {
        var response = await PostAsync<ProductDto>(BaseUrl, createProductDto);
        return response.Data ?? new ProductDto();
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
    {
        var endpoint = ApiEndpoints.ProductById.Replace("{id}", id.ToString());
        var response = await PutAsync<ProductDto>(endpoint, updateProductDto);
        return response.Data;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var endpoint = ApiEndpoints.ProductById.Replace("{id}", id.ToString());
        var response = await DeleteAsync(endpoint);
        return response.Success;
    }

    public async Task<bool> AdjustStockAsync(ProductStockAdjustmentDto adjustmentDto)
    {
        var endpoint = ApiEndpoints.ProductStockAdjust.Replace("{id}", adjustmentDto.ProductId.ToString());
        var response = await PostAsync<bool>(endpoint, adjustmentDto);
        return response.Data;
    }

    public async Task<List<ProductDto>> GetProductsByCategoryAsync(int categoryId)
    {
        var endpoint = ApiEndpoints.ProductsByCategory.Replace("{categoryId}", categoryId.ToString());
        var response = await GetAsync<List<ProductDto>>(endpoint);
        return response.Data ?? new List<ProductDto>();
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        var response = await GetAsync<List<ProductDto>>(ApiEndpoints.LowStockProducts);
        return response.Data ?? new List<ProductDto>();
    }

    public async Task<List<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        var endpoint = $"{ApiEndpoints.SearchProducts}?term={Uri.EscapeDataString(searchTerm)}";
        var response = await GetAsync<List<ProductDto>>(endpoint);
        return response.Data ?? new List<ProductDto>();
    }
}
