using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.API.Controllers;

/// <summary>
/// API Controller for managing user-warehouse assignment relationships and access control
/// </summary>
/// <remarks>
/// This controller provides comprehensive management of user-warehouse assignments including:
/// 
/// **Core Functionality:**
/// - Assign/remove warehouse access for users
/// - Update assignment details (access levels, default warehouse)
/// - Bulk assignment operations
/// - Access validation and checking
/// 
/// **Access Levels:**
/// - **Full**: Complete read/write access to all warehouse operations
/// - **ReadOnly**: View-only access to warehouse data and reports
/// - **Limited**: Restricted access to specific warehouse operations
/// 
/// **Business Rules:**
/// - Users can have multiple warehouse assignments
/// - Only one warehouse per user can be marked as default
/// - Admin and Manager users have implicit access to all warehouses
/// - Regular users only access explicitly assigned warehouses
/// 
/// **Security:**
/// - All endpoints require authentication
/// - Assignment operations require Admin or Manager roles
/// - Access queries allow users to check their own access
/// - Comprehensive authorization checks for cross-user operations
/// 
/// **Related Controllers:**
/// - UserController: User-centric warehouse assignment operations
/// - WarehouseController: Warehouse-centric user assignment operations
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
[Tags("User-Warehouse Assignments")]
public class UserWarehouseController : ControllerBase
{
    private readonly IUserWarehouseService _userWarehouseService;
    private readonly ILogger<UserWarehouseController> _logger;

    public UserWarehouseController(
        IUserWarehouseService userWarehouseService,
        ILogger<UserWarehouseController> logger)
    {
        _userWarehouseService = userWarehouseService;
        _logger = logger;
    }

    /// <summary>
    /// Assign a warehouse to a user with specific access level
    /// </summary>
    /// <param name="userId">The unique identifier of the user to assign warehouse access to</param>
    /// <param name="assignmentDto">Assignment configuration including warehouse ID, access level (Full/ReadOnly/Limited), and default warehouse flag</param>
    /// <returns>Returns the created warehouse assignment with user and warehouse details</returns>
    /// <response code="200">Warehouse successfully assigned to user</response>
    /// <response code="400">Invalid request data or business rule violation (e.g., warehouse already assigned, invalid access level)</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to assign warehouses (requires Admin or Manager role)</response>
    /// <response code="404">User or warehouse not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This endpoint allows administrators and managers to assign warehouse access to users.
    /// 
    /// **Access Levels:**
    /// - **Full**: Complete read/write access to all warehouse operations
    /// - **ReadOnly**: View-only access to warehouse data
    /// - **Limited**: Restricted access to specific warehouse operations
    /// 
    /// **Business Rules:**
    /// - Users can only have one assignment per warehouse
    /// - Only one warehouse per user can be marked as default
    /// - If setting as default, any existing default warehouse will be unset
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "warehouseId": 123,
    ///   "accessLevel": "Full",
    ///   "isDefault": true
    /// }
    /// ```
    /// </remarks>
    [HttpPost("users/{userId}/warehouses")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<UserWarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserWarehouseDto>>> AssignWarehouseToUser(string userId, [FromBody] AssignWarehouseDto assignmentDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<UserWarehouseDto>.CreateFailure("Validation failed", errors));
            }

            var result = await _userWarehouseService.AssignWarehouseToUserAsync(userId, assignmentDto);

            if (result.Success && result.Data != null)
            {
                return Ok(ApiResponse<UserWarehouseDto>.CreateSuccess(result.Data));
            }

