using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Web API service for UserWarehouse management operations
/// </summary>
public class WebUserWarehouseApiService : WebBaseApiService, IUserWarehouseService
{
    public WebUserWarehouseApiService(
        HttpClient httpClient,
        IUrlBuilderService urlBuilderService,
        IResilientApiService resilientApiService,
        IApiErrorHandler errorHandler,
        IRequestValidator requestValidator,
        IAutoTokenRefreshService autoTokenRefreshService,
        ILogger<WebUserWarehouseApiService> logger)
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, autoTokenRefreshService, logger)
    {
    }

    public async Task<ApiResponse<List<UserWarehouseDto>>?> GetUserWarehousesAsync(string userId)
    {
        try
        {
            var endpoint = $"/api/user/{userId}/warehouses";
            return await GetAsync<List<UserWarehouseDto>>(endpoint);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting warehouses for user {UserId}", userId);
            return new ApiResponse<List<UserWarehouseDto>>
            {
                Success = false,
                ErrorMessage = "Failed to get user warehouses"
            };
        }
    }

    public async Task<ApiResponse<List<UserWarehouseDto>>?> GetWarehouseUsersAsync(int warehouseId)
    {
        try
        {
            var endpoint = $"/api/warehouse/{warehouseId}/users";
            return await GetAsync<List<UserWarehouseDto>>(endpoint);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting users for warehouse {WarehouseId}", warehouseId);
            return new ApiResponse<List<UserWarehouseDto>>
            {
                Success = false,
                ErrorMessage = "Failed to get warehouse users"
            };
        }
    }

    public async Task<ApiResponse<UserWarehouseDto>?> AssignWarehouseToUserAsync(string userId, AssignWarehouseDto assignmentDto)
    {
        try
        {
            var endpoint = $"/api/user/{userId}/warehouses";
            return await PostAsync<UserWarehouseDto>(endpoint, assignmentDto);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error assigning warehouse to user {UserId}", userId);
            return new ApiResponse<UserWarehouseDto>
            {
                Success = false,
                ErrorMessage = "Failed to assign warehouse to user"
            };
        }
    }

    public async Task<ApiResponse<object>?> RemoveWarehouseAssignmentAsync(string userId, int warehouseId)
    {
        try
        {
            var endpoint = $"/api/user/{userId}/warehouses/{warehouseId}";
            var response = await DeleteAsync(endpoint);
            return new ApiResponse<object>
            {
                Success = response.Success,
                ErrorMessage = response.ErrorMessage,
                Data = response.Success ? new { message = "Warehouse assignment removed successfully" } : null
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing warehouse {WarehouseId} assignment from user {UserId}", warehouseId, userId);
            return new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to remove warehouse assignment"
            };
        }
    }

    public async Task<ApiResponse<UserWarehouseDto>?> UpdateWarehouseAssignmentAsync(string userId, int warehouseId, UpdateWarehouseAssignmentDto updateDto)
    {
        try
        {
            var endpoint = $"/api/user/{userId}/warehouses/{warehouseId}";
            return await PutAsync<UserWarehouseDto>(endpoint, updateDto);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating warehouse {WarehouseId} assignment for user {UserId}", warehouseId, userId);
            return new ApiResponse<UserWarehouseDto>
            {
                Success = false,
                ErrorMessage = "Failed to update warehouse assignment"
            };
        }
    }

    public async Task<ApiResponse<object>?> SetDefaultWarehouseAsync(string userId, int warehouseId)
    {
        try
        {
            var endpoint = $"/api/user/{userId}/warehouses/{warehouseId}/default";
            return await PutAsync<object>(endpoint, new { });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting default warehouse {WarehouseId} for user {UserId}", warehouseId, userId);
            return new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to set default warehouse"
            };
        }
    }

    public async Task<ApiResponse<object>?> BulkAssignUsersToWarehouseAsync(BulkAssignWarehousesDto bulkAssignDto)
    {
        try
        {
            var endpoint = "/api/warehouse/bulk-assign-users";
            return await PostAsync<object>(endpoint, bulkAssignDto);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in bulk assignment to warehouse {WarehouseId}", bulkAssignDto.WarehouseId);
            return new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to bulk assign users to warehouse"
            };
        }
    }

    public async Task<ApiResponse<object>?> CheckWarehouseAccessAsync(string userId, int warehouseId, string? requiredAccessLevel = null)
    {
        try
        {
            var queryString = !string.IsNullOrEmpty(requiredAccessLevel) ? $"?requiredAccessLevel={Uri.EscapeDataString(requiredAccessLevel)}" : "";
            var endpoint = $"/api/userwarehouse/users/{userId}/warehouses/{warehouseId}/access{queryString}";
            return await GetAsync<object>(endpoint);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking warehouse {WarehouseId} access for user {UserId}", warehouseId, userId);
            return new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to check warehouse access"
            };
        }
    }

    public async Task<ApiResponse<List<int>>?> GetAccessibleWarehousesAsync(string userId)
    {
        try
        {
            var endpoint = $"/api/userwarehouse/users/{userId}/accessible-warehouses";
            return await GetAsync<List<int>>(endpoint);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting accessible warehouses for user {UserId}", userId);
            return new ApiResponse<List<int>>
            {
                Success = false,
                ErrorMessage = "Failed to get accessible warehouses"
            };
        }
    }
}