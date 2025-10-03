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
public class UnitOfMeasureController(AppDbContext context, ILogger<UnitOfMeasureController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<UnitOfMeasureDto>>> GetUnitOfMeasures(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = context.UnitOfMeasures.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Name.Contains(search) || 
                    u.Symbol.Contains(search) ||
                    (u.Description != null && u.Description.Contains(search)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }
            else
            {
                // By default, show only active units for non-admin users
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    query = query.Where(u => u.IsActive);
                }
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var unitOfMeasures = await query
                .OrderBy(u => u.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UnitOfMeasureDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Symbol = u.Symbol,
                    Description = u.Description,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<UnitOfMeasureDto>
            {
                Items = unitOfMeasures,
                total = totalCount,
                page = page,
                PageSize = pageSize
            };

            return Ok(PagedApiResponse<UnitOfMeasureDto>.CreateSuccess(pagedResponse));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving unit of measures");
            return StatusCode(500, PagedApiResponse<UnitOfMeasureDto>.CreateFailure("Failed to retrieve unit of measures"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UnitOfMeasureDto>>> GetUnitOfMeasure(int id)
    {
        try
        {
            var unitOfMeasure = await context.UnitOfMeasures
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (unitOfMeasure == null)
            {
                return NotFound(ApiResponse<UnitOfMeasureDto>.ErrorResult("Unit of measure not found"));
            }

            var unitOfMeasureDto = new UnitOfMeasureDto
            {
                Id = unitOfMeasure.Id,
                Name = unitOfMeasure.Name,
                Symbol = unitOfMeasure.Symbol,
                Description = unitOfMeasure.Description,
                IsActive = unitOfMeasure.IsActive,
                CreatedAt = unitOfMeasure.CreatedAt,
                UpdatedAt = unitOfMeasure.UpdatedAt
            };

            return Ok(ApiResponse<UnitOfMeasureDto>.SuccessResult(unitOfMeasureDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving unit of measure {UnitOfMeasureId}", id);
            return StatusCode(500, ApiResponse<UnitOfMeasureDto>.ErrorResult("Failed to retrieve unit of measure"));
        }
    }

    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<List<UnitOfMeasureDto>>>> GetAllUnitOfMeasures()
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var query = context.UnitOfMeasures.AsQueryable();

            // Show only active units for non-admin users
            if (userRole != "Admin")
            {
                query = query.Where(u => u.IsActive);
            }

            var unitOfMeasures = await query
                .OrderBy(u => u.Name)
                .Select(u => new UnitOfMeasureDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Symbol = u.Symbol,
                    Description = u.Description,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<UnitOfMeasureDto>>.SuccessResult(unitOfMeasures));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all unit of measures");
            return StatusCode(500, ApiResponse<List<UnitOfMeasureDto>>.ErrorResult("Failed to retrieve unit of measures"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UnitOfMeasureDto>>> CreateUnitOfMeasure([FromBody] CreateUnitOfMeasureDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<UnitOfMeasureDto>.ErrorResult("Invalid model state", errors));
            }

            // Check if unit of measure with same symbol already exists
            var existingUnit = await context.UnitOfMeasures
                .FirstOrDefaultAsync(u => u.Symbol == request.Symbol);

            if (existingUnit != null)
            {
                return BadRequest(ApiResponse<UnitOfMeasureDto>.ErrorResult("Unit of measure with this symbol already exists"));
            }

            var unitOfMeasure = new UnitOfMeasure
            {
                Name = request.Name,
                Symbol = request.Symbol,
                Description = request.Description,
                IsActive = true, // Units are created as active by default
                CreatedAt = DateTime.UtcNow
            };

            context.UnitOfMeasures.Add(unitOfMeasure);
            await context.SaveChangesAsync();

            logger.LogInformation("Unit of measure created: {UnitName} with ID {UnitId}", unitOfMeasure.Name, unitOfMeasure.Id);

            var unitOfMeasureDto = new UnitOfMeasureDto
            {
                Id = unitOfMeasure.Id,
                Name = unitOfMeasure.Name,
                Symbol = unitOfMeasure.Symbol,
                Description = unitOfMeasure.Description,
                IsActive = unitOfMeasure.IsActive,
                CreatedAt = unitOfMeasure.CreatedAt,
                UpdatedAt = unitOfMeasure.UpdatedAt
            };

            return CreatedAtAction(nameof(GetUnitOfMeasure), new { id = unitOfMeasure.Id }, ApiResponse<UnitOfMeasureDto>.SuccessResult(unitOfMeasureDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating unit of measure");
            return StatusCode(500, ApiResponse<UnitOfMeasureDto>.ErrorResult("Failed to create unit of measure"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UnitOfMeasureDto>>> UpdateUnitOfMeasure(int id, [FromBody] UpdateUnitOfMeasureDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<UnitOfMeasureDto>.ErrorResult("Invalid model state", errors));
            }

            var unitOfMeasure = await context.UnitOfMeasures.FindAsync(id);
            if (unitOfMeasure == null)
            {
                return NotFound(ApiResponse<UnitOfMeasureDto>.ErrorResult("Unit of measure not found"));
            }

            // Check if another unit of measure with the same symbol exists
            var existingUnit = await context.UnitOfMeasures
                .FirstOrDefaultAsync(u => u.Symbol == request.Symbol && u.Id != id);

            if (existingUnit != null)
            {
                return BadRequest(ApiResponse<UnitOfMeasureDto>.ErrorResult("Unit of measure with this symbol already exists"));
            }

            unitOfMeasure.Name = request.Name;
            unitOfMeasure.Symbol = request.Symbol;
            unitOfMeasure.Description = request.Description;
            unitOfMeasure.IsActive = request.IsActive; // Only Admin can modify IsActive
            unitOfMeasure.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Unit of measure updated: {UnitName} with ID {UnitId}", unitOfMeasure.Name, unitOfMeasure.Id);

            var unitOfMeasureDto = new UnitOfMeasureDto
            {
                Id = unitOfMeasure.Id,
                Name = unitOfMeasure.Name,
                Symbol = unitOfMeasure.Symbol,
                Description = unitOfMeasure.Description,
                IsActive = unitOfMeasure.IsActive,
                CreatedAt = unitOfMeasure.CreatedAt,
                UpdatedAt = unitOfMeasure.UpdatedAt
            };

            return Ok(ApiResponse<UnitOfMeasureDto>.SuccessResult(unitOfMeasureDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating unit of measure {UnitOfMeasureId}", id);
            return StatusCode(500, ApiResponse<UnitOfMeasureDto>.ErrorResult("Failed to update unit of measure"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUnitOfMeasure(int id)
    {
        try
        {
            var unitOfMeasure = await context.UnitOfMeasures.FindAsync(id);
            if (unitOfMeasure == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Unit of measure not found"));
            }

            // Check if unit of measure has products
            var hasProducts = await context.Products
                .AnyAsync(p => p.UnitOfMeasureId == id && p.IsActive);

            if (hasProducts)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete unit of measure with products"));
            }

            // Soft delete - set IsActive to false
            unitOfMeasure.IsActive = false;
            unitOfMeasure.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Unit of measure deleted (soft): {UnitName} with ID {UnitId}", unitOfMeasure.Name, unitOfMeasure.Id);

            return Ok(ApiResponse<object>.SuccessResult(new { message = "Unit of measure deleted successfully" }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting unit of measure {UnitOfMeasureId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to delete unit of measure"));
        }
    }
}
