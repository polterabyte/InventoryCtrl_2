using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class ProductModelApiService(HttpClient httpClient, ILogger<ProductModelApiService> logger, IRetryService? retryService = null, INotificationService? notificationService = null) 
    : BaseApiService(httpClient, ApiEndpoints.ProductModels, logger), IProductModelService
{
    private readonly IRetryService? _retryService = retryService;
    private readonly INotificationService? _notificationService = notificationService;

    public async Task<List<ProductModelDto>> GetAllProductModelsAsync()
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await GetAsync<List<ProductModelDto>>(ApiEndpoints.ProductModels);
                    return response.Data ?? new List<ProductModelDto>();
                },
                "GetAllProductModels"
            );
        }
        
        var response = await GetAsync<List<ProductModelDto>>(ApiEndpoints.ProductModels);
        return response.Data ?? new List<ProductModelDto>();
    }

    public async Task<ProductModelDto?> GetProductModelByIdAsync(int id)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await GetAsync<ProductModelDto>($"{ApiEndpoints.ProductModels}/{id}");
                    return response.Data;
                },
                "GetProductModelById"
            );
        }
        
        var response = await GetAsync<ProductModelDto>($"{ApiEndpoints.ProductModels}/{id}");
        return response.Data;
    }

    public async Task<ProductModelDto> CreateProductModelAsync(CreateProductModelDto createProductModelDto)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await PostAsync<ProductModelDto>(ApiEndpoints.ProductModels, createProductModelDto);
                    return response.Data ?? throw new InvalidOperationException("Failed to create product model");
                },
                "CreateProductModel"
            );
        }
        
        var response = await PostAsync<ProductModelDto>(ApiEndpoints.ProductModels, createProductModelDto);
        return response.Data ?? throw new InvalidOperationException("Failed to create product model");
    }

    public async Task<ProductModelDto?> UpdateProductModelAsync(int id, UpdateProductModelDto updateProductModelDto)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await PutAsync<ProductModelDto>($"{ApiEndpoints.ProductModels}/{id}", updateProductModelDto);
                    return response.Data;
                },
                "UpdateProductModel"
            );
        }
        
        var response = await PutAsync<ProductModelDto>($"{ApiEndpoints.ProductModels}/{id}", updateProductModelDto);
        return response.Data;
    }

    public async Task<bool> DeleteProductModelAsync(int id)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await DeleteAsync($"{ApiEndpoints.ProductModels}/{id}");
                    return response.Data;
                },
                "DeleteProductModel"
            );
        }
        
        var response = await DeleteAsync($"{ApiEndpoints.ProductModels}/{id}");
        return response.Data;
    }
}
