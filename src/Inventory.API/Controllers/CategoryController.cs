using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Serilog;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoryController(AppDbContext context, ILogger<CategoryController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await context.Categories
                .Where(c => c.IsActive)
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

            return Ok(new ApiResponse<List<CategoryDto>>
            {
                Success = true,
                Data = categories
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, new ApiResponse<List<CategoryDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve categories"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        try
        {
            var category = await context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (category == null)
            {
                return NotFound(new ApiResponse<CategoryDto>
                {
                    Success = false,
                    ErrorMessage = "Category not found"
                });
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

            return Ok(new ApiResponse<CategoryDto>
            {
                Success = true,
                Data = categoryDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving category {CategoryId}", id);
            return StatusCode(500, new ApiResponse<CategoryDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve category"
            });
        }
    }

    [HttpGet("root")]
    public async Task<IActionResult> GetRootCategories()
    {
        try
        {
            var rootCategories = await context.Categories
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

            return Ok(new ApiResponse<List<CategoryDto>>
            {
                Success = true,
                Data = rootCategories
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving root categories");
            return StatusCode(500, new ApiResponse<List<CategoryDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve root categories"
            });
        }
    }

    [HttpGet("{parentId}/sub")]
    public async Task<IActionResult> GetSubCategories(int parentId)
    {
        try
        {
            var subCategories = await context.Categories
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

            return Ok(new ApiResponse<List<CategoryDto>>
            {
                Success = true,
                Data = subCategories
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving subcategories for parent {ParentId}", parentId);
            return StatusCode(500, new ApiResponse<List<CategoryDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve subcategories"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<CategoryDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            // Check if parent category exists (if specified)
            if (request.ParentCategoryId.HasValue)
            {
                var parentCategory = await context.Categories
                    .FirstOrDefaultAsync(c => c.Id == request.ParentCategoryId.Value && c.IsActive);
                
                if (parentCategory == null)
                {
                    return BadRequest(new ApiResponse<CategoryDto>
                    {
                        Success = false,
                        ErrorMessage = "Parent category not found"
                    });
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

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            logger.LogInformation("Category created: {CategoryName} with ID {CategoryId}", category.Name, category.Id);

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

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new ApiResponse<CategoryDto>
            {
                Success = true,
                Data = categoryDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating category");
            return StatusCode(500, new ApiResponse<CategoryDto>
            {
                Success = false,
                ErrorMessage = "Failed to create category"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<CategoryDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var category = await context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new ApiResponse<CategoryDto>
                {
                    Success = false,
                    ErrorMessage = "Category not found"
                });
            }

            // Check if parent category exists (if specified)
            if (request.ParentCategoryId.HasValue)
            {
                var parentCategory = await context.Categories
                    .FirstOrDefaultAsync(c => c.Id == request.ParentCategoryId.Value && c.IsActive);
                
                if (parentCategory == null)
                {
                    return BadRequest(new ApiResponse<CategoryDto>
                    {
                        Success = false,
                        ErrorMessage = "Parent category not found"
                    });
                }
            }

            category.Name = request.Name;
            category.Description = request.Description;
            category.IsActive = request.IsActive;
            category.ParentCategoryId = request.ParentCategoryId;
            category.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Category updated: {CategoryName} with ID {CategoryId}", category.Name, category.Id);

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

            return Ok(new ApiResponse<CategoryDto>
            {
                Success = true,
                Data = categoryDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, new ApiResponse<CategoryDto>
            {
                Success = false,
                ErrorMessage = "Failed to update category"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var category = await context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Category not found"
                });
            }

            // Check if category has subcategories
            var hasSubCategories = await context.Categories
                .AnyAsync(c => c.ParentCategoryId == id && c.IsActive);

            if (hasSubCategories)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Cannot delete category with subcategories"
                });
            }

            // Check if category has products
            var hasProducts = await context.Products
                .AnyAsync(p => p.CategoryId == id && p.IsActive);

            if (hasProducts)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Cannot delete category with products"
                });
            }

            // Soft delete - set IsActive to false
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Category deleted (soft): {CategoryName} with ID {CategoryId}", category.Name, category.Id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "Category deleted successfully" }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to delete category"
            });
        }
    }
}
