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
public class LocationController(AppDbContext context, ILogger<LocationController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<LocationDto>>> GetLocations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? parentId = null)
    {
        try
        {
            var query = context.Locations
                .Include(l => l.ParentLocation)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => l.Name.Contains(search) || 
                    (l.Description != null && l.Description.Contains(search)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(l => l.IsActive == isActive.Value);
            }
            else
            {
                // By default, show only active locations for non-admin users
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    query = query.Where(l => l.IsActive);
                }
            }

            if (parentId.HasValue)
            {
                query = query.Where(l => l.ParentLocationId == parentId.Value);
            }
            else if (parentId == 0) // Special case for root locations
            {
                query = query.Where(l => l.ParentLocationId == null);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var locations = await query
                .OrderBy(l => l.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Description = l.Description,
                    IsActive = l.IsActive,
                    ParentLocationId = l.ParentLocationId,
                    ParentLocationName = l.ParentLocation != null ? l.ParentLocation.Name : null,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<LocationDto>
            {
                Items = locations,
                total = totalCount,
                page = page,
                PageSize = pageSize
            };

            return Ok(PagedApiResponse<LocationDto>.CreateSuccess(pagedResponse));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting locations");
            return StatusCode(500, PagedApiResponse<LocationDto>.CreateFailure("An error occurred while retrieving locations"));
        }
    }

    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LocationDto>>>> GetAllLocations()
    {
        try
        {
            var locations = await context.Locations
                .Include(l => l.ParentLocation)
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Description = l.Description,
                    IsActive = l.IsActive,
                    ParentLocationId = l.ParentLocationId,
                    ParentLocationName = l.ParentLocation != null ? l.ParentLocation.Name : null,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<LocationDto>>.SuccessResult(locations));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all locations");
            return StatusCode(500, ApiResponse<IEnumerable<LocationDto>>.ErrorResult("An error occurred while retrieving locations"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> GetLocation(int id)
    {
        try
        {
            var location = await context.Locations
                .Include(l => l.ParentLocation)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                return NotFound(ApiResponse<LocationDto>.ErrorResult("Location not found"));
            }

            var locationDto = new LocationDto
            {
                Id = location.Id,
                Name = location.Name,
                Description = location.Description,
                IsActive = location.IsActive,
                ParentLocationId = location.ParentLocationId,
                ParentLocationName = location.ParentLocation?.Name,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
            };

            return Ok(ApiResponse<LocationDto>.SuccessResult(locationDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting location {Id}", id);
            return StatusCode(500, ApiResponse<LocationDto>.ErrorResult("An error occurred while retrieving the location"));
        }
    }

    [HttpGet("parent/{parentId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LocationDto>>>> GetLocationsByParentId(int parentId)
    {
        try
        {
            var locations = await context.Locations
                .Include(l => l.ParentLocation)
                .Where(l => l.ParentLocationId == parentId && l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Description = l.Description,
                    IsActive = l.IsActive,
                    ParentLocationId = l.ParentLocationId,
                    ParentLocationName = l.ParentLocation != null ? l.ParentLocation.Name : null,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<LocationDto>>.SuccessResult(locations));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting locations by parent {ParentId}", parentId);
            return StatusCode(500, ApiResponse<IEnumerable<LocationDto>>.ErrorResult("An error occurred while retrieving locations"));
        }
    }

    [HttpGet("root")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LocationDto>>>> GetRootLocations()
    {
        try
        {
            var locations = await context.Locations
                .Include(l => l.ParentLocation)
                .Where(l => l.ParentLocationId == null && l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Description = l.Description,
                    IsActive = l.IsActive,
                    ParentLocationId = l.ParentLocationId,
                    ParentLocationName = l.ParentLocation != null ? l.ParentLocation.Name : null,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<LocationDto>>.SuccessResult(locations));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting root locations");
            return StatusCode(500, ApiResponse<IEnumerable<LocationDto>>.ErrorResult("An error occurred while retrieving root locations"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> CreateLocation([FromBody] CreateLocationDto createLocationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<LocationDto>.ErrorResult("Invalid model state", errors));
            }

            // Check if parent location exists (if specified)
            if (createLocationDto.ParentLocationId.HasValue)
            {
                var parentExists = await context.Locations
                    .AnyAsync(l => l.Id == createLocationDto.ParentLocationId.Value && l.IsActive);
                
                if (!parentExists)
                {
                    return BadRequest(ApiResponse<LocationDto>.ErrorResult("Parent location not found or inactive"));
                }
            }

            // Check for duplicate name within the same parent
            var duplicateExists = await context.Locations
                .AnyAsync(l => l.Name == createLocationDto.Name && 
                    l.ParentLocationId == createLocationDto.ParentLocationId);

            if (duplicateExists)
            {
                return BadRequest(ApiResponse<LocationDto>.ErrorResult("A location with this name already exists in the same parent location"));
            }

            var location = new Location
            {
                Name = createLocationDto.Name,
                Description = createLocationDto.Description,
                ParentLocationId = createLocationDto.ParentLocationId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Locations.Add(location);
            await context.SaveChangesAsync();

            // Return the created location with parent information
            var createdLocation = await context.Locations
                .Include(l => l.ParentLocation)
                .FirstAsync(l => l.Id == location.Id);

            var locationDto = new LocationDto
            {
                Id = createdLocation.Id,
                Name = createdLocation.Name,
                Description = createdLocation.Description,
                IsActive = createdLocation.IsActive,
                ParentLocationId = createdLocation.ParentLocationId,
                ParentLocationName = createdLocation.ParentLocation?.Name,
                CreatedAt = createdLocation.CreatedAt,
                UpdatedAt = createdLocation.UpdatedAt
            };

            return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, ApiResponse<LocationDto>.SuccessResult(locationDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating location");
            return StatusCode(500, ApiResponse<LocationDto>.ErrorResult("An error occurred while creating the location"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> UpdateLocation(int id, [FromBody] UpdateLocationDto updateLocationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<LocationDto>.ErrorResult("Invalid model state", errors));
            }

            var location = await context.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound(ApiResponse<LocationDto>.ErrorResult("Location not found"));
            }

            // Check if parent location exists (if specified)
            if (updateLocationDto.ParentLocationId.HasValue)
            {
                var parentExists = await context.Locations
                    .AnyAsync(l => l.Id == updateLocationDto.ParentLocationId.Value && l.IsActive);
                
                if (!parentExists)
                {
                    return BadRequest(ApiResponse<LocationDto>.ErrorResult("Parent location not found or inactive"));
                }

                // Prevent circular reference
                if (updateLocationDto.ParentLocationId.Value == id)
                {
                    return BadRequest(ApiResponse<LocationDto>.ErrorResult("A location cannot be its own parent"));
                }
            }

            // Check for duplicate name within the same parent (excluding current location)
            var duplicateExists = await context.Locations
                .AnyAsync(l => l.Name == updateLocationDto.Name && 
                    l.ParentLocationId == updateLocationDto.ParentLocationId &&
                    l.Id != id);

            if (duplicateExists)
            {
                return BadRequest(ApiResponse<LocationDto>.ErrorResult("A location with this name already exists in the same parent location"));
            }

            location.Name = updateLocationDto.Name;
            location.Description = updateLocationDto.Description;
            location.IsActive = updateLocationDto.IsActive;
            location.ParentLocationId = updateLocationDto.ParentLocationId;
            location.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Return the updated location with parent information
            var updatedLocation = await context.Locations
                .Include(l => l.ParentLocation)
                .FirstAsync(l => l.Id == location.Id);

            var locationDto = new LocationDto
            {
                Id = updatedLocation.Id,
                Name = updatedLocation.Name,
                Description = updatedLocation.Description,
                IsActive = updatedLocation.IsActive,
                ParentLocationId = updatedLocation.ParentLocationId,
                ParentLocationName = updatedLocation.ParentLocation?.Name,
                CreatedAt = updatedLocation.CreatedAt,
                UpdatedAt = updatedLocation.UpdatedAt
            };

            return Ok(ApiResponse<LocationDto>.SuccessResult(locationDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating location {Id}", id);
            return StatusCode(500, ApiResponse<LocationDto>.ErrorResult("An error occurred while updating the location"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteLocation(int id)
    {
        try
        {
            var location = await context.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Location not found"));
            }

            // Check if location has sub-locations
            var hasSubLocations = await context.Locations
                .AnyAsync(l => l.ParentLocationId == id);

            if (hasSubLocations)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete location that has sub-locations"));
            }

            // Check if location is used in inventory transactions
            var hasTransactions = await context.InventoryTransactions
                .AnyAsync(t => t.LocationId == id);

            if (hasTransactions)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete location that is used in inventory transactions"));
            }

            context.Locations.Remove(location);
            await context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new object()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting location {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while deleting the location"));
        }
    }
}
