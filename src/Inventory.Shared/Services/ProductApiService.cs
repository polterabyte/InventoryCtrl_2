using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class ProductApiService(HttpClient httpClient, ILogger<ProductApiService> logger, IRetryService? retryService = null, INotificationService? notificationService = null) 
    : BaseApiService(httpClient, "", logger), IProductService
{
    private readonly IRetryService? _retryService = retryService;
    private readonly INotificationService? _notificationService = notificationService;

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await GetPagedAsync<ProductDto>(ApiEndpoints.Products);
                    return response.Data?.Items ?? new List<ProductDto>();
                },
                "GetAllProducts"
            );
        }
        
        var response = await GetPagedAsync<ProductDto>(ApiEndpoints.Products);
        return response.Data?.Items ?? new List<ProductDto>();
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
        var response = await PostAsync<ProductDto>(ApiEndpoints.Products, createProductDto);
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
        var response = await GetPagedAsync<ProductDto>(endpoint);
        return response.Data?.Items ?? new List<ProductDto>();
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        var response = await GetPagedAsync<ProductDto>(ApiEndpoints.LowStockProducts);
        return response.Data?.Items ?? new List<ProductDto>();
    }

    public async Task<List<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        var endpoint = $"{ApiEndpoints.SearchProducts}?term={Uri.EscapeDataString(searchTerm)}";
        var response = await GetPagedAsync<ProductDto>(endpoint);
        return response.Data?.Items ?? new List<ProductDto>();
    }
}
