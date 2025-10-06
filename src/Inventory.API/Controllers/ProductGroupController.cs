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
public class ProductGroupController(AppDbContext context, ILogger<ProductGroupController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<ProductGroupDto>>> GetProductGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = context.ProductGroups.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(pg => pg.Name.Contains(search));
            }

            if (isActive.HasValue)
            {
                query = query.Where(pg => pg.IsActive == isActive.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var productGroups = await query
                .OrderBy(pg => pg.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(pg => new ProductGroupDto
                {
                    Id = pg.Id,
                    Name = pg.Name,
                    IsActive = pg.IsActive,
                    ParentProductGroupId = pg.ParentProductGroupId,
                    ParentProductGroupName = pg.ParentProductGroup != null ? pg.ParentProductGroup.Name : null,
                    CreatedAt = pg.CreatedAt,
                    UpdatedAt = pg.UpdatedAt
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<ProductGroupDto>
            {
                Items = productGroups,
                total = totalCount,
                page = page,
                PageSize = pageSize
            };

            return Ok(PagedApiResponse<ProductGroupDto>.CreateSuccess(pagedResponse));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product groups");
            return StatusCode(500, PagedApiResponse<ProductGroupDto>.CreateFailure("Failed to retrieve product groups"));
        }
    }

    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<List<ProductGroupDto>>>> GetAllProductGroups()
    {
        try
        {
            var productGroups = await context.ProductGroups
                .OrderBy(pg => pg.Name)
                .Select(pg => new ProductGroupDto
                {
                    Id = pg.Id,
                    Name = pg.Name,
                    IsActive = pg.IsActive,
                    ParentProductGroupId = pg.ParentProductGroupId,
                    ParentProductGroupName = pg.ParentProductGroup != null ? pg.ParentProductGroup.Name : null,
                    CreatedAt = pg.CreatedAt,
                    UpdatedAt = pg.UpdatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ProductGroupDto>>.SuccessResult(productGroups));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all product groups");
            return StatusCode(500, ApiResponse<List<ProductGroupDto>>.ErrorResult("Failed to retrieve product groups"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductGroupDto>>> GetProductGroup(int id)
    {
        try
        {
            var productGroup = await context.ProductGroups
                .Where(pg => pg.Id == id)
                .Select(pg => new ProductGroupDto
                {
                    Id = pg.Id,
                    Name = pg.Name,
                    IsActive = pg.IsActive,
                    ParentProductGroupId = pg.ParentProductGroupId,
                    ParentProductGroupName = pg.ParentProductGroup != null ? pg.ParentProductGroup.Name : null,
                    CreatedAt = pg.CreatedAt,
                    UpdatedAt = pg.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (productGroup == null)
            {
                return NotFound(ApiResponse<ProductGroupDto>.ErrorResult("Product group not found"));
            }

            return Ok(ApiResponse<ProductGroupDto>.SuccessResult(productGroup));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product group {ProductGroupId}", id);
            return StatusCode(500, ApiResponse<ProductGroupDto>.ErrorResult("Failed to retrieve product group"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductGroupDto>>> CreateProductGroup([FromBody] CreateProductGroupDto createProductGroupDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<ProductGroupDto>.ErrorResult("Invalid model data"));
            }

            if (createProductGroupDto.ParentProductGroupId.HasValue)
            {
                var parentExists = await context.ProductGroups
                    .AnyAsync(pg => pg.Id == createProductGroupDto.ParentProductGroupId.Value);

                if (!parentExists)
                {
                    return BadRequest(ApiResponse<ProductGroupDto>.ErrorResult("Parent product group not found"));
                }
            }

            var productGroup = new ProductGroup
            {
                Name = createProductGroupDto.Name,
                IsActive = true,
                ParentProductGroupId = createProductGroupDto.ParentProductGroupId,
                CreatedAt = DateTime.UtcNow
            };

            context.ProductGroups.Add(productGroup);
            await context.SaveChangesAsync();

            var productGroupDto = await context.ProductGroups
                .Where(pg => pg.Id == productGroup.Id)
                .Select(pg => new ProductGroupDto
                {
                    Id = pg.Id,
                    Name = pg.Name,
                    IsActive = pg.IsActive,
                    ParentProductGroupId = pg.ParentProductGroupId,
                    ParentProductGroupName = pg.ParentProductGroup != null ? pg.ParentProductGroup.Name : null,
                    CreatedAt = pg.CreatedAt,
                    UpdatedAt = pg.UpdatedAt
                })
                .FirstAsync();

            logger.LogInformation("Product group {ProductGroupName} created with ID {ProductGroupId}", productGroup.Name, productGroup.Id);

            return CreatedAtAction(nameof(GetProductGroup), new { id = productGroup.Id }, ApiResponse<ProductGroupDto>.SuccessResult(productGroupDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product group");
            return StatusCode(500, ApiResponse<ProductGroupDto>.ErrorResult("Failed to create product group"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductGroupDto>>> UpdateProductGroup(int id, [FromBody] UpdateProductGroupDto updateProductGroupDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<ProductGroupDto>.ErrorResult("Invalid model data"));
            }

            var productGroup = await context.ProductGroups.FindAsync(id);
            if (productGroup == null)
            {
                return NotFound(ApiResponse<ProductGroupDto>.ErrorResult("Product group not found"));
            }

            if (updateProductGroupDto.ParentProductGroupId.HasValue)
            {
                if (updateProductGroupDto.ParentProductGroupId.Value == id)
                {
                    return BadRequest(ApiResponse<ProductGroupDto>.ErrorResult("Product group cannot be its own parent"));
                }

                var parentExists = await context.ProductGroups
                    .AnyAsync(pg => pg.Id == updateProductGroupDto.ParentProductGroupId.Value);

                if (!parentExists)
                {
                    return BadRequest(ApiResponse<ProductGroupDto>.ErrorResult("Parent product group not found"));
                }

                var parentIdToCheck = updateProductGroupDto.ParentProductGroupId.Value;
                while (true)
                {
                    if (parentIdToCheck == id)
                    {
                        return BadRequest(ApiResponse<ProductGroupDto>.ErrorResult("Parent group cannot be a descendant of the current group"));
                    }

                    var nextParentId = await context.ProductGroups
                        .Where(pg => pg.Id == parentIdToCheck)
                        .Select(pg => pg.ParentProductGroupId)
                        .FirstOrDefaultAsync();

                    if (!nextParentId.HasValue)
                    {
                        break;
                    }

                    parentIdToCheck = nextParentId.Value;
                }
            }

            productGroup.Name = updateProductGroupDto.Name;
            productGroup.IsActive = updateProductGroupDto.IsActive;
            productGroup.ParentProductGroupId = updateProductGroupDto.ParentProductGroupId;
            productGroup.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            var productGroupDto = await context.ProductGroups
                .Where(pg => pg.Id == productGroup.Id)
                .Select(pg => new ProductGroupDto
                {
                    Id = pg.Id,
                    Name = pg.Name,
                    IsActive = pg.IsActive,
                    ParentProductGroupId = pg.ParentProductGroupId,
                    ParentProductGroupName = pg.ParentProductGroup != null ? pg.ParentProductGroup.Name : null,
                    CreatedAt = pg.CreatedAt,
                    UpdatedAt = pg.UpdatedAt
                })
                .FirstAsync();

            logger.LogInformation("Product group {ProductGroupId} updated", id);

            return Ok(ApiResponse<ProductGroupDto>.SuccessResult(productGroupDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product group {ProductGroupId}", id);
            return StatusCode(500, ApiResponse<ProductGroupDto>.ErrorResult("Failed to update product group"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProductGroup(int id)
    {
        try
        {
            var productGroup = await context.ProductGroups.FindAsync(id);
            if (productGroup == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Product group not found"));
            }

            // Check if product group is used by any products
            var hasProducts = await context.Products.AnyAsync(p => p.ProductGroupId == id);
            if (hasProducts)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Cannot delete product group that is used by products"));
            }

            context.ProductGroups.Remove(productGroup);
            await context.SaveChangesAsync();

            logger.LogInformation("Product group {ProductGroupId} deleted", id);

            return Ok(ApiResponse<bool>.SuccessResult(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product group {ProductGroupId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("Failed to delete product group"));
        }
    }
}