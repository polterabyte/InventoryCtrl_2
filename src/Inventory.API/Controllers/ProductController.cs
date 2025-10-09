using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.API.Services;
using Inventory.API.Enums;
using Inventory.Shared.DTOs;
using Serilog;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
[EnableRateLimiting("ApiPolicy")]
public class ProductController(AppDbContext context, ILogger<ProductController> logger, AuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<ProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductModel)
                .Include(p => p.ProductGroup)
                .Include(p => p.UnitOfMeasure)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }
            else
            {
                // By default, show only active products for non-admin users
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    query = query.Where(p => p.IsActive);
                }
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Quantity = 0, // Will be populated from ProductOnHandView
                    UnitOfMeasureId = p.UnitOfMeasureId,
                    UnitOfMeasureName = p.UnitOfMeasure.Name,
                    UnitOfMeasureSymbol = p.UnitOfMeasure.Symbol,
                    IsActive = p.IsActive,
                     CategoryId = p.CategoryId,
                     CategoryName = p.Category.Name,
                     ProductModelId = p.ProductModelId,
                    ProductModelName = p.ProductModel.Name,
                    ProductGroupId = p.ProductGroupId,
                    ProductGroupName = p.ProductGroup.Name,
                    Note = p.Note,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();
                
            // Get quantities from ProductOnHandView and update the DTOs
            var productIds = products.Select(p => p.Id).ToList();
            var onHandQuantities = await context.ProductOnHand
                .Where(v => productIds.Contains(v.ProductId))
                .ToDictionaryAsync(v => v.ProductId, v => v.OnHandQty);
                
            foreach (var product in products)
            {
                product.Quantity = onHandQuantities.GetValueOrDefault(product.Id, 0);
            }

            var pagedResponse = new PagedResponse<ProductDto>
            {
                Items = products,
                total = totalCount,
                page = page,
                PageSize = pageSize
            };

            return Ok(PagedApiResponse<ProductDto>.CreateSuccess(pagedResponse));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, PagedApiResponse<ProductDto>.CreateFailure("Failed to retrieve products"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
    {
        try
        {
            var product = await context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductModel)
                .Include(p => p.ProductGroup)
                .Include(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound(ApiResponse<ProductDto>.ErrorResult("Product not found"));
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Quantity = 0, // Will be populated from ProductOnHandView
                UnitOfMeasureId = product.UnitOfMeasureId,
                UnitOfMeasureName = product.UnitOfMeasure.Name,
                UnitOfMeasureSymbol = product.UnitOfMeasure.Symbol,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                ProductModelId = product.ProductModelId,
                ProductModelName = product.ProductModel.Name,
                ProductGroupId = product.ProductGroupId,
                ProductGroupName = product.ProductGroup.Name,
                Note = product.Note,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
            
            // Get quantity from ProductOnHandView
            var onHandQuantity = await context.ProductOnHand
                .Where(v => v.ProductId == product.Id)
                .Select(v => v.OnHandQty)
                .FirstOrDefaultAsync();
            productDto.Quantity = onHandQuantity;

            return Ok(ApiResponse<ProductDto>.SuccessResult(productDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return StatusCode(500, ApiResponse<ProductDto>.ErrorResult("Failed to retrieve product"));
        }
    }



    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Invalid model state", errors));
            }

            // Check if user has permission to set IsActive
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && request.IsActive != true)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProductDto>.ErrorResult("Only administrators can create inactive products"));
            }

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                // New products start with 0 quantity - actual quantity managed through transactions
                UnitOfMeasureId = request.UnitOfMeasureId,
                IsActive = userRole == "Admin" ? request.IsActive : true, // Only Admin can set IsActive
                CategoryId = request.CategoryId,
                ProductModelId = request.ProductModelId,
                ProductGroupId = request.ProductGroupId,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Log audit entry for product creation with enhanced details
            var requestId = HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            await auditService.LogEntityChangeAsync(
                "Product",
                product.Id.ToString(),
                "CREATE",
                ActionType.Create,
                "Product",
                null,
                new {
                    Name = product.Name,
                    Description = product.Description,
                     CategoryId = product.CategoryId,
                     ProductModelId = product.ProductModelId,
                    ProductGroupId = product.ProductGroupId,
                    UnitOfMeasureId = product.UnitOfMeasureId,
                            Note = product.Note,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt
                },
                $"Product '{product.Name}' created",
                requestId
            );

            // Return the created product
            var createdProduct = await context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductModel)
                .Include(p => p.ProductGroup)
                .Include(p => p.UnitOfMeasure)
                .FirstAsync(p => p.Id == product.Id);

            var productDto = new ProductDto
            {
                Id = createdProduct.Id,
                Name = createdProduct.Name,
                Description = createdProduct.Description,
                Quantity = 0, // Will be populated from ProductOnHandView
                UnitOfMeasureId = createdProduct.UnitOfMeasureId,
                UnitOfMeasureName = createdProduct.UnitOfMeasure.Name,
                UnitOfMeasureSymbol = createdProduct.UnitOfMeasure.Symbol,
                IsActive = createdProduct.IsActive,
                CategoryId = createdProduct.CategoryId,
                CategoryName = createdProduct.Category.Name,
                ProductModelId = createdProduct.ProductModelId,
                ProductModelName = createdProduct.ProductModel.Name,
                ProductGroupId = createdProduct.ProductGroupId,
                ProductGroupName = createdProduct.ProductGroup.Name,
                Note = createdProduct.Note,
                CreatedAt = createdProduct.CreatedAt,
                UpdatedAt = createdProduct.UpdatedAt
            };
            
            // Get quantity from ProductOnHandView
            var onHandQuantity = await context.ProductOnHand
                .Where(v => v.ProductId == createdProduct.Id)
                .Select(v => v.OnHandQty)
                .FirstOrDefaultAsync();
            productDto.Quantity = onHandQuantity;

            logger.LogInformation("Product created: {ProductName}", product.Name);

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ApiResponse<ProductDto>.SuccessResult(productDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product");
            return StatusCode(500, ApiResponse<ProductDto>.ErrorResult("Failed to create product"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Invalid model state", errors));
            }

            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(ApiResponse<ProductDto>.ErrorResult("Product not found"));
            }

            // Check if user has permission to modify IsActive
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && product.IsActive != request.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProductDto>.ErrorResult("Only administrators can modify the IsActive status of products"));
            }

            // Store old values for audit
            var oldValues = new
            {
                Name = product.Name,
                Description = product.Description,
                UnitOfMeasureId = product.UnitOfMeasureId,
                CategoryId = product.CategoryId,
                ProductModelId = product.ProductModelId,
                ProductGroupId = product.ProductGroupId,
                Note = product.Note,
                IsActive = product.IsActive,
                UpdatedAt = product.UpdatedAt
            };

            product.Name = request.Name;
            product.Description = request.Description;
            product.UnitOfMeasureId = request.UnitOfMeasureId;
            product.CategoryId = request.CategoryId;
            product.ProductModelId = request.ProductModelId;
            product.ProductGroupId = request.ProductGroupId;
            product.Note = request.Note;
            
            // Only Admin can modify IsActive
            if (userRole == "Admin")
            {
                product.IsActive = request.IsActive;
            }
            
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Log audit entry for product update with enhanced details
            var requestId = HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            var newValues = new
            {
                Name = product.Name,
                Description = product.Description,
                UnitOfMeasureId = product.UnitOfMeasureId,
                CategoryId = product.CategoryId,
                ProductModelId = product.ProductModelId,
                ProductGroupId = product.ProductGroupId,
                Note = product.Note,
                IsActive = product.IsActive,
                UpdatedAt = product.UpdatedAt
            };

            await auditService.LogEntityChangeAsync(
                "Product",
                product.Id.ToString(),
                "UPDATE",
                ActionType.Update,
                "Product",
                oldValues,
                newValues,
                $"Product '{product.Name}' updated",
                requestId
            );

            logger.LogInformation("Product updated: {ProductName} with ID {ProductId}", product.Name, product.Id);

            var updatedDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Quantity = 0, // Will be populated from ProductOnHandView
                UnitOfMeasureId = product.UnitOfMeasureId,
                UnitOfMeasureName = product.UnitOfMeasure.Name,
                UnitOfMeasureSymbol = product.UnitOfMeasure.Symbol,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                ProductModelId = product.ProductModelId,
                ProductModelName = product.ProductModel.Name,
                ProductGroupId = product.ProductGroupId,
                ProductGroupName = product.ProductGroup.Name,
                Note = product.Note,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
            return Ok(ApiResponse<ProductDto>.SuccessResult(updatedDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, ApiResponse<ProductDto>.ErrorResult("Failed to update product"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
    {
        try
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Product not found"));
            }

            // Store old values for audit
            var oldValues = new
            {
                Name = product.Name,
                Description = product.Description,
                IsActive = product.IsActive,
                UpdatedAt = product.UpdatedAt
            };

            // Soft delete - set IsActive to false
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Log audit entry for product deletion with enhanced details
            var requestId = HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            var newValues = new
            {
                Name = product.Name,
                Description = product.Description,
                IsActive = product.IsActive,
                UpdatedAt = product.UpdatedAt
            };

            await auditService.LogEntityChangeAsync(
                "Product",
                product.Id.ToString(),
                "DELETE",
                ActionType.Delete,
                "Product",
                oldValues,
                newValues,
                $"Product '{product.Name}' deleted (soft delete)",
                requestId
            );

            logger.LogInformation("Product deleted (soft): {ProductName} with ID {ProductId}", product.Name, product.Id);

            return Ok(ApiResponse<object>.SuccessResult(new { message = "Product deleted successfully" }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to delete product"));
        }
    }

    [HttpPost("{id}/stock/adjust")]
    public async Task<ActionResult<ApiResponse<object>>> AdjustStock(int id, [FromBody] StockAdjustmentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid model state", errors));
            }

            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Product not found"));
            }

            // Get current quantity from ProductOnHandView for logging purposes
            var currentQuantity = await context.ProductOnHand
                .Where(v => v.ProductId == id)
                .Select(v => v.OnHandQty)
                .FirstOrDefaultAsync();
            
            // We no longer modify Product.Quantity directly - it's managed via transactions
            product.UpdatedAt = DateTime.UtcNow;

            // Create transaction record
            var transaction = new InventoryTransaction
            {
                ProductId = id,
                WarehouseId = request.WarehouseId,
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                Type = request.Quantity > 0 ? TransactionType.Income : TransactionType.Outcome,
                Quantity = Math.Abs(request.Quantity),
                Date = DateTime.UtcNow,
                Description = request.Description ?? $"Stock adjustment: {request.Quantity}"
            };

            context.InventoryTransactions.Add(transaction);
            await context.SaveChangesAsync();

            // Get updated quantity from ProductOnHandView
            var newQuantity = await context.ProductOnHand
                .Where(v => v.ProductId == id)
                .Select(v => v.OnHandQty)
                .FirstOrDefaultAsync();

            logger.LogInformation("Stock adjusted for product {ProductId}: {OldQuantity} -> {NewQuantity}", 
                id, currentQuantity, newQuantity);

            var responseData = new 
            { 
                message = "Stock adjusted successfully",
                oldQuantity = currentQuantity,
                newQuantity = newQuantity,
                adjustment = request.Quantity
            };
            return Ok(ApiResponse<object>.SuccessResult(responseData));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adjusting stock for product {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to adjust stock"));
        }
    }
}

public class StockAdjustmentRequest
{
    public int Quantity { get; set; }
    public int WarehouseId { get; set; }
    public string? Description { get; set; }
}
