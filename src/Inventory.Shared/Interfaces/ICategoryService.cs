using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<List<CategoryDto>> GetRootCategoriesAsync();
    Task<List<CategoryDto>> GetSubCategoriesAsync(int parentId);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto);
    Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto);
    Task<bool> DeleteCategoryAsync(int id);
}
