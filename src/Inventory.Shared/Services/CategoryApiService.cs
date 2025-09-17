using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Shared.Services;

public class CategoryApiService(HttpClient httpClient, ILogger<CategoryApiService> logger) 
    : BaseApiService(httpClient, ApiEndpoints.Categories, logger), ICategoryService
{

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        logger.LogInformation("GetAllCategoriesAsync called, requesting from: {BaseUrl}", BaseUrl);
        // Request all categories by setting a large page size
        var response = await GetPagedAsync<CategoryDto>($"{BaseUrl}?page=1&pageSize=1000");
        var categories = response.Data?.Items ?? new List<CategoryDto>();
        logger.LogInformation("GetAllCategoriesAsync returned {Count} categories", categories.Count);
        return categories;
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var endpoint = ApiEndpoints.CategoryById.Replace("{id}", id.ToString());
        var response = await GetAsync<CategoryDto>(endpoint);
        return response.Data;
    }

    public async Task<List<CategoryDto>> GetRootCategoriesAsync()
    {
        var response = await GetAsync<List<CategoryDto>>(ApiEndpoints.RootCategories);
        return response.Data ?? new List<CategoryDto>();
    }

    public async Task<List<CategoryDto>> GetSubCategoriesAsync(int parentId)
    {
        var endpoint = ApiEndpoints.SubCategories.Replace("{parentId}", parentId.ToString());
        var response = await GetAsync<List<CategoryDto>>(endpoint);
        return response.Data ?? new List<CategoryDto>();
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
    {
        var response = await PostAsync<CategoryDto>(BaseUrl, createCategoryDto);
        return response.Data ?? new CategoryDto();
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto)
    {
        var endpoint = ApiEndpoints.CategoryById.Replace("{id}", id.ToString());
        var response = await PutAsync<CategoryDto>(endpoint, updateCategoryDto);
        return response.Data;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var endpoint = ApiEndpoints.CategoryById.Replace("{id}", id.ToString());
        var response = await DeleteAsync(endpoint);
        return response.Success;
    }
}
