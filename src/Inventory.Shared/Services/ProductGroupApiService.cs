using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class ProductGroupApiService(HttpClient httpClient, ILogger<ProductGroupApiService> logger, IRetryService? retryService = null, INotificationService? notificationService = null) 
    : BaseApiService(httpClient, ApiEndpoints.ProductGroups, logger), IProductGroupService
{
    private readonly IRetryService? _retryService = retryService;
    private readonly INotificationService? _notificationService = notificationService;

    public async Task<List<ProductGroupDto>> GetAllProductGroupsAsync()
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await GetAsync<List<ProductGroupDto>>(ApiEndpoints.ProductGroups);
                    return response.Data ?? new List<ProductGroupDto>();
                },
                "GetAllProductGroups"
            );
        }
        
        var response = await GetAsync<List<ProductGroupDto>>(ApiEndpoints.ProductGroups);
        return response.Data ?? new List<ProductGroupDto>();
    }

    public async Task<ProductGroupDto?> GetProductGroupByIdAsync(int id)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await GetAsync<ProductGroupDto>($"{ApiEndpoints.ProductGroups}/{id}");
                    return response.Data;
                },
                "GetProductGroupById"
            );
        }
        
        var response = await GetAsync<ProductGroupDto>($"{ApiEndpoints.ProductGroups}/{id}");
        return response.Data;
    }

    public async Task<ProductGroupDto> CreateProductGroupAsync(CreateProductGroupDto createProductGroupDto)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await PostAsync<ProductGroupDto>(ApiEndpoints.ProductGroups, createProductGroupDto);
                    return response.Data ?? throw new InvalidOperationException("Failed to create product group");
                },
                "CreateProductGroup"
            );
        }
        
        var response = await PostAsync<ProductGroupDto>(ApiEndpoints.ProductGroups, createProductGroupDto);
        return response.Data ?? throw new InvalidOperationException("Failed to create product group");
    }

    public async Task<ProductGroupDto?> UpdateProductGroupAsync(int id, UpdateProductGroupDto updateProductGroupDto)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await PutAsync<ProductGroupDto>($"{ApiEndpoints.ProductGroups}/{id}", updateProductGroupDto);
                    return response.Data;
                },
                "UpdateProductGroup"
            );
        }
        
        var response = await PutAsync<ProductGroupDto>($"{ApiEndpoints.ProductGroups}/{id}", updateProductGroupDto);
        return response.Data;
    }

    public async Task<bool> DeleteProductGroupAsync(int id)
    {
        if (_retryService != null)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await DeleteAsync($"{ApiEndpoints.ProductGroups}/{id}");
                    return response.Data;
                },
                "DeleteProductGroup"
            );
        }
        
        var response = await DeleteAsync($"{ApiEndpoints.ProductGroups}/{id}");
        return response.Data;
    }
}
