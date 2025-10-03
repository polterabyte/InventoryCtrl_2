using Inventory.API.Interfaces;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inventory.API.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;
        private readonly Serilog.ILogger _logger;

        public CategoryService(AppDbContext context, Serilog.ILogger logger)
        {
            _context = context;
            _logger = logger.ForContext<CategoryService>();
        }

        public async Task<PagedApiResponse<CategoryDto>> GetCategoriesAsync(int page, int pageSize, string? search, int? parentId, bool? isActive, bool userIsAdmin)
        {
            try
            {
                var query = _context.Categories
                    .Include(c => c.ParentCategory)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => c.Name.Contains(search) || (c.Description != null && c.Description.Contains(search)));
                }

                if (parentId.HasValue)
                {
                    query = query.Where(c => c.ParentCategoryId == parentId.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(c => c.IsActive == isActive.Value);
                }
                else if (!userIsAdmin)
                {
                    query = query.Where(c => c.IsActive);
                }

                var totalCount = await query.CountAsync();
                _logger.Information("Found {TotalCount} categories after filtering", totalCount);

                var categories = await query
                    .OrderBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        IsActive = c.IsActive,
                        ParentCategoryId = c.ParentCategoryId,
                        ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                _logger.Information("Returning {CategoryCount} categories for page {Page}", categories.Count, page);

                var pagedResponse = new PagedResponse<CategoryDto>
                {
                    Items = categories,
                    total = totalCount,
                    page = page,
                    PageSize = pageSize
                };

                return PagedApiResponse<CategoryDto>.CreateSuccess(pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving categories");
                return PagedApiResponse<CategoryDto>.CreateFailure("Failed to retrieve categories");
            }
        }

        public async Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (category == null)
                {
                    return ApiResponse<CategoryDto>.ErrorResult("Category not found");
                }

                var categoryDto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    ParentCategoryId = category.ParentCategoryId,
                    ParentCategoryName = category.ParentCategory?.Name,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt
                };

                return ApiResponse<CategoryDto>.SuccessResult(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving category {CategoryId}", id);
                return ApiResponse<CategoryDto>.ErrorResult("Failed to retrieve category");
            }
        }

        public async Task<ApiResponse<List<CategoryDto>>> GetRootCategoriesAsync()
        {
            try
            {
                var rootCategories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        IsActive = c.IsActive,
                        ParentCategoryId = c.ParentCategoryId,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                return ApiResponse<List<CategoryDto>>.SuccessResult(rootCategories);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving root categories");
                return ApiResponse<List<CategoryDto>>.ErrorResult("Failed to retrieve root categories");
            }
        }

        public async Task<ApiResponse<List<CategoryDto>>> GetSubCategoriesAsync(int parentId)
        {
            try
            {
                var subCategories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == parentId)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        IsActive = c.IsActive,
                        ParentCategoryId = c.ParentCategoryId,
                        ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                return ApiResponse<List<CategoryDto>>.SuccessResult(subCategories);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving subcategories for parent {ParentId}", parentId);
                return ApiResponse<List<CategoryDto>>.ErrorResult("Failed to retrieve subcategories");
            }
        }

        public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryDto request)
        {
            try
            {
                if (request.ParentCategoryId.HasValue)
                {
                    var parentCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Id == request.ParentCategoryId.Value && c.IsActive);

                    if (parentCategory == null)
                    {
                        return ApiResponse<CategoryDto>.ErrorResult("Parent category not found");
                    }
                }

                var category = new Category
                {
                    Name = request.Name,
                    Description = request.Description,
                    ParentCategoryId = request.ParentCategoryId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _logger.Information("Category created: {CategoryName} with ID {CategoryId}", category.Name, category.Id);

                var categoryDto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    ParentCategoryId = category.ParentCategoryId,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt
                };

                return ApiResponse<CategoryDto>.SuccessResult(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating category");
                return ApiResponse<CategoryDto>.ErrorResult("Failed to create category");
            }
        }

        public async Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryDto request)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return ApiResponse<CategoryDto>.ErrorResult("Category not found");
                }

                if (request.ParentCategoryId.HasValue)
                {
                    var parentCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Id == request.ParentCategoryId.Value && c.IsActive);

                    if (parentCategory == null)
                    {
                        return ApiResponse<CategoryDto>.ErrorResult("Parent category not found");
                    }
                }

                category.Name = request.Name;
                category.Description = request.Description;
                category.IsActive = request.IsActive;
                category.ParentCategoryId = request.ParentCategoryId;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.Information("Category updated: {CategoryName} with ID {CategoryId}", category.Name, category.Id);

                var categoryDto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    ParentCategoryId = category.ParentCategoryId,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt
                };

                return ApiResponse<CategoryDto>.SuccessResult(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating category {CategoryId}", id);
                return ApiResponse<CategoryDto>.ErrorResult("Failed to update category");
            }
        }

        public async Task<ApiResponse<object>> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return ApiResponse<object>.ErrorResult("Category not found");
                }

                var hasSubCategories = await _context.Categories
                    .AnyAsync(c => c.ParentCategoryId == id && c.IsActive);

                if (hasSubCategories)
                {
                    return ApiResponse<object>.ErrorResult("Cannot delete category with subcategories");
                }

                var hasProducts = await _context.Products
                    .AnyAsync(p => p.CategoryId == id && p.IsActive);

                if (hasProducts)
                {
                    return ApiResponse<object>.ErrorResult("Cannot delete category with products");
                }

                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.Information("Category deleted (soft): {CategoryName} with ID {CategoryId}", category.Name, category.Id);

                return ApiResponse<object>.SuccessResult(new { message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting category {CategoryId}", id);
                return ApiResponse<object>.ErrorResult("Failed to delete category");
            }
        }
    }
}
