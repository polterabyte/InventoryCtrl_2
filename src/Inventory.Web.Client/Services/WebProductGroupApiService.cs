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
        IApiErrorHandler errorHandler,
        IRequestValidator requestValidator,
        IAutoTokenRefreshService autoTokenRefreshService,
        ILogger<WebProductGroupApiService> logger)
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, autoTokenRefreshService, logger)
    {
    }

    public async Task<List<ProductGroupDto>> GetAllProductGroupsAsync()
    {
        Logger.LogInformation("GetAllProductGroupsAsync called, requesting from: {Endpoint}", ApiEndpoints.ProductGroupAll);
        var response = await GetAsync<List<ProductGroupDto>>(ApiEndpoints.ProductGroupAll);
        var productGroups = response?.Data ?? new List<ProductGroupDto>();
        Logger.LogInformation("GetAllProductGroupsAsync returned {Count} product groups", productGroups.Count);
        return productGroups;
    }

    public async Task<PagedApiResponse<ProductGroupDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, bool? isActive = null)
    {
        Logger.LogInformation("GetPagedAsync called, requesting from: {Endpoint} with page={Page}, pageSize={PageSize}, search={Search}, isActive={IsActive}", 
            ApiEndpoints.ProductGroups, page, pageSize, search, isActive);
        
        var queryParams = new List<string>();
        if (page > 1) queryParams.Add($"page={page}");
        if (pageSize != 10) queryParams.Add($"pageSize={pageSize}");
        if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (isActive.HasValue) queryParams.Add($"isActive={isActive.Value}");
        
        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var response = await GetPagedAsync<ProductGroupDto>($"{ApiEndpoints.ProductGroups}{queryString}");
        
        Logger.LogInformation("GetPagedAsync returned {Count} product groups", response?.Data?.Items?.Count ?? 0);
        return response ?? new PagedApiResponse<ProductGroupDto>();
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
