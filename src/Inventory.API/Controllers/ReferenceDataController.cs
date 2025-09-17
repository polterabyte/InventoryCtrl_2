using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Serilog;
using System.Security.Claims;

namespace Inventory.API.Controllers;

/// <summary>
/// Base controller for reference data management
/// </summary>
/// <typeparam name="TDto">DTO type</typeparam>
/// <typeparam name="TCreateDto">Create DTO type</typeparam>
/// <typeparam name="TUpdateDto">Update DTO type</typeparam>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class ReferenceDataController<TDto, TCreateDto, TUpdateDto> : ControllerBase
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected readonly IReferenceDataService<TDto, TCreateDto, TUpdateDto> _service;
    protected readonly Microsoft.Extensions.Logging.ILogger _logger;

    protected ReferenceDataController(
        IReferenceDataService<TDto, TCreateDto, TUpdateDto> service,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all reference data items with pagination and filtering
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var result = await _service.GetAllAsync(page, pageSize, search, isActive);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {TypeName} data", typeof(TDto).Name);
            return StatusCode(500, new PagedApiResponse<TDto>
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve {typeof(TDto).Name.ToLower()} data"
            });
        }
    }

    /// <summary>
    /// Get all reference data items (for dropdowns, etc.)
    /// </summary>
    [HttpGet("all")]
    public virtual async Task<IActionResult> GetAllSimple()
    {
        try
        {
            var result = await _service.GetAllSimpleAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all {TypeName} data", typeof(TDto).Name);
            return StatusCode(500, new ApiResponse<List<TDto>>
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve {typeof(TDto).Name.ToLower()} data"
            });
        }
    }

    /// <summary>
    /// Get reference data item by ID
    /// </summary>
    [HttpGet("{id}")]
    public virtual async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {TypeName} with ID {Id}", typeof(TDto).Name, id);
            return StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve {typeof(TDto).Name.ToLower()}"
            });
        }
    }

    /// <summary>
    /// Create new reference data item
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public virtual async Task<IActionResult> Create([FromBody] TCreateDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<TDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var result = await _service.CreateAsync(createDto);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetById), new { id = GetIdFromDto(result.Data!) }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {TypeName}", typeof(TDto).Name);
            return StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                ErrorMessage = $"Failed to create {typeof(TDto).Name.ToLower()}"
            });
        }
    }

    /// <summary>
    /// Update reference data item
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public virtual async Task<IActionResult> Update(int id, [FromBody] TUpdateDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<TDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var result = await _service.UpdateAsync(id, updateDto);
            
            if (!result.Success)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {TypeName} with ID {Id}", typeof(TDto).Name, id);
            return StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                ErrorMessage = $"Failed to update {typeof(TDto).Name.ToLower()}"
            });
        }
    }

    /// <summary>
    /// Delete reference data item (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public virtual async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);
            
            if (!result.Success)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {TypeName} with ID {Id}", typeof(TDto).Name, id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = $"Failed to delete {typeof(TDto).Name.ToLower()}"
            });
        }
    }

    /// <summary>
    /// Check if item exists
    /// </summary>
    [HttpGet("exists")]
    public virtual async Task<IActionResult> Exists([FromQuery] string identifier)
    {
        try
        {
            var exists = await _service.ExistsAsync(identifier);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = exists
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking {TypeName} existence", typeof(TDto).Name);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = $"Failed to check {typeof(TDto).Name.ToLower()} existence"
            });
        }
    }

    /// <summary>
    /// Get items count
    /// </summary>
    [HttpGet("count")]
    public virtual async Task<IActionResult> GetCount([FromQuery] bool? isActive = null)
    {
        try
        {
            var count = await _service.GetCountAsync(isActive);
            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {TypeName} count", typeof(TDto).Name);
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                ErrorMessage = $"Failed to get {typeof(TDto).Name.ToLower()} count"
            });
        }
    }

    /// <summary>
    /// Extract ID from DTO for CreatedAtAction
    /// </summary>
    protected virtual int GetIdFromDto(TDto dto)
    {
        var idProperty = typeof(TDto).GetProperty("Id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(dto);
            if (value is int intValue)
            {
                return intValue;
            }
        }
        return 0;
    }
}
