using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Serilog;
using System.Security.Claims;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehouseController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUserWarehouseService _userWarehouseService;
    private readonly ILogger<WarehouseController> _logger;

    public WarehouseController(
        AppDbContext context,
        IUserWarehouseService userWarehouseService,
        ILogger<WarehouseController> logger)
    {
        _context = context;
        _userWarehouseService = userWarehouseService;
        _logger = logger;
    }
    [HttpGet]
    public async Task<IActionResult> GetWarehouses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            // Get current user information
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(PagedApiResponse<WarehouseDto>.CreateFailure("User not authenticated"));
            }

            // Get accessible warehouse IDs for the current user
            var accessibleWarehouseIds = await _userWarehouseService.GetAccessibleWarehouseIdsAsync(currentUserId, currentUserRole ?? "User");

            var query = _context.Warehouses
                .Include(w => w.Location)
                .Where(w => accessibleWarehouseIds.Contains(w.Id)) // Apply warehouse access control
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(w => w.Name.Contains(search) || (w.Location != null && w.Location.Name.Contains(search)));
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
                    LocationId = w.LocationId,
                    LocationName = w.Location != null ? w.Location.Name : null,
                    ContactInfo = w.ContactInfo,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<WarehouseDto>
            {
                Items = warehouses,
                total = totalCount,
                page = page,
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
            _logger.LogError(ex, "Error retrieving warehouses");
            return StatusCode(500, PagedApiResponse<WarehouseDto>.CreateFailure("Failed to retrieve warehouses"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetWarehouse(int id)
    {
        try
        {
            // Get current user information
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(ApiResponse<WarehouseDto>.ErrorResult("User not authenticated"));
            }

            // Check if user has access to this warehouse
            var hasAccess = await _userWarehouseService.CheckWarehouseAccessAsync(currentUserId, id);
            if (!hasAccess.HasAccess)
            {
                return Forbid();
            }

            var warehouse = await _context.Warehouses
                .Include(w => w.Location)
                .FirstOrDefaultAsync(w => w.Id == id && w.IsActive);

            if (warehouse == null)
            {
                return NotFound(ApiResponse<WarehouseDto>.ErrorResult("Warehouse not found"));
            }

            // Get warehouse user assignments
            var userAssignments = await _userWarehouseService.GetWarehouseUsersAsync(id);

            var warehouseDto = new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Description = warehouse.Description,
                LocationId = warehouse.LocationId,
                LocationName = warehouse.Location != null ? warehouse.Location.Name : null,
                ContactInfo = warehouse.ContactInfo,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt,
                AssignedUsers = userAssignments,
                TotalAssignedUsers = userAssignments.Count
            };

            return Ok(ApiResponse<WarehouseDto>.SuccessResult(warehouseDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warehouse {WarehouseId}", id);
            return StatusCode(500, ApiResponse<WarehouseDto>.ErrorResult("Failed to retrieve warehouse"));
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
                return BadRequest(ApiResponse<WarehouseDto>.ErrorResult("Invalid model state", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
            }

            // Check if warehouse already exists
            var existingWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Name == request.Name);

            if (existingWarehouse != null)
            {
                return BadRequest(ApiResponse<WarehouseDto>.ErrorResult("Warehouse with this name already exists"));
            }

            var warehouse = new Warehouse
            {
                Name = request.Name,
                Description = request.Description,
                LocationId = request.LocationId,
                ContactInfo = request.ContactInfo ?? string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Warehouse created: {WarehouseName} with ID {WarehouseId}", warehouse.Name, warehouse.Id);

            var warehouseDto = new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Description = warehouse.Description,
                LocationId = warehouse.LocationId,
                LocationName = (await _context.Locations.FindAsync(warehouse.LocationId))?.Name,
                ContactInfo = warehouse.ContactInfo,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };

            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, ApiResponse<WarehouseDto>.SuccessResult(warehouseDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warehouse");
            return StatusCode(500, ApiResponse<WarehouseDto>.ErrorResult("Failed to create warehouse"));
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
                return BadRequest(ApiResponse<WarehouseDto>.ErrorResult("Invalid model state", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
            }

            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound(ApiResponse<WarehouseDto>.ErrorResult("Warehouse not found"));
            }

            // Check if another warehouse with the same name exists
            var existingWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Name == request.Name && w.Id != id);

            if (existingWarehouse != null)
            {
                return BadRequest(ApiResponse<WarehouseDto>.ErrorResult("Warehouse with this name already exists"));
            }

            warehouse.Name = request.Name;
            warehouse.Description = request.Description;
            warehouse.LocationId = request.LocationId;
            warehouse.ContactInfo = request.ContactInfo ?? string.Empty;
            warehouse.IsActive = request.IsActive;
            warehouse.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Warehouse updated: {WarehouseName} with ID {WarehouseId}", warehouse.Name, warehouse.Id);

            var warehouseDto = new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Description = warehouse.Description,
                LocationId = warehouse.LocationId,
                LocationName = (await _context.Locations.FindAsync(warehouse.LocationId))?.Name,
                ContactInfo = warehouse.ContactInfo,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };

            return Ok(ApiResponse<WarehouseDto>.SuccessResult(warehouseDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating warehouse {WarehouseId}", id);
            return StatusCode(500, ApiResponse<WarehouseDto>.ErrorResult("Failed to update warehouse"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        try
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Warehouse not found"));
            }

            // Check if warehouse has transactions
            var hasTransactions = await _context.InventoryTransactions
                .AnyAsync(t => t.WarehouseId == id);

            if (hasTransactions)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete warehouse with transactions"));
            }

            // Soft delete - set IsActive to false
            warehouse.IsActive = false;
            warehouse.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Warehouse deleted (soft): {WarehouseName} with ID {WarehouseId}", warehouse.Name, warehouse.Id);

            return Ok(ApiResponse<object>.SuccessResult(new { message = "Warehouse deleted successfully" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting warehouse {WarehouseId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to delete warehouse"));
        }
    }

    // User Assignment Endpoints
    
    /// <summary>
    /// Get warehouse's assigned users
    /// </summary>
    /// <param name="id">The unique identifier of the warehouse to retrieve user assignments for</param>
    /// <returns>Returns a list of user assignments for the specified warehouse</returns>
    /// <response code="200">Successfully retrieved warehouse's user assignments</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to view warehouse assignments (requires Admin or Manager role)</response>
    /// <response code="404">Warehouse not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Alternative endpoint for retrieving warehouse user assignments via the Warehouse controller.
    /// For comprehensive warehouse assignment management, consider using the UserWarehouse controller endpoints.
    /// 
    /// **Authorization:**
    /// - Requires Admin or Manager role
    /// 
    /// **Response Data:**
    /// - User information (ID, username, email)
    /// - Access levels for each user
    /// - Default warehouse flags
    /// - Assignment timestamps
    /// 
    /// **Use Cases:**
    /// - Warehouse-centric user management
    /// - Access control auditing
    /// - Permission reporting
    /// </remarks>
    [HttpGet("{id}/users")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetWarehouseUsers(int id)
    {
        try
        {
            var users = await _userWarehouseService.GetWarehouseUsersAsync(id);

            return Ok(ApiResponse<List<UserWarehouseDto>>.SuccessResult(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for warehouse {WarehouseId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Internal server error"));
        }
    }

    /// <summary>
    /// Bulk assign multiple users to warehouse
    /// </summary>
    /// <param name="bulkAssignDto">Bulk assignment configuration including warehouse ID, user IDs list, and access settings</param>
    /// <returns>Returns bulk assignment results with success and failure details</returns>
    /// <response code="200">Bulk assignment completed (may include partial failures)</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to perform bulk assignments (requires Admin role)</response>
    /// <response code="404">Warehouse not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Alternative endpoint for bulk user assignment via the Warehouse controller.
    /// For comprehensive warehouse assignment management, consider using the UserWarehouse controller endpoints.
    /// 
    /// **Authorization:**
    /// - Requires Admin role (highest privilege level)
    /// 
    /// **Operation Details:**
    /// - Processes all assignments individually
    /// - Continues on individual failures
    /// - Returns comprehensive success/failure report
    /// - Maintains data consistency per user
    /// 
    /// **Request Format:**
    /// ```json
    /// {
    ///   "warehouseId": 123,
    ///   "userIds": ["user1", "user2", "user3"],
    ///   "accessLevel": "ReadOnly",
    ///   "setAsDefault": false
    /// }
    /// ```
    /// 
    /// **Response Format:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "successfulAssignments": [...],
    ///     "errors": [...],
    ///     "totalRequested": 3,
    ///     "totalSuccessful": 2,
    ///     "totalFailed": 1
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpPost("bulk-assign-users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkAssignUsersToWarehouse([FromBody] BulkAssignWarehousesDto bulkAssignDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResult("Validation failed", errors));
            }

            var result = await _userWarehouseService.BulkAssignUsersToWarehouseAsync(bulkAssignDto);

            return Ok(new ApiResponse<object>
            {
                Success = result.Success,
                Data = new
                {
                    SuccessfulAssignments = result.Data,
                    Errors = result.Errors,
                    TotalRequested = bulkAssignDto.UserIds.Count,
                    TotalSuccessful = result.Data?.Count ?? 0,
                    TotalFailed = result.Errors?.Count ?? 0
                },
                ErrorMessage = result.Success ? null : "Some assignments failed",
                ValidationErrors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk assignment to warehouse {WarehouseId}", bulkAssignDto.WarehouseId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Internal server error"));
        }
    }
}
