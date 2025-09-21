using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Inventory.Shared.Services;

public class AuditApiService(HttpClient httpClient, ILogger<AuditApiService> logger) : BaseApiService(httpClient, "https://staging.warehouse.cuby/api", logger), IAuditService
{

    public async Task<ApiResponse<AuditLogResponse>> GetAuditLogsAsync(
        string? actionType = null,
        string? entityType = null,
        string? userName = null,
        string? requestId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        bool? isSuccess = null,
        string? ipAddress = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(actionType)) queryParams.Add($"actionType={Uri.EscapeDataString(actionType)}");
            if (!string.IsNullOrEmpty(entityType)) queryParams.Add($"entityType={Uri.EscapeDataString(entityType)}");
            if (!string.IsNullOrEmpty(userName)) queryParams.Add($"userName={Uri.EscapeDataString(userName)}");
            if (!string.IsNullOrEmpty(requestId)) queryParams.Add($"requestId={Uri.EscapeDataString(requestId)}");
            if (dateFrom.HasValue) queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (dateTo.HasValue) queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (isSuccess.HasValue) queryParams.Add($"isSuccess={isSuccess.Value}");
            if (!string.IsNullOrEmpty(ipAddress)) queryParams.Add($"ipAddress={Uri.EscapeDataString(ipAddress)}");
            
            queryParams.Add($"page={page}");
            queryParams.Add($"pageSize={pageSize}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            return await GetAsync<AuditLogResponse>($"audit{queryString}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting audit logs");
            return new ApiResponse<AuditLogResponse>
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving audit logs"
            };
        }
    }

    public async Task<ApiResponse<string>> ExportAuditLogsAsync(
        string? actionType = null,
        string? entityType = null,
        string? userName = null,
        string? requestId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        bool? isSuccess = null,
        string? ipAddress = null)
    {
        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(actionType)) queryParams.Add($"actionType={Uri.EscapeDataString(actionType)}");
            if (!string.IsNullOrEmpty(entityType)) queryParams.Add($"entityType={Uri.EscapeDataString(entityType)}");
            if (!string.IsNullOrEmpty(userName)) queryParams.Add($"userName={Uri.EscapeDataString(userName)}");
            if (!string.IsNullOrEmpty(requestId)) queryParams.Add($"requestId={Uri.EscapeDataString(requestId)}");
            if (dateFrom.HasValue) queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (dateTo.HasValue) queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (isSuccess.HasValue) queryParams.Add($"isSuccess={isSuccess.Value}");
            if (!string.IsNullOrEmpty(ipAddress)) queryParams.Add($"ipAddress={Uri.EscapeDataString(ipAddress)}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await GetAsync<string>($"audit/export{queryString}");
            
            if (response.Success && response.Data != null)
            {
                return response;
            }
            
            return new ApiResponse<string>
            {
                Success = false,
                ErrorMessage = response.ErrorMessage ?? "Failed to export audit logs"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting audit logs");
            return new ApiResponse<string>
            {
                Success = false,
                ErrorMessage = "An error occurred while exporting audit logs"
            };
        }
    }

    public async Task<ApiResponse<List<AuditLogDto>>> GetEntityAuditLogsAsync(string entityType, string entityId)
    {
        try
        {
            return await GetAsync<List<AuditLogDto>>($"audit/entity/{Uri.EscapeDataString(entityType)}/{Uri.EscapeDataString(entityId)}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting entity audit logs for {EntityType} {EntityId}", entityType, entityId);
            return new ApiResponse<List<AuditLogDto>>
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving entity audit logs"
            };
        }
    }

    public async Task<ApiResponse<List<AuditLogDto>>> GetUserAuditLogsAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            return await GetAsync<List<AuditLogDto>>($"audit/user/{Uri.EscapeDataString(userId)}?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting user audit logs for {UserId}", userId);
            return new ApiResponse<List<AuditLogDto>>
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving user audit logs"
            };
        }
    }

    public async Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsByRequestIdAsync(string requestId)
    {
        try
        {
            return await GetAsync<List<AuditLogDto>>($"audit/trace/{Uri.EscapeDataString(requestId)}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting audit logs for request {RequestId}", requestId);
            return new ApiResponse<List<AuditLogDto>>
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving request audit logs"
            };
        }
    }

    public async Task<ApiResponse<AuditLogDto>> GetAuditLogAsync(int logId)
    {
        try
        {
            return await GetAsync<AuditLogDto>($"audit/{logId}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting audit log {LogId}", logId);
            return new ApiResponse<AuditLogDto>
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving audit log"
            };
        }
    }
}
