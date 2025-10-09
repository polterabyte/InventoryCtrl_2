using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ManufacturerController(AppDbContext context, ILogger<ManufacturerController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ManufacturerDto>>>> GetManufacturers()
    {
        try
        {
            var manufacturers = await context.Manufacturers
                .Select(m => new ManufacturerDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    ContactInfo = m.ContactInfo,
                    Website = m.Website,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

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

            var existingManufacturer = await context.Manufacturers
                .FirstOrDefaultAsync(m => m.Name == request.Name);

            if (existingManufacturer != null)
            {
                return BadRequest(ApiResponse<ManufacturerDto>.ErrorResult("Manufacturer with this name already exists"));
            }

            var manufacturer = new Manufacturer
            {
                Name = request.Name,
                Description = request.Description,
                ContactInfo = request.ContactInfo,
                Website = request.Website,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            context.Manufacturers.Add(manufacturer);
            await context.SaveChangesAsync();

            logger.LogInformation("Manufacturer created: {ManufacturerName} with ID {ManufacturerId}", manufacturer.Name, manufacturer.Id);

            var manufacturerDto = new ManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                Description = manufacturer.Description,
                ContactInfo = manufacturer.ContactInfo,
                Website = manufacturer.Website,
                IsActive = manufacturer.IsActive,
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
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manufacturer == null)
            {
                return NotFound(ApiResponse<ManufacturerDto>.ErrorResult("Manufacturer not found"));
            }

            var existingManufacturer = await context.Manufacturers
                .FirstOrDefaultAsync(m => m.Name == request.Name && m.Id != id);

            if (existingManufacturer != null)
            {
                return BadRequest(ApiResponse<ManufacturerDto>.ErrorResult("Manufacturer with this name already exists"));
            }

            manufacturer.Name = request.Name;
            manufacturer.Description = request.Description;
            manufacturer.ContactInfo = request.ContactInfo;
            manufacturer.Website = request.Website;
            manufacturer.IsActive = request.IsActive;
            manufacturer.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Manufacturer updated: {ManufacturerName} with ID {ManufacturerId}", manufacturer.Name, manufacturer.Id);

            var manufacturerDto = new ManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                Description = manufacturer.Description,
                ContactInfo = manufacturer.ContactInfo,
                Website = manufacturer.Website,
                IsActive = manufacturer.IsActive,
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

