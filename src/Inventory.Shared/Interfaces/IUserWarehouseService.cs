using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

/// <summary>
/// Interface for UserWarehouse management operations on the client side
/// </summary>
public interface IUserWarehouseService
{
    /// <summary>
    /// Get user's warehouse assignments
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of user's warehouse assignments</returns>
    Task<ApiResponse<List<UserWarehouseDto>>?> GetUserWarehousesAsync(string userId);

    /// <summary>
    /// Get warehouse's assigned users
    /// </summary>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <returns>List of warehouse's user assignments</returns>
    Task<ApiResponse<List<UserWarehouseDto>>?> GetWarehouseUsersAsync(int warehouseId);

    /// <summary>
    /// Assign warehouse to user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="assignmentDto">Assignment details</param>
    /// <returns>Assignment result</returns>
    Task<ApiResponse<UserWarehouseDto>?> AssignWarehouseToUserAsync(string userId, AssignWarehouseDto assignmentDto);

    /// <summary>
    /// Remove warehouse assignment from user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <returns>Removal result</returns>
    Task<ApiResponse<object>?> RemoveWarehouseAssignmentAsync(string userId, int warehouseId);

    /// <summary>
    /// Update warehouse assignment details
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="updateDto">Update details</param>
    /// <returns>Updated assignment</returns>
    Task<ApiResponse<UserWarehouseDto>?> UpdateWarehouseAssignmentAsync(string userId, int warehouseId, UpdateWarehouseAssignmentDto updateDto);

    /// <summary>
    /// Set default warehouse for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <returns>Success result</returns>
    Task<ApiResponse<object>?> SetDefaultWarehouseAsync(string userId, int warehouseId);

    /// <summary>
    /// Bulk assign users to warehouse
    /// </summary>
    /// <param name="bulkAssignDto">Bulk assignment details</param>
    /// <returns>Bulk assignment result</returns>
    Task<ApiResponse<object>?> BulkAssignUsersToWarehouseAsync(BulkAssignWarehousesDto bulkAssignDto);

    /// <summary>
    /// Check if user has access to warehouse
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="requiredAccessLevel">Required access level (optional)</param>
    /// <returns>Access check result</returns>
    Task<ApiResponse<object>?> CheckWarehouseAccessAsync(string userId, int warehouseId, string? requiredAccessLevel = null);

    /// <summary>
    /// Get warehouses accessible by user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of accessible warehouse IDs</returns>
    Task<ApiResponse<List<int>>?> GetAccessibleWarehousesAsync(string userId);
}