using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.API.Services;
using Inventory.API.Models;
using Inventory.API.Enums;
using Microsoft.AspNetCore.RateLimiting;
using Inventory.Shared.DTOs;

namespace Inventory.API.Controllers;

/// <summary>
/// Controller for managing audit logs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
[EnableRateLimiting("ApiPolicy")]
public class AuditController(AuditService auditService, ILogger<AuditController> logger) : ControllerBase
{
    private readonly AuditService _auditService = auditService;
    private readonly ILogger<AuditController> _logger = logger;

    /// <summary>
    /// Gets audit logs with filtering and pagination
    /// </summary>
    /// <param name="entityName">Filter by entity name</param>
    /// <param name="action">Filter by action</param>
    /// <param name="actionType">Filter by action type</param>
    /// <param name="entityType">Filter by entity type</param>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="userName">Filter by user name</param>
    /// <param name="startDate">Filter by start date</param>
    /// <param name="endDate">Filter by end date</param>
    /// <param name="dateFrom">Filter by date from (alternative to startDate)</param>
    /// <param name="dateTo">Filter by date to (alternative to endDate)</param>
    /// <param name="severity">Filter by severity</param>
    /// <param name="requestId">Filter by request ID</param>
    /// <param name="isSuccess">Filter by success status</param>
    /// <param name="ipAddress">Filter by IP address</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated audit logs</returns>
    [HttpGet]
        public async Task<ActionResult<PagedApiResponse<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? entityName = null,
        [FromQuery] string? action = null,
        [FromQuery] ActionType? actionType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? userName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? requestId = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate page size
            if (pageSize > 100)
                pageSize = 100;

            if (page < 1)
                page = 1;

            // Use dateFrom/dateTo if provided, otherwise fall back to startDate/endDate
            var fromDate = dateFrom ?? startDate;
            var toDate = dateTo ?? endDate;
            
            // Use userName if provided, otherwise fall back to userId
            var userFilter = !string.IsNullOrEmpty(userName) ? userName : userId;

