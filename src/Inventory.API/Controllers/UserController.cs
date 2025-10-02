using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Serilog;
using System.Security.Claims;
using System.Text;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IUserWarehouseService _userWarehouseService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        UserManager<User> userManager,
        IUserWarehouseService userWarehouseService,
        ILogger<UserController> logger)
    {
        _userManager = userManager;
        _userWarehouseService = userWarehouseService;
        _logger = logger;
    }
    [HttpGet("info")]
    public ActionResult<ApiResponse<object>> GetUserInfo()
    {
        try
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            _logger.LogInformation("User info requested for: {Username}", username);
            
            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                Username = username,
                UserId = userId,
                Roles = roles
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to retrieve user info"));
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedApiResponse<UserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.UserName!.Contains(search) || u.Email!.Contains(search));
            }

            // Apply role filter
            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id);
                query = query.Where(u => userIds.Contains(u.Id));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    Email = u.Email!,
                    Role = u.Role,
                    EmailConfirmed = u.EmailConfirmed,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            // Get roles for each user
            foreach (var user in users)
            {
                var userEntity = await _userManager.FindByIdAsync(user.Id);
                if (userEntity != null)
                {
                    user.Roles = (await _userManager.GetRolesAsync(userEntity)).ToList();
                }
            }

            var pagedResponse = new PagedResponse<UserDto>
            {
                Items = users,
                total = totalCount,
                page = page,
                PageSize = pageSize
            };

            return Ok(PagedApiResponse<UserDto>.CreateSuccess(pagedResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, PagedApiResponse<UserDto>.CreateFailure("Failed to retrieve users"));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.CreateFailure("User not found"));
            }

            var roles = await _userManager.GetRolesAsync(user);
            
            // Get user's warehouse assignments
            var warehouseAssignments = await _userWarehouseService.GetUserWarehousesAsync(id);
            var defaultWarehouse = warehouseAssignments.FirstOrDefault(w => w.IsDefault);
            
            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Role = user.Role,
                Roles = roles.ToList(),
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                AssignedWarehouses = warehouseAssignments,
                DefaultWarehouseId = defaultWarehouse?.WarehouseId,
                DefaultWarehouseName = defaultWarehouse?.WarehouseName
            };

            return Ok(ApiResponse<UserDto>.CreateSuccess(userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, ApiResponse<UserDto>.CreateFailure("Failed to retrieve user"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(string id, [FromBody] UpdateUserDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<UserDto>.CreateFailure("Invalid model state", errors));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.CreateFailure("User not found"));
            }

            // Update user properties
            user.UserName = request.UserName;
            user.Email = request.Email;
            user.Role = request.Role;
            user.EmailConfirmed = request.EmailConfirmed;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse<UserDto>.CreateFailure(errors));
            }

            // Update roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, request.Role);

            _logger.LogInformation("User updated: {Username} with ID {UserId}", user.UserName, user.Id);

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Role = user.Role,
                Roles = new List<string> { request.Role },
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(ApiResponse<UserDto>.CreateSuccess(userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, ApiResponse<UserDto>.CreateFailure("Failed to update user"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("User not found"));
            }

            // Prevent deletion of the current user
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                return BadRequest(ApiResponse<object>.CreateFailure("Cannot delete your own account"));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse<object>.CreateFailure(errors));
            }

            _logger.LogInformation("User deleted: {Username} with ID {UserId}", user.UserName, user.Id);

            return Ok(ApiResponse<object>.CreateSuccess(new { message = "User deleted successfully" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to delete user"));
        }
    }

    [HttpGet("export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.UserName!.Contains(search) || u.Email!.Contains(search));
            }

            // Apply role filter
            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id);
                query = query.Where(u => userIds.Contains(u.Id));
            }

            var users = await query
                .OrderBy(u => u.UserName)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    Email = u.Email!,
                    Role = u.Role,
                    EmailConfirmed = u.EmailConfirmed,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            // Get roles for each user
            foreach (var user in users)
            {
                var userEntity = await _userManager.FindByIdAsync(user.Id);
                if (userEntity != null)
                {
                    user.Roles = (await _userManager.GetRolesAsync(userEntity)).ToList();
                }
            }

            // Generate CSV content
            var csv = new StringBuilder();
            csv.AppendLine("Username,Email,Role,EmailConfirmed,CreatedAt,UpdatedAt");
            
            foreach (var user in users)
            {
                csv.AppendLine($"{user.UserName},{user.Email},{user.Role},{user.EmailConfirmed},{user.CreatedAt:yyyy-MM-dd HH:mm:ss},{user.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"users-export-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users");
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to export users"));
        }
    }

    [HttpPost("{id}/change-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(string id, [FromBody] ChangePasswordDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<object>.CreateFailure("Invalid model state", errors));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.CreateFailure("User not found"));
            }

            // Remove current password and set new one
            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse<object>.CreateFailure(errors));
            }

            _logger.LogInformation("Password changed for user: {Username} with ID {UserId}", user.UserName, user.Id);

            return Ok(ApiResponse<object>.CreateSuccess(new { message = "Password changed successfully" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", id);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Failed to change password"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<UserDto>.CreateFailure("Invalid model state", errors));
            }

            // Check if username already exists
            var existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<UserDto>.CreateFailure("Username already exists"));
            }

            // Check if email already exists
            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return BadRequest(ApiResponse<UserDto>.CreateFailure("Email already exists"));
            }

            var user = new Inventory.API.Models.User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.UserName,
                Email = request.Email,
                EmailConfirmed = request.EmailConfirmed,
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse<UserDto>.CreateFailure(errors));
            }

            // Assign role to user
            await _userManager.AddToRoleAsync(user, request.Role);

            _logger.LogInformation("User created: {Username} with role {Role} and ID {UserId}", 
                user.UserName, user.Role, user.Id);

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Role = user.Role,
                Roles = new List<string> { request.Role },
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Created($"/api/user/{user.Id}", ApiResponse<UserDto>.CreateSuccess(userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, ApiResponse<UserDto>.CreateFailure("Failed to create user"));
        }
    }

    // Warehouse Assignment Endpoints
    
    /// <summary>
    /// Get user's warehouse assignments
    /// </summary>
    /// <param name="id">The unique identifier of the user to retrieve warehouse assignments for</param>
    /// <returns>Returns a list of warehouse assignments for the specified user</returns>
    /// <response code="200">Successfully retrieved user's warehouse assignments</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to view warehouse assignments (requires Admin or Manager role)</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Alternative endpoint for retrieving user warehouse assignments via the User controller.
    /// For comprehensive warehouse assignment management, consider using the UserWarehouse controller endpoints.
    /// 
    /// **Authorization:**
    /// - Requires Admin or Manager role
    /// 
    /// **Response Data:**
    /// - Complete warehouse assignment details
    /// - Access levels and default warehouse information
    /// - Assignment timestamps
    /// </remarks>
    [HttpGet("{id}/warehouses")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<List<UserWarehouseDto>>>> GetUserWarehouses(string id)
    {
        try
        {
            var warehouses = await _userWarehouseService.GetUserWarehousesAsync(id);

            return Ok(ApiResponse<List<UserWarehouseDto>>.CreateSuccess(warehouses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouses for user {UserId}", id);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Assign warehouse to user
    /// </summary>
    /// <param name="id">The unique identifier of the user to assign warehouse access to</param>
    /// <param name="assignmentDto">Assignment configuration including warehouse ID, access level, and default warehouse flag</param>
    /// <returns>Returns the created warehouse assignment details</returns>
    /// <response code="200">Warehouse successfully assigned to user</response>
    /// <response code="400">Invalid request data or business rule violation</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to assign warehouses (requires Admin or Manager role)</response>
    /// <response code="404">User or warehouse not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Alternative endpoint for assigning warehouses to users via the User controller.
    /// For comprehensive warehouse assignment management, consider using the UserWarehouse controller endpoints.
    /// 
    /// **Authorization:**
    /// - Requires Admin or Manager role
    /// 
    /// **Business Rules:**
    /// - Same validation rules as UserWarehouse controller
    /// - Prevents duplicate assignments
    /// - Manages default warehouse logic
    /// </remarks>
    [HttpPost("{id}/warehouses")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<UserWarehouseDto>>> AssignWarehouseToUser(string id, [FromBody] AssignWarehouseDto assignmentDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.CreateFailure("Validation failed", errors));
            }

            var result = await _userWarehouseService.AssignWarehouseToUserAsync(id, assignmentDto);

            if (result.Success)
            {
                return Ok(ApiResponse<UserWarehouseDto>.CreateSuccess(result.Data!));
            }

            return BadRequest(ApiResponse<object>.CreateFailure(result.Error!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning warehouse to user {UserId}", id);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Remove warehouse assignment from user
    /// </summary>
    /// <param name="id">The unique identifier of the user to remove warehouse access from</param>
    /// <param name="warehouseId">The unique identifier of the warehouse to remove access to</param>
    /// <returns>Returns success message when assignment is removed</returns>
    /// <response code="200">Warehouse assignment successfully removed</response>
    /// <response code="400">Assignment does not exist or cannot be removed</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to remove warehouse assignments (requires Admin or Manager role)</response>
    /// <response code="404">User, warehouse, or assignment not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Alternative endpoint for removing warehouse assignments via the User controller.
    /// For comprehensive warehouse assignment management, consider using the UserWarehouse controller endpoints.
    /// 
    /// **Authorization:**
    /// - Requires Admin or Manager role
    /// 
    /// **Important Considerations:**
    /// - Removing default warehouse clears user's default selection
    /// - May impact user's existing workflows and transactions
    /// </remarks>
    [HttpDelete("{id}/warehouses/{warehouseId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveWarehouseAssignment(string id, int warehouseId)
    {
        try
        {
            var result = await _userWarehouseService.RemoveWarehouseAssignmentAsync(id, warehouseId);

            if (result.Success)
            {
                return Ok(ApiResponse<object>.CreateSuccess(new { message = "Warehouse assignment removed successfully" }));
            }

            return BadRequest(ApiResponse<object>.CreateFailure(result.Error!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing warehouse {WarehouseId} assignment from user {UserId}", 
                warehouseId, id);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Update warehouse assignment for user
    /// </summary>
    /// <param name="id">The unique identifier of the user whose warehouse assignment will be updated</param>
    /// <param name="warehouseId">The unique identifier of the warehouse assignment to update</param>
    /// <param name="updateDto">Updated assignment configuration including access level and default warehouse flag</param>
    /// <returns>Returns the updated warehouse assignment details</returns>
    /// <response code="200">Warehouse assignment successfully updated</response>
    /// <response code="400">Invalid request data or business rule violation</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to update warehouse assignments (requires Admin or Manager role)</response>
    /// <response code="404">User, warehouse, or assignment not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Alternative endpoint for updating warehouse assignments via the User controller.
    /// For comprehensive warehouse assignment management, consider using the UserWarehouse controller endpoints.
    /// 
    /// **Authorization:**
    /// - Requires Admin or Manager role
    /// 
    /// **Updatable Properties:**
    /// - Access level (Full/ReadOnly/Limited)
    /// - Default warehouse flag
    /// </remarks>
    [HttpPut("{id}/warehouses/{warehouseId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<UserWarehouseDto>>> UpdateWarehouseAssignment(string id, int warehouseId, [FromBody] UpdateWarehouseAssignmentDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.CreateFailure("Validation failed", errors));
            }

            var result = await _userWarehouseService.UpdateWarehouseAssignmentAsync(id, warehouseId, updateDto);

            if (result.Success)
            {
                return Ok(ApiResponse<UserWarehouseDto>.CreateSuccess(result.Data!));
            }

            return BadRequest(ApiResponse<object>.CreateFailure(result.Error!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating warehouse {WarehouseId} assignment for user {UserId}", 
                warehouseId, id);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Set default warehouse for user
    /// </summary>
    /// <param name="id">The unique identifier of the user to set default warehouse for</param>
    /// <param name="warehouseId">The unique identifier of the warehouse to set as default</param>
    /// <returns>Returns success message when default warehouse is set</returns>
    /// <response code="200">Default warehouse successfully set</response>
    /// <response code="400">User does not have access to the specified warehouse</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to set default warehouses (requires Admin or Manager role)</response>
    /// <response code="404">User, warehouse, or assignment not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Alternative endpoint for setting default warehouse via the User controller.
    /// For comprehensive warehouse assignment management, consider using the UserWarehouse controller endpoints.
    /// 
    /// **Authorization:**
    /// - Requires Admin or Manager role
    /// 
    /// **Business Logic:**
    /// - User must have existing assignment to the warehouse
    /// - Automatically unsets previous default warehouse
    /// - Default warehouse used for transaction defaults
    /// </remarks>
    [HttpPut("{id}/warehouses/{warehouseId}/default")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<object>>> SetDefaultWarehouse(string id, int warehouseId)
    {
        try
        {
            var result = await _userWarehouseService.SetDefaultWarehouseAsync(id, warehouseId);

            if (result.Success)
            {
                return Ok(ApiResponse<object>.CreateSuccess(new { message = "Default warehouse set successfully" }));
            }

            return BadRequest(ApiResponse<object>.CreateFailure(result.Error!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default warehouse {WarehouseId} for user {UserId}", 
                warehouseId, id);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Internal server error"));
        }
    }
}
