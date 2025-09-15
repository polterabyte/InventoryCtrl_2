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
    public async Task<IActionResult> GetProductGroups()
    {
        try
        {
            var productGroups = await context.ProductGroups
                .Select(pg => new ProductGroupDto
                {
                    Id = pg.Id,
                    Name = pg.Name,
                    IsActive = pg.IsActive,
                    CreatedAt = pg.CreatedAt,
                    UpdatedAt = pg.UpdatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<ProductGroupDto>>
            {
                Success = true,
                Data = productGroups
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product groups");
            return StatusCode(500, new ApiResponse<List<ProductGroupDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve product groups"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductGroup(int id)
    {
        try
        {
            var productGroup = await context.ProductGroups.FindAsync(id);

            if (productGroup == null)
            {
                return NotFound(new ApiResponse<ProductGroupDto>
                {
                    Success = false,
                    ErrorMessage = "Product group not found"
                });
            }

            var productGroupDto = new ProductGroupDto
            {
                Id = productGroup.Id,
                Name = productGroup.Name,
                IsActive = productGroup.IsActive,
                CreatedAt = productGroup.CreatedAt,
                UpdatedAt = productGroup.UpdatedAt
            };

            return Ok(new ApiResponse<ProductGroupDto>
            {
                Success = true,
                Data = productGroupDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product group {ProductGroupId}", id);
            return StatusCode(500, new ApiResponse<ProductGroupDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve product group"
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProductGroup([FromBody] CreateProductGroupDto createProductGroupDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ProductGroupDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model data",
                });
            }

            var productGroup = new ProductGroup
            {
                Name = createProductGroupDto.Name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.ProductGroups.Add(productGroup);
            await context.SaveChangesAsync();

            var productGroupDto = new ProductGroupDto
            {
                Id = productGroup.Id,
                Name = productGroup.Name,
                IsActive = productGroup.IsActive,
                CreatedAt = productGroup.CreatedAt,
                UpdatedAt = productGroup.UpdatedAt
            };

            logger.LogInformation("Product group {ProductGroupName} created with ID {ProductGroupId}", productGroup.Name, productGroup.Id);

            return CreatedAtAction(nameof(GetProductGroup), new { id = productGroup.Id }, new ApiResponse<ProductGroupDto>
            {
                Success = true,
                Data = productGroupDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product group");
            return StatusCode(500, new ApiResponse<ProductGroupDto>
            {
                Success = false,
                ErrorMessage = "Failed to create product group"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProductGroup(int id, [FromBody] UpdateProductGroupDto updateProductGroupDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ProductGroupDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model data",
                });
            }

            var productGroup = await context.ProductGroups.FindAsync(id);
            if (productGroup == null)
            {
                return NotFound(new ApiResponse<ProductGroupDto>
                {
                    Success = false,
                    ErrorMessage = "Product group not found"
                });
            }

            productGroup.Name = updateProductGroupDto.Name;
            productGroup.IsActive = updateProductGroupDto.IsActive;
            productGroup.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            var productGroupDto = new ProductGroupDto
            {
                Id = productGroup.Id,
                Name = productGroup.Name,
                IsActive = productGroup.IsActive,
                CreatedAt = productGroup.CreatedAt,
                UpdatedAt = productGroup.UpdatedAt
            };

            logger.LogInformation("Product group {ProductGroupId} updated", id);

            return Ok(new ApiResponse<ProductGroupDto>
            {
                Success = true,
                Data = productGroupDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product group {ProductGroupId}", id);
            return StatusCode(500, new ApiResponse<ProductGroupDto>
            {
                Success = false,
                ErrorMessage = "Failed to update product group"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProductGroup(int id)
    {
        try
        {
            var productGroup = await context.ProductGroups.FindAsync(id);
            if (productGroup == null)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Product group not found"
                });
            }

            // Check if product group is used by any products
            var hasProducts = await context.Products.AnyAsync(p => p.ProductGroupId == id);
            if (hasProducts)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Cannot delete product group that is used by products"
                });
            }

            context.ProductGroups.Remove(productGroup);
            await context.SaveChangesAsync();

            logger.LogInformation("Product group {ProductGroupId} deleted", id);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product group {ProductGroupId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to delete product group"
            });
        }
    }
}