            var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
                entityName, action, actionType, entityType, userFilter, fromDate, toDate, severity, requestId, page, pageSize);

            var pagedResponse = new PagedResponse<AuditLogDto>
            {
                Items = logs.Select(MapToDto).ToList(),
                total = totalCount,
                page = page,
                PageSize = pageSize
            };

            return Ok(PagedApiResponse<AuditLogDto>.CreateSuccess(pagedResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, PagedApiResponse<AuditLogDto>.CreateFailure("An error occurred while retrieving audit logs"));
        }
    }

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    /// <param name="entityName">Entity name</param>
    /// <param name="entityId">Entity ID</param>
    /// <returns>Audit logs for the entity</returns>
    [HttpGet("entity/{entityName}/{entityId}")]
    public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetEntityAuditLogs(
        string entityName, 
        string entityId)
    {
        try
        {
            var logs = await _auditService.GetEntityAuditLogsAsync(entityName, entityId);
            return Ok(ApiResponse<List<AuditLogDto>>.SuccessResult(logs.Select(MapToDto).ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for entity {EntityName} {EntityId}", 
                entityName, entityId);
            return StatusCode(500, ApiResponse<List<AuditLogDto>>.ErrorResult("An error occurred while retrieving entity audit logs"));
        }
    }

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <returns>Audit logs for the user</returns>
    [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetUserAuditLogs(
        string userId, 
        [FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 365)
                return BadRequest(ApiResponse<List<AuditLogDto>>.ErrorResult("Days must be between 1 and 365"));

            var logs = await _auditService.GetUserAuditLogsAsync(userId, days);
            return Ok(ApiResponse<List<AuditLogDto>>.SuccessResult(logs.Select(MapToDto).ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
            return StatusCode(500, ApiResponse<List<AuditLogDto>>.ErrorResult("An error occurred while retrieving user audit logs"));
        }
    }

    /// <summary>
    /// Gets audit log statistics
    /// </summary>
    /// <param name="days">Number of days to analyze (default: 30)</param>
    /// <returns>Audit log statistics</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<AuditStatisticsDto>>> GetAuditStatistics([FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 365)
                return BadRequest(ApiResponse<AuditStatisticsDto>.ErrorResult("Days must be between 1 and 365"));

            var startDate = DateTime.UtcNow.AddDays(-days);
            var (logs, _) = await _auditService.GetAuditLogsAsync(
                entityName: null,
                action: null,
                actionType: null,
                entityType: null,
                userId: null,
                startDate: startDate,
                endDate: null,
                severity: null,
                requestId: null,
                page: 1,
                pageSize: int.MaxValue);

            var statistics = new AuditStatisticsDto
            {
                TotalLogs = logs.Count,
                SuccessfulLogs = logs.Count(l => l.IsSuccess),
                FailedLogs = logs.Count(l => !l.IsSuccess),
                LogsByAction = logs.GroupBy(l => l.Action)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LogsByEntity = logs.GroupBy(l => l.EntityName)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LogsBySeverity = logs.GroupBy(l => l.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LogsByUser = logs.GroupBy(l => l.Username ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageResponseTime = logs.Where(l => l.Duration.HasValue)
                    .Average(l => l.Duration!.Value),
                TopErrors = logs.Where(l => !l.IsSuccess && !string.IsNullOrEmpty(l.ErrorMessage))
                    .GroupBy(l => l.ErrorMessage!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(ApiResponse<AuditStatisticsDto>.SuccessResult(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit statistics");
            return StatusCode(500, ApiResponse<AuditStatisticsDto>.ErrorResult("An error occurred while retrieving audit statistics"));
        }
    }

    /// <summary>
    /// Gets audit logs by action type
    /// </summary>
    /// <param name="actionType">Action type to filter by</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated audit logs by action type</returns>
    [HttpGet("by-action-type/{actionType}")]
        public async Task<ActionResult<PagedApiResponse<AuditLogDto>>> GetAuditLogsByActionType(
        ActionType actionType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (pageSize > 100)
                pageSize = 100;

            if (page < 1)
                page = 1;

            var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
                actionType: actionType, page: page, pageSize: pageSize);

            var pagedResponse = new PagedResponse<AuditLogDto>
            {
                Items = logs.Select(MapToDto).ToList(),
                total = totalCount,
                page = page,
                PageSize = pageSize
            };

            return Ok(PagedApiResponse<AuditLogDto>.CreateSuccess(pagedResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs by action type {ActionType}", actionType);
            return StatusCode(500, PagedApiResponse<AuditLogDto>.CreateFailure("An error occurred while retrieving audit logs by action type"));
        }
    }

    /// <summary>
    /// Gets audit logs by entity type
    /// </summary>
    /// <param name="entityType">Entity type to filter by</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated audit logs by entity type</returns>
    [HttpGet("by-entity-type/{entityType}")]
        public async Task<ActionResult<PagedApiResponse<AuditLogDto>>> GetAuditLogsByEntityType(
        string entityType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (pageSize > 100)
                pageSize = 100;

            if (page < 1)
                page = 1;

            var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
                entityType: entityType, page: page, pageSize: pageSize);

            var pagedResponse = new PagedResponse<AuditLogDto>
            {
                Items = logs.Select(MapToDto).ToList(),
                total = totalCount,
                page = page,
                PageSize = pageSize
            };

            return Ok(PagedApiResponse<AuditLogDto>.CreateSuccess(pagedResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs by entity type {EntityType}", entityType);
            return StatusCode(500, PagedApiResponse<AuditLogDto>.CreateFailure("An error occurred while retrieving audit logs by entity type"));
        }
    }

    /// <summary>
    /// Gets audit logs by request ID for tracing
    /// </summary>
    /// <param name="requestId">Request ID to trace</param>
    /// <returns>Audit logs for the request</returns>
    [HttpGet("trace/{requestId}")]
        public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetAuditLogsByRequestId(string requestId)
    {
        try
        {
            var (logs, _) = await _auditService.GetAuditLogsAsync(requestId: requestId, pageSize: int.MaxValue);
            return Ok(ApiResponse<List<AuditLogDto>>.SuccessResult(logs.Select(MapToDto).ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for request {RequestId}", requestId);
            return StatusCode(500, ApiResponse<List<AuditLogDto>>.ErrorResult("An error occurred while retrieving audit logs for request"));
        }
    }

    /// <summary>
    /// Exports audit logs to CSV format
    /// </summary>
    /// <param name="entityName">Filter by entity name</param>
    /// <param name="action">Filter by action</param>
    /// <param name="actionType">Filter by action type</param>
    /// <param name="entityType">Filter by entity type</param>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="userName">Filter by user name</param>
    /// <param name="startDate">Filter by start date</param>
    /// <param name="endDate">Filter by end date</param>
    /// <param name="dateFrom">Filter by date from</param>
    /// <param name="dateTo">Filter by date to</param>
    /// <param name="severity">Filter by severity</param>
    /// <param name="requestId">Filter by request ID</param>
    /// <param name="isSuccess">Filter by success status</param>
    /// <param name="ipAddress">Filter by IP address</param>
    /// <returns>CSV content</returns>
    [HttpGet("export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string? entityName = null,
        [FromQuery] string? action = null,
        [FromQuery] ActionType? actionType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? userName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? requestId = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] string? ipAddress = null)
    {
        try
        {
            // Use dateFrom/dateTo if provided, otherwise fall back to startDate/endDate
            var fromDate = dateFrom ?? startDate;
            var toDate = dateTo ?? endDate;
            
            // Use userName if provided, otherwise fall back to userId
            var userFilter = !string.IsNullOrEmpty(userName) ? userName : userId;

            var (logs, _) = await _auditService.GetAuditLogsAsync(
                entityName, action, actionType, entityType, userFilter, fromDate, toDate, severity, requestId, 1, int.MaxValue);

            // Generate CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,Timestamp,Action,ActionType,EntityType,EntityId,EntityName,Username,UserId,IpAddress,UserAgent,HttpMethod,Url,StatusCode,Duration,IsSuccess,ErrorMessage,Severity,RequestId,Changes,OldValues,NewValues,Description,Metadata");
            
            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Id},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{EscapeCsv(log.Action)},{log.ActionType},{EscapeCsv(log.EntityType)},{EscapeCsv(log.EntityId)},{EscapeCsv(log.EntityName)},{EscapeCsv(log.Username)},{EscapeCsv(log.UserId)},{EscapeCsv(log.IpAddress)},{EscapeCsv(log.UserAgent)},{EscapeCsv(log.HttpMethod)},{EscapeCsv(log.Url)},{log.StatusCode},{log.Duration},{log.IsSuccess},{EscapeCsv(log.ErrorMessage)},{EscapeCsv(log.Severity)},{EscapeCsv(log.RequestId)},{EscapeCsv(log.Changes)},{EscapeCsv(log.OldValues)},{EscapeCsv(log.NewValues)},{EscapeCsv(log.Description)},{EscapeCsv(log.Metadata)}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            var response = ApiResponse<string>.ErrorResult("An error occurred while exporting audit logs");
            return StatusCode(500, response);
        }
    }

    /// <summary>
    /// Cleans up old audit logs
    /// </summary>
    /// <param name="daysToKeep">Number of days to keep logs (default: 90)</param>
    /// <returns>Number of logs deleted</returns>
    [HttpDelete("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CleanupResultDto>>> CleanupOldLogs([FromQuery] int daysToKeep = 90)
    {
        try
        {
            if (daysToKeep < 30)
                return BadRequest(ApiResponse<CleanupResultDto>.ErrorResult("Days to keep must be at least 30"));

            var deletedCount = await _auditService.CleanupOldLogsAsync(daysToKeep);
            
            var result = new CleanupResultDto
            {
                DeletedCount = deletedCount,
                DaysToKeep = daysToKeep,
                CleanupDate = DateTime.UtcNow
            };

            return Ok(ApiResponse<CleanupResultDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old audit logs");
            return StatusCode(500, ApiResponse<CleanupResultDto>.ErrorResult("An error occurred while cleaning up audit logs"));
        }
    }

    private static AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            Action = log.Action,
            ActionType = log.ActionType,
            EntityType = log.EntityType,
            Changes = log.Changes,
            RequestId = log.RequestId,
            UserId = log.UserId,
            Username = log.Username,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            Description = log.Description,
            Timestamp = log.Timestamp,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            HttpMethod = log.HttpMethod,
            Url = log.Url,
            StatusCode = log.StatusCode,
            Duration = log.Duration,
            Metadata = log.Metadata,
            Severity = log.Severity,
            IsSuccess = log.IsSuccess,
            ErrorMessage = log.ErrorMessage
        };
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
            
        // Escape quotes and wrap in quotes if contains comma, quote, or newline
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        
        return value;
    }
}

/// <summary>
/// Response DTO for paginated audit logs
/// </summary>
public class AuditLogResponse
{
    public List<AuditLogDto> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// DTO for audit log data
/// </summary>
public class AuditLogDto
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? Changes { get; set; }
    public string? RequestId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public int? StatusCode { get; set; }
    public long? Duration { get; set; }
    public string? Metadata { get; set; }
    public string Severity { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for audit statistics
/// </summary>
public class AuditStatisticsDto
{
    public int TotalLogs { get; set; }
    public int SuccessfulLogs { get; set; }
    public int FailedLogs { get; set; }
    public Dictionary<string, int> LogsByAction { get; set; } = new();
    public Dictionary<string, int> LogsByEntity { get; set; } = new();
    public Dictionary<string, int> LogsBySeverity { get; set; } = new();
    public Dictionary<string, int> LogsByUser { get; set; } = new();
    public double AverageResponseTime { get; set; }
    public Dictionary<string, int> TopErrors { get; set; } = new();
}

/// <summary>
/// DTO for cleanup result
/// </summary>
public class CleanupResultDto
{
    public int DeletedCount { get; set; }
    public int DaysToKeep { get; set; }
    public DateTime CleanupDate { get; set; }
}
