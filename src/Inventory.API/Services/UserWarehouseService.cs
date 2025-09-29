using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Inventory.API.Models;
using Inventory.Shared.DTOs;

namespace Inventory.API.Services;

/// <summary>
/// Service implementation for UserWarehouse business logic
/// </summary>
public class UserWarehouseService : IUserWarehouseService
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserWarehouseService> _logger;

    public UserWarehouseService(
        AppDbContext context,
        UserManager<User> userManager,
        ILogger<UserWarehouseService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<(bool Success, UserWarehouseDto? Data, string? Error)> AssignWarehouseToUserAsync(
        string userId, AssignWarehouseDto assignmentDto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate business rules
            var validation = await ValidateAssignmentAsync(userId, assignmentDto.WarehouseId, assignmentDto.IsDefault, cancellationToken);
            if (!validation.IsValid)
            {
                return (false, null, string.Join("; ", validation.Errors));
            }

            // Check if assignment already exists
            var existingAssignment = await _context.UserWarehouses
                .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == assignmentDto.WarehouseId, cancellationToken);

            if (existingAssignment != null)
            {
                return (false, null, "User is already assigned to this warehouse");
            }

            // Clear previous default if this should be default
            if (assignmentDto.IsDefault)
            {
                await ClearPreviousDefaultAsync(userId, cancellationToken);
            }

            // Create new assignment
            var userWarehouse = new UserWarehouse
            {
                UserId = userId,
                WarehouseId = assignmentDto.WarehouseId,
                AccessLevel = assignmentDto.AccessLevel,
                IsDefault = assignmentDto.IsDefault,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserWarehouses.Add(userWarehouse);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} assigned to warehouse {WarehouseId} with access level {AccessLevel}",
                userId, assignmentDto.WarehouseId, assignmentDto.AccessLevel);

            // Return the created assignment
            var result = await GetUserWarehouseAssignmentAsync(userId, assignmentDto.WarehouseId, cancellationToken);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning warehouse {WarehouseId} to user {UserId}", 
                assignmentDto.WarehouseId, userId);
            return (false, null, "Failed to assign warehouse to user");
        }
    }

    public async Task<(bool Success, string? Error)> RemoveWarehouseAssignmentAsync(
        string userId, int warehouseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var assignment = await _context.UserWarehouses
                .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == warehouseId, cancellationToken);

            if (assignment == null)
            {
                return (false, "Warehouse assignment not found");
            }

            // Check if this is the user's only assignment
            var userAssignmentCount = await _context.UserWarehouses
                .CountAsync(uw => uw.UserId == userId, cancellationToken);

            if (userAssignmentCount <= 1)
            {
                return (false, "Cannot remove user's last warehouse assignment. Users must have at least one warehouse assignment.");
            }

            // If removing default warehouse, set another as default
            if (assignment.IsDefault)
            {
                var newDefault = await _context.UserWarehouses
                    .Where(uw => uw.UserId == userId && uw.WarehouseId != warehouseId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (newDefault != null)
                {
                    newDefault.IsDefault = true;
                    newDefault.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.UserWarehouses.Remove(assignment);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed warehouse {WarehouseId} assignment from user {UserId}", 
                warehouseId, userId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing warehouse {WarehouseId} assignment from user {UserId}", 
                warehouseId, userId);
            return (false, "Failed to remove warehouse assignment");
        }
    }

    public async Task<(bool Success, UserWarehouseDto? Data, string? Error)> UpdateWarehouseAssignmentAsync(
        string userId, int warehouseId, UpdateWarehouseAssignmentDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var assignment = await _context.UserWarehouses
                .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == warehouseId, cancellationToken);

            if (assignment == null)
            {
                return (false, null, "Warehouse assignment not found");
            }

            // Handle default warehouse change
            if (updateDto.IsDefault && !assignment.IsDefault)
            {
                await ClearPreviousDefaultAsync(userId, cancellationToken);
            }

            // Update assignment
            assignment.AccessLevel = updateDto.AccessLevel;
            assignment.IsDefault = updateDto.IsDefault;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated warehouse {WarehouseId} assignment for user {UserId}", 
                warehouseId, userId);

            // Return updated assignment
            var result = await GetUserWarehouseAssignmentAsync(userId, warehouseId, cancellationToken);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating warehouse {WarehouseId} assignment for user {UserId}", 
                warehouseId, userId);
            return (false, null, "Failed to update warehouse assignment");
        }
    }

    public async Task<(bool Success, string? Error)> SetDefaultWarehouseAsync(
        string userId, int warehouseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var assignment = await _context.UserWarehouses
                .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == warehouseId, cancellationToken);

            if (assignment == null)
            {
                return (false, "User is not assigned to this warehouse");
            }

            if (assignment.IsDefault)
            {
                return (true, null); // Already default
            }

            // Clear previous default
            await ClearPreviousDefaultAsync(userId, cancellationToken);

            // Set new default
            assignment.IsDefault = true;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Set warehouse {WarehouseId} as default for user {UserId}", 
                warehouseId, userId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default warehouse {WarehouseId} for user {UserId}", 
                warehouseId, userId);
            return (false, "Failed to set default warehouse");
        }
    }

    public async Task<List<UserWarehouseDto>> GetUserWarehousesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserWarehouses
                .Where(uw => uw.UserId == userId)
                .Include(uw => uw.Warehouse)
                .Select(uw => new UserWarehouseDto
                {
                    UserId = uw.UserId,
                    WarehouseId = uw.WarehouseId,
                    WarehouseName = uw.Warehouse.Name,
                    IsDefault = uw.IsDefault,
                    AccessLevel = uw.AccessLevel,
                    AssignedAt = uw.AssignedAt,
                    CreatedAt = uw.CreatedAt,
                    UpdatedAt = uw.UpdatedAt
                })
                .OrderByDescending(uw => uw.IsDefault)
                .ThenBy(uw => uw.WarehouseName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouses for user {UserId}", userId);
            return new List<UserWarehouseDto>();
        }
    }

    public async Task<List<UserWarehouseDto>> GetWarehouseUsersAsync(int warehouseId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserWarehouses
                .Where(uw => uw.WarehouseId == warehouseId)
                .Include(uw => uw.User)
                .Select(uw => new UserWarehouseDto
                {
                    UserId = uw.UserId,
                    WarehouseId = uw.WarehouseId,
                    WarehouseName = uw.Warehouse.Name,
                    IsDefault = uw.IsDefault,
                    AccessLevel = uw.AccessLevel,
                    AssignedAt = uw.AssignedAt,
                    CreatedAt = uw.CreatedAt,
                    UpdatedAt = uw.UpdatedAt
                })
                .OrderBy(uw => uw.UserId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for warehouse {WarehouseId}", warehouseId);
            return new List<UserWarehouseDto>();
        }
    }

    public async Task<(bool Success, List<UserWarehouseDto> Data, List<string> Errors)> BulkAssignUsersToWarehouseAsync(
        BulkAssignWarehousesDto bulkAssignDto, CancellationToken cancellationToken = default)
    {
        var results = new List<UserWarehouseDto>();
        var errors = new List<string>();

        try
        {
            foreach (var userId in bulkAssignDto.UserIds)
            {
                var assignmentDto = new AssignWarehouseDto
                {
                    WarehouseId = bulkAssignDto.WarehouseId,
                    AccessLevel = bulkAssignDto.AccessLevel,
                    IsDefault = bulkAssignDto.SetAsDefault
                };

                var result = await AssignWarehouseToUserAsync(userId, assignmentDto, cancellationToken);
                if (result.Success && result.Data != null)
                {
                    results.Add(result.Data);
                }
                else
                {
                    errors.Add($"User {userId}: {result.Error}");
                }
            }

            return (errors.Count == 0, results, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk assignment to warehouse {WarehouseId}", bulkAssignDto.WarehouseId);
            errors.Add("Bulk assignment operation failed");
            return (false, results, errors);
        }
    }

    public async Task<(bool HasAccess, string? AccessLevel)> CheckWarehouseAccessAsync(
        string userId, int warehouseId, string? requiredAccessLevel = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user is admin (admins have access to all warehouses)
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && user.Role == "Admin")
            {
                return (true, "Full");
            }

            var assignment = await _context.UserWarehouses
                .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == warehouseId, cancellationToken);

            if (assignment == null)
            {
                return (false, null);
            }

            // Check if specific access level is required
            if (!string.IsNullOrEmpty(requiredAccessLevel))
            {
                var hasRequiredAccess = requiredAccessLevel switch
                {
                    "Full" => assignment.AccessLevel == "Full",
                    "ReadOnly" => assignment.AccessLevel is "Full" or "ReadOnly",
                    "Limited" => assignment.AccessLevel is "Full" or "ReadOnly" or "Limited",
                    _ => false
                };

                return (hasRequiredAccess, assignment.AccessLevel);
            }

            return (true, assignment.AccessLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking warehouse {WarehouseId} access for user {UserId}", 
                warehouseId, userId);
            return (false, null);
        }
    }

    public async Task<List<int>> GetAccessibleWarehouseIdsAsync(string userId, string userRole, CancellationToken cancellationToken = default)
    {
        try
        {
            // Admins have access to all warehouses
            if (userRole == "Admin")
            {
                return await _context.Warehouses
                    .Where(w => w.IsActive)
                    .Select(w => w.Id)
                    .ToListAsync(cancellationToken);
            }

            // Regular users only have access to assigned warehouses
            return await _context.UserWarehouses
                .Where(uw => uw.UserId == userId)
                .Include(uw => uw.Warehouse)
                .Where(uw => uw.Warehouse.IsActive)
                .Select(uw => uw.WarehouseId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accessible warehouses for user {UserId}", userId);
            return new List<int>();
        }
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateAssignmentAsync(
        string userId, int warehouseId, bool isDefault = false, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        try
        {
            // Check if user exists
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                errors.Add("User not found");
            }

            // Check if warehouse exists and is active
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == warehouseId, cancellationToken);
            if (warehouse == null)
            {
                errors.Add("Warehouse not found");
            }
            else if (!warehouse.IsActive)
            {
                errors.Add("Cannot assign user to inactive warehouse");
            }

            return (errors.Count == 0, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating assignment for user {UserId} and warehouse {WarehouseId}", 
                userId, warehouseId);
            errors.Add("Validation failed");
            return (false, errors);
        }
    }

    #region Private Helper Methods

    private async Task ClearPreviousDefaultAsync(string userId, CancellationToken cancellationToken = default)
    {
        var previousDefault = await _context.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.IsDefault, cancellationToken);

        if (previousDefault != null)
        {
            previousDefault.IsDefault = false;
            previousDefault.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<UserWarehouseDto?> GetUserWarehouseAssignmentAsync(string userId, int warehouseId, CancellationToken cancellationToken = default)
    {
        return await _context.UserWarehouses
            .Where(uw => uw.UserId == userId && uw.WarehouseId == warehouseId)
            .Include(uw => uw.Warehouse)
            .Select(uw => new UserWarehouseDto
            {
                UserId = uw.UserId,
                WarehouseId = uw.WarehouseId,
                WarehouseName = uw.Warehouse.Name,
                IsDefault = uw.IsDefault,
                AccessLevel = uw.AccessLevel,
                AssignedAt = uw.AssignedAt,
                CreatedAt = uw.CreatedAt,
                UpdatedAt = uw.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    #endregion
}