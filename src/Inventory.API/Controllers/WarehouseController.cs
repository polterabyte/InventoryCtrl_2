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
public class WarehouseController(AppDbContext context, ILogger<WarehouseController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWarehouses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = context.Warehouses.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(w => w.Name.Contains(search) || w.Address.Contains(search));
            }

            if (isActive.HasValue)
            {
                query = query.Where(w => w.IsActive == isActive.Value);
            }
            else
            {
                // By default, show only active warehouses for non-admin users
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    query = query.Where(w => w.IsActive);
                }
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var warehouses = await query
                .OrderBy(w => w.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Description = w.Description,
                    Address = w.Address,
                    ContactInfo = w.ContactInfo,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<WarehouseDto>
            {
                Items = warehouses,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(new PagedApiResponse<WarehouseDto>
            {
                Success = true,
                Data = pagedResponse
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving warehouses");
            return StatusCode(500, new PagedApiResponse<WarehouseDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve warehouses"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetWarehouse(int id)
    {
        try
        {
            var warehouse = await context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == id && w.IsActive);

            if (warehouse == null)
            {
                return NotFound(new ApiResponse<WarehouseDto>
                {
                    Success = false,
                    ErrorMessage = "Warehouse not found"
                });
            }

            var warehouseDto = new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Description = warehouse.Description,
                Address = warehouse.Address,
                ContactInfo = warehouse.ContactInfo,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };

            return Ok(new ApiResponse<WarehouseDto>
            {
                Success = true,
                Data = warehouseDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving warehouse {WarehouseId}", id);
            return StatusCode(500, new ApiResponse<WarehouseDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve warehouse"
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<WarehouseDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            // Check if warehouse already exists
            var existingWarehouse = await context.Warehouses
                .FirstOrDefaultAsync(w => w.Name == request.Name);

            if (existingWarehouse != null)
            {
                return BadRequest(new ApiResponse<WarehouseDto>
                {
                    Success = false,
                    ErrorMessage = "Warehouse with this name already exists"
                });
            }

            var warehouse = new Warehouse
            {
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                ContactInfo = request.ContactInfo,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            logger.LogInformation("Warehouse created: {WarehouseName} with ID {WarehouseId}", warehouse.Name, warehouse.Id);

            var warehouseDto = new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Description = warehouse.Description,
                Address = warehouse.Address,
                ContactInfo = warehouse.ContactInfo,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };

            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, new ApiResponse<WarehouseDto>
            {
                Success = true,
                Data = warehouseDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating warehouse");
            return StatusCode(500, new ApiResponse<WarehouseDto>
            {
                Success = false,
                ErrorMessage = "Failed to create warehouse"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] UpdateWarehouseDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<WarehouseDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var warehouse = await context.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound(new ApiResponse<WarehouseDto>
                {
                    Success = false,
                    ErrorMessage = "Warehouse not found"
                });
            }

            // Check if another warehouse with the same name exists
            var existingWarehouse = await context.Warehouses
                .FirstOrDefaultAsync(w => w.Name == request.Name && w.Id != id);

            if (existingWarehouse != null)
            {
                return BadRequest(new ApiResponse<WarehouseDto>
                {
                    Success = false,
                    ErrorMessage = "Warehouse with this name already exists"
                });
            }

            warehouse.Name = request.Name;
            warehouse.Description = request.Description;
            warehouse.Address = request.Address;
            warehouse.ContactInfo = request.ContactInfo;
            warehouse.IsActive = request.IsActive;
            warehouse.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Warehouse updated: {WarehouseName} with ID {WarehouseId}", warehouse.Name, warehouse.Id);

            var warehouseDto = new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Description = warehouse.Description,
                Address = warehouse.Address,
                ContactInfo = warehouse.ContactInfo,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };

            return Ok(new ApiResponse<WarehouseDto>
            {
                Success = true,
                Data = warehouseDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating warehouse {WarehouseId}", id);
            return StatusCode(500, new ApiResponse<WarehouseDto>
            {
                Success = false,
                ErrorMessage = "Failed to update warehouse"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        try
        {
            var warehouse = await context.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Warehouse not found"
                });
            }

            // Check if warehouse has transactions
            var hasTransactions = await context.InventoryTransactions
                .AnyAsync(t => t.WarehouseId == id);

            if (hasTransactions)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Cannot delete warehouse with transactions"
                });
            }

            // Soft delete - set IsActive to false
            warehouse.IsActive = false;
            warehouse.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Warehouse deleted (soft): {WarehouseName} with ID {WarehouseId}", warehouse.Name, warehouse.Id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "Warehouse deleted successfully" }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting warehouse {WarehouseId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to delete warehouse"
            });
        }
    }
}
