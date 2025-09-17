using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Serilog;
using System.Security.Claims;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoryController(AppDbContext context, ILogger<CategoryController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? parentId = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            // Log user information for debugging
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            
            logger.LogInformation("GetCategories called by user: {UserId}, Name: {UserName}, Roles: {Roles}", 
                userId, userName, string.Join(", ", userRoles));

            var query = context.Categories
                .Include(c => c.ParentCategory)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || 
                    (c.Description != null && c.Description.Contains(search)));
            }

            if (parentId.HasValue)
            {
                query = query.Where(c => c.ParentCategoryId == parentId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }
            else
            {
                // By default, show only active categories for non-admin users
                var isAdmin = userRoles.Contains("Admin") || userRoles.Contains("SuperUser");
                
                if (!isAdmin)
                {
                    query = query.Where(c => c.IsActive);
                }
            }

            // Get total count
            var totalCount = await query.CountAsync();
            
            logger.LogInformation("Found {TotalCount} categories after filtering", totalCount);

            // Apply pagination
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
                
            logger.LogInformation("Returning {CategoryCount} categories for page {Page}", categories.Count, page);

            var pagedResponse = new PagedResponse<CategoryDto>
            {
                Items = categories,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(new PagedApiResponse<CategoryDto>
            {
                Success = true,
                Data = pagedResponse
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, new PagedApiResponse<CategoryDto>
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
    [Authorize(Roles = "Admin")]
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
                IsActive = true, // Categories are created as active by default
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
    [Authorize(Roles = "Admin")]
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
            category.IsActive = request.IsActive; // Only Admin can modify IsActive
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
    [Authorize(Roles = "Admin")]
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
