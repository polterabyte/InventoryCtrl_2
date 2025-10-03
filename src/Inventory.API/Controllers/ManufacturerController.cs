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
public class ManufacturerController(AppDbContext context, ILogger<ManufacturerController> logger) : ControllerBase
{
    /// <summary>
    /// Получает полный путь локации (например: "Склад А > Секция 1 > Полка 3")
    /// </summary>
    private async Task<string> GetLocationFullPathAsync(int locationId)
    {
        var location = await context.Locations
            .Include(l => l.ParentLocation)
            .FirstOrDefaultAsync(l => l.Id == locationId);
        
        if (location == null) return string.Empty;
        
        var pathParts = new List<string>();
        var current = location;
        
        while (current != null)
        {
            pathParts.Insert(0, current.Name);
            current = current.ParentLocation;
        }
        
        return string.Join(" > ", pathParts);
    }
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ManufacturerDto>>>> GetManufacturers()
    {
        try
        {
            var manufacturers = await context.Manufacturers
                .Include(m => m.Location)
                .Select(m => new ManufacturerDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    ContactInfo = m.ContactInfo,
                    Website = m.Website,
                    IsActive = m.IsActive,
                    LocationId = m.LocationId,
                    LocationName = m.Location.Name,
                    LocationFullPath = "", // Будет заполнено ниже
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            // Заполняем полные пути локаций
            foreach (var manufacturer in manufacturers)
            {
                manufacturer.LocationFullPath = await GetLocationFullPathAsync(manufacturer.LocationId);
            }

            return Ok(ApiResponse<List<ManufacturerDto>>.SuccessResult(manufacturers));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving manufacturers");
            return StatusCode(500, ApiResponse<List<ManufacturerDto>>.ErrorResult("Failed to retrieve manufacturers"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ManufacturerDto>>> GetManufacturer(int id)
    {
        try
        {
            var manufacturer = await context.Manufacturers
                .Include(m => m.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (manufacturer == null)
            {
                return NotFound(ApiResponse<ManufacturerDto>.ErrorResult("Manufacturer not found"));
            }

            var manufacturerDto = new ManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                Description = manufacturer.Description,
                ContactInfo = manufacturer.ContactInfo,
                Website = manufacturer.Website,
                IsActive = manufacturer.IsActive,
                LocationId = manufacturer.LocationId,
                LocationName = manufacturer.Location.Name,
                LocationFullPath = await GetLocationFullPathAsync(manufacturer.LocationId),
                CreatedAt = manufacturer.CreatedAt,
                UpdatedAt = manufacturer.UpdatedAt
            };

            return Ok(ApiResponse<ManufacturerDto>.SuccessResult(manufacturerDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving manufacturer {ManufacturerId}", id);
            return StatusCode(500, ApiResponse<ManufacturerDto>.ErrorResult("Failed to retrieve manufacturer"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ManufacturerDto>>> CreateManufacturer([FromBody] CreateManufacturerDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<ManufacturerDto>.ErrorResult("Invalid model state", errors));
            }

            // Check if manufacturer already exists
            var existingManufacturer = await context.Manufacturers
                .FirstOrDefaultAsync(m => m.Name == request.Name);

            if (existingManufacturer != null)
            {
                return BadRequest(ApiResponse<ManufacturerDto>.ErrorResult("Manufacturer with this name already exists"));
            }

            // Check if location exists
            var location = await context.Locations
                .FirstOrDefaultAsync(l => l.Id == request.LocationId && l.IsActive);

            if (location == null)
            {
                return BadRequest(ApiResponse<ManufacturerDto>.ErrorResult("Location not found or inactive"));
            }

            var manufacturer = new Manufacturer
            {
                Name = request.Name,
                Description = request.Description,
                ContactInfo = request.ContactInfo,
                Website = request.Website,
                LocationId = request.LocationId,
                CreatedAt = DateTime.UtcNow
            };

            context.Manufacturers.Add(manufacturer);
            await context.SaveChangesAsync();

            logger.LogInformation("Manufacturer created: {ManufacturerName} with ID {ManufacturerId} at Location {LocationId}", 
                manufacturer.Name, manufacturer.Id, manufacturer.LocationId);

            var manufacturerDto = new ManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                Description = manufacturer.Description,
                ContactInfo = manufacturer.ContactInfo,
                Website = manufacturer.Website,
                IsActive = manufacturer.IsActive,
                LocationId = manufacturer.LocationId,
                LocationName = location.Name,
                LocationFullPath = await GetLocationFullPathAsync(manufacturer.LocationId),
                CreatedAt = manufacturer.CreatedAt,
                UpdatedAt = manufacturer.UpdatedAt
            };

            return CreatedAtAction(nameof(GetManufacturer), new { id = manufacturer.Id }, ApiResponse<ManufacturerDto>.SuccessResult(manufacturerDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating manufacturer");
            return StatusCode(500, ApiResponse<ManufacturerDto>.ErrorResult("Failed to create manufacturer"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ManufacturerDto>>> UpdateManufacturer(int id, [FromBody] UpdateManufacturerDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<ManufacturerDto>.ErrorResult("Invalid model state", errors));
            }

            var manufacturer = await context.Manufacturers
                .Include(m => m.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (manufacturer == null)
            {
                return NotFound(ApiResponse<ManufacturerDto>.ErrorResult("Manufacturer not found"));
            }

            // Check if another manufacturer with the same name exists
            var existingManufacturer = await context.Manufacturers
                .FirstOrDefaultAsync(m => m.Name == request.Name && m.Id != id);

            if (existingManufacturer != null)
            {
                return BadRequest(ApiResponse<ManufacturerDto>.ErrorResult("Manufacturer with this name already exists"));
            }

            // Check if location exists
            var location = await context.Locations
                .FirstOrDefaultAsync(l => l.Id == request.LocationId && l.IsActive);

            if (location == null)
            {
                return BadRequest(ApiResponse<ManufacturerDto>.ErrorResult("Location not found or inactive"));
            }

            manufacturer.Name = request.Name;
            manufacturer.Description = request.Description;
            manufacturer.ContactInfo = request.ContactInfo;
            manufacturer.Website = request.Website;
            manufacturer.IsActive = request.IsActive;
            manufacturer.LocationId = request.LocationId;
            manufacturer.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Manufacturer updated: {ManufacturerName} with ID {ManufacturerId} at Location {LocationId}", 
                manufacturer.Name, manufacturer.Id, manufacturer.LocationId);

            var manufacturerDto = new ManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                Description = manufacturer.Description,
                ContactInfo = manufacturer.ContactInfo,
                Website = manufacturer.Website,
                IsActive = manufacturer.IsActive,
                LocationId = manufacturer.LocationId,
                LocationName = location.Name,
                LocationFullPath = await GetLocationFullPathAsync(manufacturer.LocationId),
                CreatedAt = manufacturer.CreatedAt,
                UpdatedAt = manufacturer.UpdatedAt
            };

            return Ok(ApiResponse<ManufacturerDto>.SuccessResult(manufacturerDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating manufacturer {ManufacturerId}", id);
            return StatusCode(500, ApiResponse<ManufacturerDto>.ErrorResult("Failed to update manufacturer"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteManufacturer(int id)
    {
        try
        {
            var manufacturer = await context.Manufacturers.FindAsync(id);
            if (manufacturer == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Manufacturer not found"));
            }

            // Check if manufacturer has products
            var hasProducts = await context.Products
                .AnyAsync(p => p.ManufacturerId == id && p.IsActive);

            if (hasProducts)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete manufacturer with products"));
            }

            context.Manufacturers.Remove(manufacturer);
            await context.SaveChangesAsync();

            logger.LogInformation("Manufacturer deleted: {ManufacturerName} with ID {ManufacturerId}", manufacturer.Name, manufacturer.Id);

            return Ok(ApiResponse<object>.SuccessResult(new { message = "Manufacturer deleted successfully" }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting manufacturer {ManufacturerId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to delete manufacturer"));
        }
    }
}
