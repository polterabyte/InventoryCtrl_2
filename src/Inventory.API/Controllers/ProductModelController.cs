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
    public async Task<ActionResult<ApiResponse<List<ProductModelDto>>>> GetProductModels()
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

            return Ok(ApiResponse<List<ProductModelDto>>.CreateSuccess(productModels));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product models");
            return StatusCode(500, ApiResponse<List<ProductModelDto>>.CreateFailure("Failed to retrieve product models"));
        }
    }

    [HttpGet("manufacturer/{manufacturerId}")]
    public async Task<ActionResult<ApiResponse<List<ProductModelDto>>>> GetProductModelsByManufacturer(int manufacturerId)
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

            return Ok(ApiResponse<List<ProductModelDto>>.CreateSuccess(productModels));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product models for manufacturer {ManufacturerId}", manufacturerId);
            return StatusCode(500, ApiResponse<List<ProductModelDto>>.CreateFailure("Failed to retrieve product models"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductModelDto>>> GetProductModel(int id)
    {
        try
        {
            var productModel = await context.ProductModels
                .Include(pm => pm.Manufacturer)
                .FirstOrDefaultAsync(pm => pm.Id == id);

            if (productModel == null)
            {
                return NotFound(ApiResponse<ProductModelDto>.CreateFailure("Product model not found"));
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

            return Ok(ApiResponse<ProductModelDto>.CreateSuccess(productModelDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product model {ProductModelId}", id);
            return StatusCode(500, ApiResponse<ProductModelDto>.CreateFailure("Failed to retrieve product model"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductModelDto>>> CreateProductModel([FromBody] CreateProductModelDto createProductModelDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value?.Errors.Select(e => e.ErrorMessage) ?? new string[0])
                    .ToList();

                return BadRequest(ApiResponse<ProductModelDto>.CreateFailure("Validation failed", errors));
            }

            // Check if manufacturer exists
            var manufacturer = await context.Manufacturers.FindAsync(createProductModelDto.ManufacturerId);
            if (manufacturer == null)
            {
                return BadRequest(ApiResponse<ProductModelDto>.CreateFailure("Manufacturer not found"));
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

            return CreatedAtAction(nameof(GetProductModel), new { id = productModel.Id }, ApiResponse<ProductModelDto>.CreateSuccess(productModelDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product model");
            return StatusCode(500, ApiResponse<ProductModelDto>.CreateFailure("Failed to create product model"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductModelDto>>> UpdateProductModel(int id, [FromBody] UpdateProductModelDto updateProductModelDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value?.Errors.Select(e => e.ErrorMessage) ?? new string[0])
                    .ToList();

                return BadRequest(ApiResponse<ProductModelDto>.CreateFailure("Validation failed", errors));
            }

            var productModel = await context.ProductModels.FindAsync(id);
            if (productModel == null)
            {
                return NotFound(ApiResponse<ProductModelDto>.CreateFailure("Product model not found"));
            }

            // Check if manufacturer exists
            var manufacturer = await context.Manufacturers.FindAsync(updateProductModelDto.ManufacturerId);
            if (manufacturer == null)
            {
                return BadRequest(ApiResponse<ProductModelDto>.CreateFailure("Manufacturer not found"));
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

            return Ok(ApiResponse<ProductModelDto>.CreateSuccess(productModelDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product model {ProductModelId}", id);
            return StatusCode(500, ApiResponse<ProductModelDto>.CreateFailure("Failed to update product model"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProductModel(int id)
    {
        try
        {
            var productModel = await context.ProductModels.FindAsync(id);
            if (productModel == null)
            {
                return NotFound(ApiResponse<bool>.CreateFailure("Product model not found"));
            }

            // Check if product model is used by any products
            var hasProducts = await context.Products.AnyAsync(p => p.ProductModelId == id);
            if (hasProducts)
            {
                return BadRequest(ApiResponse<bool>.CreateFailure("Cannot delete product model that is used by products"));
            }

            context.ProductModels.Remove(productModel);
            await context.SaveChangesAsync();

            logger.LogInformation("Product model {ProductModelId} deleted", id);

            return Ok(ApiResponse<bool>.CreateSuccess(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product model {ProductModelId}", id);
            return StatusCode(500, ApiResponse<bool>.CreateFailure("Failed to delete product model"));
        }
    }
}
