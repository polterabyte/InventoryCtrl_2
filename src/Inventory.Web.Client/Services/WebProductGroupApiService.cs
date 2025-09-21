using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

public class WebProductGroupApiService : WebBaseApiService, IProductGroupService
{
    public WebProductGroupApiService(
        HttpClient httpClient, 
        IUrlBuilderService urlBuilderService, 
        IResilientApiService resilientApiService, 
        IApiErrorHandler errorHandler,        IRequestValidator requestValidator,
        ILogger<WebProductGroupApiService> logger)
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, logger)
    {
    }

    public async Task<List<ProductGroupDto>> GetAllProductGroupsAsync()
    {
        var response = await GetAsync<List<ProductGroupDto>>(ApiEndpoints.ProductGroups);
        return response?.Data ?? new List<ProductGroupDto>();
    }

    public async Task<ProductGroupDto?> GetProductGroupByIdAsync(int id)
    {
        var response = await GetAsync<ProductGroupDto>($"{ApiEndpoints.ProductGroups}/{id}");
        return response?.Data;
    }

    public async Task<ProductGroupDto> CreateProductGroupAsync(CreateProductGroupDto createProductGroupDto)
    {
        var response = await PostAsync<ProductGroupDto>(ApiEndpoints.ProductGroups, createProductGroupDto);
        if (response?.Data == null)
        {
            throw new InvalidOperationException("Failed to create product group");
        }
        return response.Data;
    }

    public async Task<ProductGroupDto?> UpdateProductGroupAsync(int id, UpdateProductGroupDto updateProductGroupDto)
    {
        var response = await PutAsync<ProductGroupDto>($"{ApiEndpoints.ProductGroups}/{id}", updateProductGroupDto);
        return response?.Data;
    }

    public async Task<bool> DeleteProductGroupAsync(int id)
    {
        var response = await DeleteAsync($"{ApiEndpoints.ProductGroups}/{id}");
        return response?.Data ?? false;
    }
}
