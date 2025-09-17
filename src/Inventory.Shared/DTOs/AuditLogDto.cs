using Inventory.Shared.Enums;

namespace Inventory.Shared.DTOs;

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
    public string? SessionId { get; set; }
    public string? AdditionalData { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int ResponseTime { get; set; }
    public string? Endpoint { get; set; }
    public string? HttpMethod { get; set; }
    public int? StatusCode { get; set; }
}

/// <summary>
/// Filters for audit log queries
/// </summary>
public class AuditLogFilters
{
    public string? ActionType { get; set; }
    public string? EntityType { get; set; }
    public string? UserName { get; set; }
    public string? RequestId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? IsSuccess { get; set; }
    public string? IpAddress { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Response wrapper for audit log queries
/// </summary>
public class AuditLogResponse
{
    public List<AuditLogDto> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
