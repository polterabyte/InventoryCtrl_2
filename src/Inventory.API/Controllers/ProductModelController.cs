using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Serilog;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/ProductModel")]
[Authorize]
public class ProductModelController(AppDbContext context, ILogger<ProductModelController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProductModels()
    {
        try
        {
            var productModels = await context.ProductModels
                .Include(pm => pm.Manufacturer)
                .Select(pm => new ProductModelDto
                {
                    Id = pm.Id,
                    Name = pm.Name,
                    ManufacturerId = pm.ManufacturerId,
                    ManufacturerName = pm.Manufacturer.Name,
                    IsActive = pm.IsActive,
                    CreatedAt = pm.CreatedAt,
                    UpdatedAt = pm.UpdatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<ProductModelDto>>
            {
                Success = true,
                Data = productModels
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product models");
            return StatusCode(500, new ApiResponse<List<ProductModelDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve product models"
            });
        }
    }

    [HttpGet("manufacturer/{manufacturerId}")]
    public async Task<IActionResult> GetProductModelsByManufacturer(int manufacturerId)
    {
        try
        {
            var productModels = await context.ProductModels
                .Where(pm => pm.ManufacturerId == manufacturerId && pm.IsActive)
                .Include(pm => pm.Manufacturer)
                .Select(pm => new ProductModelDto
                {
                    Id = pm.Id,
                    Name = pm.Name,
                    ManufacturerId = pm.ManufacturerId,
                    ManufacturerName = pm.Manufacturer.Name,
                    IsActive = pm.IsActive,
                    CreatedAt = pm.CreatedAt,
                    UpdatedAt = pm.UpdatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<ProductModelDto>>
            {
                Success = true,
                Data = productModels
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product models for manufacturer {ManufacturerId}", manufacturerId);
            return StatusCode(500, new ApiResponse<List<ProductModelDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve product models"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductModel(int id)
    {
        try
        {
            var productModel = await context.ProductModels
                .Include(pm => pm.Manufacturer)
                .FirstOrDefaultAsync(pm => pm.Id == id);

            if (productModel == null)
            {
                return NotFound(new ApiResponse<ProductModelDto>
                {
                    Success = false,
                    ErrorMessage = "Product model not found"
                });
            }

            var productModelDto = new ProductModelDto
            {
                Id = productModel.Id,
                Name = productModel.Name,
                ManufacturerId = productModel.ManufacturerId,
                ManufacturerName = productModel.Manufacturer.Name,
                IsActive = productModel.IsActive,
                CreatedAt = productModel.CreatedAt,
                UpdatedAt = productModel.UpdatedAt
            };

            return Ok(new ApiResponse<ProductModelDto>
            {
                Success = true,
                Data = productModelDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product model {ProductModelId}", id);
            return StatusCode(500, new ApiResponse<ProductModelDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve product model"
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProductModel([FromBody] CreateProductModelDto createProductModelDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value?.Errors.Select(e => e.ErrorMessage) ?? new string[0])
                    .ToList();

                return BadRequest(new ApiResponse<ProductModelDto>
                {
                    Success = false,
                    ErrorMessage = "Validation failed",
                    Errors = errors
                });
            }

            // Check if manufacturer exists
            var manufacturer = await context.Manufacturers.FindAsync(createProductModelDto.ManufacturerId);
            if (manufacturer == null)
            {
                return BadRequest(new ApiResponse<ProductModelDto>
                {
                    Success = false,
                    ErrorMessage = "Manufacturer not found"
                });
            }

            var productModel = new ProductModel
            {
                Name = createProductModelDto.Name,
                ManufacturerId = createProductModelDto.ManufacturerId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.ProductModels.Add(productModel);
            await context.SaveChangesAsync();

            var productModelDto = new ProductModelDto
            {
                Id = productModel.Id,
                Name = productModel.Name,
                ManufacturerId = productModel.ManufacturerId,
                ManufacturerName = manufacturer.Name,
                IsActive = productModel.IsActive,
                CreatedAt = productModel.CreatedAt,
                UpdatedAt = productModel.UpdatedAt
            };

            logger.LogInformation("Product model {ProductModelName} created with ID {ProductModelId}", productModel.Name, productModel.Id);

            return CreatedAtAction(nameof(GetProductModel), new { id = productModel.Id }, new ApiResponse<ProductModelDto>
            {
                Success = true,
                Data = productModelDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product model");
            return StatusCode(500, new ApiResponse<ProductModelDto>
            {
                Success = false,
                ErrorMessage = "Failed to create product model"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProductModel(int id, [FromBody] UpdateProductModelDto updateProductModelDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value?.Errors.Select(e => e.ErrorMessage) ?? new string[0])
                    .ToList();

                return BadRequest(new ApiResponse<ProductModelDto>
                {
                    Success = false,
                    ErrorMessage = "Validation failed",
                    Errors = errors
                });
            }

            var productModel = await context.ProductModels.FindAsync(id);
            if (productModel == null)
            {
                return NotFound(new ApiResponse<ProductModelDto>
                {
                    Success = false,
                    ErrorMessage = "Product model not found"
                });
            }

            // Check if manufacturer exists
            var manufacturer = await context.Manufacturers.FindAsync(updateProductModelDto.ManufacturerId);
            if (manufacturer == null)
            {
                return BadRequest(new ApiResponse<ProductModelDto>
                {
                    Success = false,
                    ErrorMessage = "Manufacturer not found"
                });
            }

            productModel.Name = updateProductModelDto.Name;
            productModel.ManufacturerId = updateProductModelDto.ManufacturerId;
            productModel.IsActive = updateProductModelDto.IsActive;
            productModel.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            var productModelDto = new ProductModelDto
            {
                Id = productModel.Id,
                Name = productModel.Name,
                ManufacturerId = productModel.ManufacturerId,
                ManufacturerName = manufacturer.Name,
                IsActive = productModel.IsActive,
                CreatedAt = productModel.CreatedAt,
                UpdatedAt = productModel.UpdatedAt
            };

            logger.LogInformation("Product model {ProductModelId} updated", id);

            return Ok(new ApiResponse<ProductModelDto>
            {
                Success = true,
                Data = productModelDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product model {ProductModelId}", id);
            return StatusCode(500, new ApiResponse<ProductModelDto>
            {
                Success = false,
                ErrorMessage = "Failed to update product model"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProductModel(int id)
    {
        try
        {
            var productModel = await context.ProductModels.FindAsync(id);
            if (productModel == null)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Product model not found"
                });
            }

            // Check if product model is used by any products
            var hasProducts = await context.Products.AnyAsync(p => p.ProductModelId == id);
            if (hasProducts)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Cannot delete product model that is used by products"
                });
            }

            context.ProductModels.Remove(productModel);
            await context.SaveChangesAsync();

            logger.LogInformation("Product model {ProductModelId} deleted", id);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product model {ProductModelId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to delete product model"
            });
        }
    }
}