            return BadRequest(ApiResponse<UserWarehouseDto>.CreateFailure(result.Error ?? "Failed to assign warehouse."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning warehouse to user {UserId}", userId);
            return StatusCode(500, ApiResponse<UserWarehouseDto>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Remove warehouse assignment from user
    /// </summary>
    /// <param name="userId">The unique identifier of the user to remove warehouse access from</param>
    /// <param name="warehouseId">The unique identifier of the warehouse to remove access to</param>
    /// <returns>Returns success message when assignment is removed</returns>
    /// <response code="200">Warehouse assignment successfully removed</response>
    /// <response code="400">Assignment does not exist or cannot be removed</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to remove warehouse assignments (requires Admin or Manager role)</response>
    /// <response code="404">User, warehouse, or assignment not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This endpoint allows administrators and managers to remove warehouse access from users.
    /// 
    /// **Important Notes:**
    /// - Removing a default warehouse assignment will clear the user's default warehouse
    /// - Users with active transactions in the warehouse may require special handling
    /// - Consider the impact on user workflows before removing access
    /// </remarks>
    [HttpDelete("users/{userId}/warehouses/{warehouseId}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveWarehouseAssignment(string userId, int warehouseId)
    {
        try
        {
            var result = await _userWarehouseService.RemoveWarehouseAssignmentAsync(userId, warehouseId);

            if (result.Success)
            {
                return Ok(ApiResponse<object>.CreateSuccess(new { message = "Warehouse assignment removed successfully" }));
            }

            return BadRequest(ApiResponse<object>.CreateFailure(result.Error ?? "Failed to remove assignment."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing warehouse {WarehouseId} assignment from user {UserId}", 
                warehouseId, userId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Update warehouse assignment details for a user
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose warehouse assignment will be updated</param>
    /// <param name="warehouseId">The unique identifier of the warehouse assignment to update</param>
    /// <param name="updateDto">Updated assignment configuration including access level and default warehouse flag</param>
    /// <returns>Returns the updated warehouse assignment with current details</returns>
    /// <response code="200">Warehouse assignment successfully updated</response>
    /// <response code="400">Invalid request data or business rule violation</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to update warehouse assignments (requires Admin or Manager role)</response>
    /// <response code="404">User, warehouse, or assignment not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This endpoint allows administrators and managers to modify existing warehouse assignments.
    /// 
    /// **Updatable Fields:**
    /// - Access Level (Full/ReadOnly/Limited)
    /// - Default Warehouse Flag
    /// 
    /// **Business Rules:**
    /// - Setting as default will unset any existing default warehouse for the user
    /// - Access level changes take effect immediately
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "accessLevel": "ReadOnly",
    ///   "isDefault": false
    /// }
    /// ```
    /// </remarks>
    [HttpPut("users/{userId}/warehouses/{warehouseId}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<UserWarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserWarehouseDto>>> UpdateWarehouseAssignment(string userId, int warehouseId, [FromBody] UpdateWarehouseAssignmentDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<UserWarehouseDto>.CreateFailure("Validation failed", errors));
            }

            var result = await _userWarehouseService.UpdateWarehouseAssignmentAsync(userId, warehouseId, updateDto);

            if (result.Success && result.Data != null)
            {
                return Ok(ApiResponse<UserWarehouseDto>.CreateSuccess(result.Data));
            }

            return BadRequest(ApiResponse<UserWarehouseDto>.CreateFailure(result.Error ?? "Failed to update assignment."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating warehouse {WarehouseId} assignment for user {UserId}", 
                warehouseId, userId);
            return StatusCode(500, ApiResponse<UserWarehouseDto>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Set default warehouse for user
    /// </summary>
    /// <param name="userId">The unique identifier of the user to set default warehouse for</param>
    /// <param name="warehouseId">The unique identifier of the warehouse to set as default</param>
    /// <returns>Returns success message when default warehouse is set</returns>
    /// <response code="200">Default warehouse successfully set</response>
    /// <response code="400">User does not have access to the specified warehouse or assignment does not exist</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to set default warehouses (requires Admin or Manager role)</response>
    /// <response code="404">User, warehouse, or assignment not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This endpoint allows administrators and managers to designate a default warehouse for a user.
    /// 
    /// **Business Rules:**
    /// - User must already have an assignment to the warehouse
    /// - Only one warehouse per user can be marked as default
    /// - Setting a new default warehouse will automatically unset the previous default
    /// - Default warehouse is used for automatic warehouse selection in transactions
    /// </remarks>
    [HttpPut("users/{userId}/warehouses/{warehouseId}/default")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> SetDefaultWarehouse(string userId, int warehouseId)
    {
        try
        {
            var result = await _userWarehouseService.SetDefaultWarehouseAsync(userId, warehouseId);

            if (result.Success)
            {
                return Ok(ApiResponse<object>.CreateSuccess(new { message = "Default warehouse set successfully" }));
            }

            return BadRequest(ApiResponse<object>.CreateFailure(result.Error ?? "Failed to set default warehouse."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default warehouse {WarehouseId} for user {UserId}", 
                warehouseId, userId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Get user's warehouse assignments
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve warehouse assignments for</param>
    /// <returns>Returns a list of warehouse assignments for the specified user</returns>
    /// <response code="200">Successfully retrieved user's warehouse assignments</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to view warehouse assignments for this user</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This endpoint retrieves all warehouse assignments for a specific user.
    /// 
    /// **Authorization Rules:**
    /// - Users can view their own warehouse assignments
    /// - Admin and Manager roles can view any user's assignments
    /// 
    /// **Response includes:**
    /// - Warehouse details (ID, name, location)
    /// - Access level (Full/ReadOnly/Limited)
    /// - Default warehouse flag
    /// - Assignment timestamp
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "userId": "user123",
    ///       "warehouseId": 1,
    ///       "warehouseName": "Main Warehouse",
    ///       "accessLevel": "Full",
    ///       "isDefault": true,
    ///       "assignedAt": "2024-01-01T00:00:00Z"
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    [HttpGet("users/{userId}/warehouses")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<UserWarehouseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<UserWarehouseDto>>>> GetUserWarehouses(string userId)
    {
        try
        {
            // Check if user can access this information
            var currentUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            var currentUserRole = User.FindFirst("role")?.Value;

            if (currentUserId != userId && currentUserRole != "Admin" && currentUserRole != "Manager")
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<List<UserWarehouseDto>>.CreateFailure("Forbidden"));
            }

            var warehouses = await _userWarehouseService.GetUserWarehousesAsync(userId);

            return Ok(ApiResponse<List<UserWarehouseDto>>.CreateSuccess(warehouses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouses for user {UserId}", userId);
            return StatusCode(500, ApiResponse<List<UserWarehouseDto>>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Get warehouse's assigned users
    /// </summary>
    /// <param name="warehouseId">The unique identifier of the warehouse to retrieve user assignments for</param>
    /// <returns>Returns a list of user assignments for the specified warehouse</returns>
    /// <response code="200">Successfully retrieved warehouse's user assignments</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to view warehouse assignments (requires Admin or Manager role)</response>
    /// <response code="404">Warehouse not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This endpoint retrieves all user assignments for a specific warehouse.
    /// 
    /// **Authorization:**
    /// - Requires Admin or Manager role
    /// 
    /// **Response includes:**
    /// - User details (ID, username, email)
    /// - Access level (Full/ReadOnly/Limited)
    /// - Default warehouse flag
    /// - Assignment timestamp
    /// 
    /// **Use Cases:**
    /// - Warehouse access management
    /// - User permission auditing
    /// - Access control reporting
    /// </remarks>
    [HttpGet("warehouses/{warehouseId}/users")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<List<UserWarehouseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<UserWarehouseDto>>>> GetWarehouseUsers(int warehouseId)
    {
        try
        {
            var users = await _userWarehouseService.GetWarehouseUsersAsync(warehouseId);

            return Ok(ApiResponse<List<UserWarehouseDto>>.CreateSuccess(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for warehouse {WarehouseId}", warehouseId);
            return StatusCode(500, ApiResponse<List<UserWarehouseDto>>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Bulk assign multiple users to a warehouse
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
    /// This endpoint allows administrators to assign multiple users to a warehouse in a single operation.
    /// 
    /// **Authorization:**
    /// - Requires Admin role (higher privilege than individual assignments)
    /// 
    /// **Features:**
    /// - Processes all assignments atomically per user
    /// - Continues processing even if individual assignments fail
    /// - Returns detailed success/failure information
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "warehouseId": 123,
    ///   "userIds": ["user1", "user2", "user3"],
    ///   "accessLevel": "ReadOnly",
    ///   "setAsDefault": false
    /// }
    /// ```
    /// 
    /// **Example Response:**
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
    [HttpPost("warehouses/bulk-assign")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> BulkAssignUsersToWarehouse([FromBody] BulkAssignWarehousesDto bulkAssignDto)
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

            var result = await _userWarehouseService.BulkAssignUsersToWarehouseAsync(bulkAssignDto);

            var responseData = new
            {
                SuccessfulAssignments = result.Data,
                Errors = result.Errors,
                TotalRequested = bulkAssignDto.UserIds.Count,
                TotalSuccessful = result.Data.Count,
                TotalFailed = result.Errors.Count
            };

            if (result.Success)
            {
                return Ok(ApiResponse<object>.CreateSuccess(responseData));
            }
            
            var response = ApiResponse<object>.CreateFailure("Some assignments failed", result.Errors);
            response.Data = responseData;
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk assignment to warehouse {WarehouseId}", bulkAssignDto.WarehouseId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Check if user has access to warehouse with optional access level validation
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check access for</param>
    /// <param name="warehouseId">The unique identifier of the warehouse to check access to</param>
    /// <param name="requiredAccessLevel">Optional minimum access level required (Full/ReadOnly/Limited)</param>
    /// <returns>Returns access check results including access status and current access level</returns>
    /// <response code="200">Access check completed successfully</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to check access for this user</response>
    /// <response code="404">User or warehouse not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This endpoint validates whether a user has access to a specific warehouse and optionally checks if they meet a minimum access level requirement.
    /// 
    /// **Authorization Rules:**
    /// - Users can check their own access
    /// - Admin and Manager roles can check any user's access
    /// 
    /// **Access Level Hierarchy:**
    /// - **Full** > **ReadOnly** > **Limited**
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "hasAccess": true,
    ///     "accessLevel": "Full",
    ///     "warehouseId": 123,
    ///     "userId": "user123",
    ///     "requiredAccessLevel": "ReadOnly"
    ///   }
    /// }
    /// ```
    /// 
    /// **Use Cases:**
    /// - Pre-transaction access validation
    /// - UI permission checks
    /// - Access control auditing
    /// </remarks>
    [HttpGet("users/{userId}/warehouses/{warehouseId}/access")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> CheckWarehouseAccess(string userId, int warehouseId, [FromQuery] string? requiredAccessLevel = null)
    {
        try
        {
            // Check if user can access this information
            var currentUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            var currentUserRole = User.FindFirst("role")?.Value;

            if (currentUserId != userId && currentUserRole != "Admin" && currentUserRole != "Manager")
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.CreateFailure("Forbidden"));
            }

            var result = await _userWarehouseService.CheckWarehouseAccessAsync(userId, warehouseId, requiredAccessLevel);

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                HasAccess = result.HasAccess,
                AccessLevel = result.AccessLevel,
                WarehouseId = warehouseId,
                UserId = userId,
                RequiredAccessLevel = requiredAccessLevel
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking warehouse {WarehouseId} access for user {UserId}", 
                warehouseId, userId);
            return StatusCode(500, ApiResponse<object>.CreateFailure("Internal server error"));
        }
    }

    /// <summary>
    /// Get warehouses accessible by user based on assignments and role
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve accessible warehouses for</param>
    /// <returns>Returns a list of warehouse IDs that the user has access to</returns>
    /// <response code="200">Successfully retrieved accessible warehouse IDs</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to view accessible warehouses for this user</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This endpoint returns warehouse IDs that a user can access based on their assignments and role.
    /// 
    /// **Authorization Rules:**
    /// - Users can view their own accessible warehouses
    /// - Admin and Manager roles can view any user's accessible warehouses
    /// 
    /// **Access Logic:**
    /// - **Admin users**: Access to all warehouses regardless of assignments
    /// - **Manager users**: Access to all warehouses regardless of assignments
    /// - **Regular users**: Access only to explicitly assigned warehouses
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": [1, 2, 5, 8]
    /// }
    /// ```
    /// 
    /// **Use Cases:**
    /// - Filtering warehouse lists in UI
    /// - Transaction warehouse validation
    /// - Report scope limitation
    /// </remarks>
    [HttpGet("users/{userId}/accessible-warehouses")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<int>>>> GetAccessibleWarehouses(string userId)
    {
        try
        {
            // Check if user can access this information
            var currentUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            var currentUserRole = User.FindFirst("role")?.Value;

            if (currentUserId != userId && currentUserRole != "Admin" && currentUserRole != "Manager")
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<List<int>>.CreateFailure("Forbidden"));
            }

            var userRole = currentUserId == userId ? currentUserRole! : "User"; // Default to User for non-self queries
            var warehouseIds = await _userWarehouseService.GetAccessibleWarehouseIdsAsync(userId, userRole);

            return Ok(ApiResponse<List<int>>.CreateSuccess(warehouseIds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accessible warehouses for user {UserId}", userId);
            return StatusCode(500, ApiResponse<List<int>>.CreateFailure("Internal server error"));
        }
    }
}