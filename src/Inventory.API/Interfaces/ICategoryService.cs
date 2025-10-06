using Inventory.Shared.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inventory.API.Interfaces
{
    public interface ICategoryService
    {
        Task<PagedApiResponse<CategoryDto>> GetCategoriesAsync(int page, int pageSize, string? search, int? parentId, bool? isActive, bool userIsAdmin);
        Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(int id);
        Task<ApiResponse<List<CategoryDto>>> GetRootCategoriesAsync();
        Task<ApiResponse<List<CategoryDto>>> GetSubCategoriesAsync(int parentId);
        Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryDto request);
        Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryDto request);
        Task<ApiResponse<object>> DeleteCategoryAsync(int id);
    }
}
