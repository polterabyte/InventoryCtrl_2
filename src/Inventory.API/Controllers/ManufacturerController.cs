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
    [HttpGet]
    public async Task<IActionResult> GetManufacturers()
    {
        try
        {
            var manufacturers = await context.Manufacturers
                .Select(m => new ManufacturerDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<ManufacturerDto>>
            {
                Success = true,
                Data = manufacturers
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving manufacturers");
            return StatusCode(500, new ApiResponse<List<ManufacturerDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve manufacturers"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetManufacturer(int id)
    {
        try
        {
            var manufacturer = await context.Manufacturers.FindAsync(id);
            if (manufacturer == null)
            {
                return NotFound(new ApiResponse<ManufacturerDto>
                {
                    Success = false,
                    ErrorMessage = "Manufacturer not found"
                });
            }

            var manufacturerDto = new ManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                CreatedAt = manufacturer.CreatedAt,
                UpdatedAt = manufacturer.UpdatedAt
            };

            return Ok(new ApiResponse<ManufacturerDto>
            {
                Success = true,
                Data = manufacturerDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving manufacturer {ManufacturerId}", id);
            return StatusCode(500, new ApiResponse<ManufacturerDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve manufacturer"
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateManufacturer([FromBody] CreateManufacturerDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ManufacturerDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            // Check if manufacturer already exists
            var existingManufacturer = await context.Manufacturers
                .FirstOrDefaultAsync(m => m.Name == request.Name);

            if (existingManufacturer != null)
            {
                return BadRequest(new ApiResponse<ManufacturerDto>
                {
                    Success = false,
                    ErrorMessage = "Manufacturer with this name already exists"
                });
            }

            var manufacturer = new Manufacturer
            {
                Name = request.Name,
                CreatedAt = DateTime.UtcNow
            };

            context.Manufacturers.Add(manufacturer);
            await context.SaveChangesAsync();

            logger.LogInformation("Manufacturer created: {ManufacturerName} with ID {ManufacturerId}", manufacturer.Name, manufacturer.Id);

            var manufacturerDto = new ManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                CreatedAt = manufacturer.CreatedAt,
                UpdatedAt = manufacturer.UpdatedAt
            };

            return CreatedAtAction(nameof(GetManufacturer), new { id = manufacturer.Id }, new ApiResponse<ManufacturerDto>
            {
                Success = true,
                Data = manufacturerDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating manufacturer");
            return StatusCode(500, new ApiResponse<ManufacturerDto>
            {
                Success = false,
                ErrorMessage = "Failed to create manufacturer"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateManufacturer(int id, [FromBody] UpdateManufacturerDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ManufacturerDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var manufacturer = await context.Manufacturers.FindAsync(id);
            if (manufacturer == null)
            {
                return NotFound(new ApiResponse<ManufacturerDto>
                {
                    Success = false,
                    ErrorMessage = "Manufacturer not found"
                });
            }

            // Check if another manufacturer with the same name exists
            var existingManufacturer = await context.Manufacturers
                .FirstOrDefaultAsync(m => m.Name == request.Name && m.Id != id);

            if (existingManufacturer != null)
            {
                return BadRequest(new ApiResponse<ManufacturerDto>
                {
                    Success = false,
                    ErrorMessage = "Manufacturer with this name already exists"
                });
            }

            manufacturer.Name = request.Name;
            manufacturer.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Manufacturer updated: {ManufacturerName} with ID {ManufacturerId}", manufacturer.Name, manufacturer.Id);

            var manufacturerDto = new ManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                CreatedAt = manufacturer.CreatedAt,
                UpdatedAt = manufacturer.UpdatedAt
            };

            return Ok(new ApiResponse<ManufacturerDto>
            {
                Success = true,
                Data = manufacturerDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating manufacturer {ManufacturerId}", id);
            return StatusCode(500, new ApiResponse<ManufacturerDto>
            {
                Success = false,
                ErrorMessage = "Failed to update manufacturer"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteManufacturer(int id)
    {
        try
        {
            var manufacturer = await context.Manufacturers.FindAsync(id);
            if (manufacturer == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Manufacturer not found"
                });
            }

            // Check if manufacturer has products
            var hasProducts = await context.Products
                .AnyAsync(p => p.ManufacturerId == id && p.IsActive);

            if (hasProducts)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Cannot delete manufacturer with products"
                });
            }

            context.Manufacturers.Remove(manufacturer);
            await context.SaveChangesAsync();

            logger.LogInformation("Manufacturer deleted: {ManufacturerName} with ID {ManufacturerId}", manufacturer.Name, manufacturer.Id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "Manufacturer deleted successfully" }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting manufacturer {ManufacturerId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to delete manufacturer"
            });
        }
    }
}
