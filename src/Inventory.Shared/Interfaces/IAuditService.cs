using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IAuditService
{
    Task<ApiResponse<AuditLogResponse>> GetAuditLogsAsync(
        string? actionType = null,
        string? entityType = null,
        string? userName = null,
        string? requestId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        bool? isSuccess = null,
        string? ipAddress = null,
        int page = 1,
        int pageSize = 20);

    Task<ApiResponse<string>> ExportAuditLogsAsync(
        string? actionType = null,
        string? entityType = null,
        string? userName = null,
        string? requestId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        bool? isSuccess = null,
        string? ipAddress = null);

    Task<ApiResponse<List<AuditLogDto>>> GetEntityAuditLogsAsync(string entityType, string entityId);
    Task<ApiResponse<List<AuditLogDto>>> GetUserAuditLogsAsync(string userId, int page = 1, int pageSize = 20);
    Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsByRequestIdAsync(string requestId);
    Task<ApiResponse<AuditLogDto>> GetAuditLogAsync(int logId);
}
