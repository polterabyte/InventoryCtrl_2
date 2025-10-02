using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

public class WebProductModelApiService : WebBaseApiService, IProductModelService
{
    public WebProductModelApiService(
        HttpClient httpClient, 
        IUrlBuilderService urlBuilderService, 
        IResilientApiService resilientApiService, 
        IApiErrorHandler errorHandler,
        IRequestValidator requestValidator,
        IAutoTokenRefreshService autoTokenRefreshService,
        ILogger<WebProductModelApiService> logger)
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, autoTokenRefreshService, logger)
    {
    }

    public async Task<List<ProductModelDto>> GetAllProductModelsAsync()
    {
        var response = await GetAsync<List<ProductModelDto>>(ApiEndpoints.ProductModels);
        return response.Data ?? new List<ProductModelDto>();
    }

    public async Task<List<ProductModelDto>> GetProductModelsByManufacturerAsync(int manufacturerId)
    {
        var response = await GetAsync<List<ProductModelDto>>($"{ApiEndpoints.ProductModels}/manufacturer/{manufacturerId}");
        return response.Data ?? new List<ProductModelDto>();
    }

    public async Task<ProductModelDto?> GetProductModelByIdAsync(int id)
    {
        var response = await GetAsync<ProductModelDto>($"{ApiEndpoints.ProductModels}/{id}");
        return response.Data;
    }

    public async Task<ProductModelDto> CreateProductModelAsync(CreateProductModelDto createProductModelDto)
    {
        var response = await PostAsync<ProductModelDto>(ApiEndpoints.ProductModels, createProductModelDto);
        return response.Data ?? throw new InvalidOperationException("Failed to create product model");
    }

    public async Task<ProductModelDto?> UpdateProductModelAsync(int id, UpdateProductModelDto updateProductModelDto)
    {
        var response = await PutAsync<ProductModelDto>($"{ApiEndpoints.ProductModels}/{id}", updateProductModelDto);
        return response.Data;
    }

    public async Task<bool> DeleteProductModelAsync(int id)
    {
        var response = await DeleteAsync($"{ApiEndpoints.ProductModels}/{id}");
        return response.Data;
    }
}
