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
public class ProductController(AppDbContext context, ILogger<ProductController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? manufacturerId = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Include(p => p.ProductModel)
                .Include(p => p.ProductGroup)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search) || 
                    (p.Description != null && p.Description.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (manufacturerId.HasValue)
            {
                query = query.Where(p => p.ManufacturerId == manufacturerId.Value);
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
                    SKU = p.SKU,
                    Description = p.Description,
                    Quantity = p.Quantity,
                    Unit = p.Unit,
                    IsActive = p.IsActive,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    ManufacturerId = p.ManufacturerId,
                    ManufacturerName = p.Manufacturer.Name,
                    ProductModelId = p.ProductModelId,
                    ProductModelName = p.ProductModel.Name,
                    ProductGroupId = p.ProductGroupId,
                    ProductGroupName = p.ProductGroup.Name,
                    MinStock = p.MinStock,
                    MaxStock = p.MaxStock,
                    Note = p.Note,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<ProductDto>
            {
                Items = products,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(new PagedApiResponse<ProductDto>
            {
                Success = true,
                Data = pagedResponse
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, new PagedApiResponse<ProductDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve products"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        try
        {
            var product = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Include(p => p.ProductModel)
                .Include(p => p.ProductGroup)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound(new ApiResponse<ProductDto>
                {
                    Success = false,
                    ErrorMessage = "Product not found"
                });
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Description = product.Description,
                Quantity = product.Quantity,
                Unit = product.Unit,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                ManufacturerId = product.ManufacturerId,
                ManufacturerName = product.Manufacturer.Name,
                ProductModelId = product.ProductModelId,
                ProductModelName = product.ProductModel.Name,
                ProductGroupId = product.ProductGroupId,
                ProductGroupName = product.ProductGroup.Name,
                MinStock = product.MinStock,
                MaxStock = product.MaxStock,
                Note = product.Note,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Ok(new ApiResponse<ProductDto>
            {
                Success = true,
                Data = productDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return StatusCode(500, new ApiResponse<ProductDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve product"
            });
        }
    }

    [HttpGet("sku/{sku}")]
    public async Task<IActionResult> GetProductBySku(string sku)
    {
        try
        {
            var product = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Include(p => p.ProductModel)
                .Include(p => p.ProductGroup)
                .FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive);

            if (product == null)
            {
                return NotFound(new ApiResponse<ProductDto>
                {
                    Success = false,
                    ErrorMessage = "Product not found"
                });
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Description = product.Description,
                Quantity = product.Quantity,
                Unit = product.Unit,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                ManufacturerId = product.ManufacturerId,
                ManufacturerName = product.Manufacturer.Name,
                ProductModelId = product.ProductModelId,
                ProductModelName = product.ProductModel.Name,
                ProductGroupId = product.ProductGroupId,
                ProductGroupName = product.ProductGroup.Name,
                MinStock = product.MinStock,
                MaxStock = product.MaxStock,
                Note = product.Note,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Ok(new ApiResponse<ProductDto>
            {
                Success = true,
                Data = productDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product by SKU {SKU}", sku);
            return StatusCode(500, new ApiResponse<ProductDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve product"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ProductDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            // Check if user has permission to set IsActive
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && request.IsActive != true)
            {
                return Forbid("Only administrators can create inactive products");
            }

            // Check if SKU already exists
            var existingProduct = await context.Products.FirstOrDefaultAsync(p => p.SKU == request.SKU);
            if (existingProduct != null)
            {
                return BadRequest(new ApiResponse<ProductDto>
                {
                    Success = false,
                    ErrorMessage = "Product with this SKU already exists"
                });
            }

            var product = new Product
            {
                Name = request.Name,
                SKU = request.SKU,
                Description = request.Description,
                Quantity = 0, // New products start with 0 quantity
                Unit = request.Unit,
                IsActive = userRole == "Admin" ? request.IsActive : true, // Only Admin can set IsActive
                CategoryId = request.CategoryId,
                ManufacturerId = request.ManufacturerId,
                ProductModelId = request.ProductModelId,
                ProductGroupId = request.ProductGroupId,
                MinStock = request.MinStock,
                MaxStock = request.MaxStock,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Return the created product
            var createdProduct = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Include(p => p.ProductModel)
                .Include(p => p.ProductGroup)
                .FirstAsync(p => p.Id == product.Id);

            var productDto = new ProductDto
            {
                Id = createdProduct.Id,
                Name = createdProduct.Name,
                SKU = createdProduct.SKU,
                Description = createdProduct.Description,
                Quantity = createdProduct.Quantity,
                Unit = createdProduct.Unit,
                IsActive = createdProduct.IsActive,
                CategoryId = createdProduct.CategoryId,
                CategoryName = createdProduct.Category.Name,
                ManufacturerId = createdProduct.ManufacturerId,
                ManufacturerName = createdProduct.Manufacturer.Name,
                ProductModelId = createdProduct.ProductModelId,
                ProductModelName = createdProduct.ProductModel.Name,
                ProductGroupId = createdProduct.ProductGroupId,
                ProductGroupName = createdProduct.ProductGroup.Name,
                MinStock = createdProduct.MinStock,
                MaxStock = createdProduct.MaxStock,
                Note = createdProduct.Note,
                CreatedAt = createdProduct.CreatedAt,
                UpdatedAt = createdProduct.UpdatedAt
            };

            logger.LogInformation("Product created: {ProductName} with SKU {SKU}", product.Name, product.SKU);

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new ApiResponse<ProductDto>
            {
                Success = true,
                Data = productDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product");
            return StatusCode(500, new ApiResponse<ProductDto>
            {
                Success = false,
                ErrorMessage = "Failed to create product"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ProductDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new ApiResponse<ProductDto>
                {
                    Success = false,
                    ErrorMessage = "Product not found"
                });
            }

            // Check if user has permission to modify IsActive
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && product.IsActive != request.IsActive)
            {
                return Forbid("Only administrators can modify the IsActive status of products");
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.Unit = request.Unit;
            product.CategoryId = request.CategoryId;
            product.ManufacturerId = request.ManufacturerId;
            product.ProductModelId = request.ProductModelId;
            product.ProductGroupId = request.ProductGroupId;
            product.MinStock = request.MinStock;
            product.MaxStock = request.MaxStock;
            product.Note = request.Note;
            
            // Only Admin can modify IsActive
            if (userRole == "Admin")
            {
                product.IsActive = request.IsActive;
            }
            
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Product updated: {ProductName} with ID {ProductId}", product.Name, product.Id);

            return Ok(new ApiResponse<ProductDto>
            {
                Success = true,
                Data = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    SKU = product.SKU,
                    Description = product.Description,
                    Quantity = product.Quantity,
                    Unit = product.Unit,
                    IsActive = product.IsActive,
                    CategoryId = product.CategoryId,
                    ManufacturerId = product.ManufacturerId,
                    ProductModelId = product.ProductModelId,
                    ProductGroupId = product.ProductGroupId,
                    MinStock = product.MinStock,
                    MaxStock = product.MaxStock,
                    Note = product.Note,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, new ApiResponse<ProductDto>
            {
                Success = false,
                ErrorMessage = "Failed to update product"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Product not found"
                });
            }

            // Soft delete - set IsActive to false
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Product deleted (soft): {ProductName} with ID {ProductId}", product.Name, product.Id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "Product deleted successfully" }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product {ProductId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to delete product"
            });
        }
    }

    [HttpPost("{id}/stock/adjust")]
    public async Task<IActionResult> AdjustStock(int id, [FromBody] StockAdjustmentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Product not found"
                });
            }

            var oldQuantity = product.Quantity;
            product.Quantity += request.Quantity;
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

            logger.LogInformation("Stock adjusted for product {ProductId}: {OldQuantity} -> {NewQuantity}", 
                id, oldQuantity, product.Quantity);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new 
                { 
                    message = "Stock adjusted successfully",
                    oldQuantity,
                    newQuantity = product.Quantity,
                    adjustment = request.Quantity
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adjusting stock for product {ProductId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to adjust stock"
            });
        }
    }
}

public class StockAdjustmentRequest
{
    public int Quantity { get; set; }
    public int WarehouseId { get; set; }
    public string? Description { get; set; }
}
