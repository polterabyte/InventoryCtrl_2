using Inventory.API.Models;
using Inventory.Shared.DTOs;

namespace Inventory.API.Services;

/// <summary>
/// Interface for UserWarehouse service providing business logic for user-warehouse assignments
/// </summary>
public interface IUserWarehouseService
{
    /// <summary>
    /// Assign a warehouse to a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="assignmentDto">Assignment details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>UserWarehouse assignment result</returns>
    Task<(bool Success, UserWarehouseDto? Data, string? Error)> AssignWarehouseToUserAsync(
        string userId, AssignWarehouseDto assignmentDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove warehouse assignment from user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<(bool Success, string? Error)> RemoveWarehouseAssignmentAsync(
        string userId, int warehouseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update warehouse assignment details
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="updateDto">Update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated assignment result</returns>
    Task<(bool Success, UserWarehouseDto? Data, string? Error)> UpdateWarehouseAssignmentAsync(
        string userId, int warehouseId, UpdateWarehouseAssignmentDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set default warehouse for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="warehouseId">Warehouse ID to set as default</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<(bool Success, string? Error)> SetDefaultWarehouseAsync(
        string userId, int warehouseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's warehouse assignments
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's warehouse assignments</returns>
    Task<List<UserWarehouseDto>> GetUserWarehousesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get warehouse's assigned users
    /// </summary>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of warehouse's user assignments</returns>
    Task<List<UserWarehouseDto>> GetWarehouseUsersAsync(int warehouseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk assign users to warehouse
    /// </summary>
    /// <param name="bulkAssignDto">Bulk assignment details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk assignment result</returns>
    Task<(bool Success, List<UserWarehouseDto> Data, List<string> Errors)> BulkAssignUsersToWarehouseAsync(
        BulkAssignWarehousesDto bulkAssignDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has access to warehouse
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="requiredAccessLevel">Required access level (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access check result</returns>
    Task<(bool HasAccess, string? AccessLevel)> CheckWarehouseAccessAsync(
        string userId, int warehouseId, string? requiredAccessLevel = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get warehouses accessible by user based on role and assignments
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="userRole">User role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of accessible warehouse IDs</returns>
    Task<List<int>> GetAccessibleWarehouseIdsAsync(string userId, string userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate assignment business rules
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="isDefault">Whether this should be default warehouse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<(bool IsValid, List<string> Errors)> ValidateAssignmentAsync(
        string userId, int warehouseId, bool isDefault = false, CancellationToken cancellationToken = default);
}