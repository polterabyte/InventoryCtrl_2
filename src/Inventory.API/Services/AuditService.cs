using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Inventory.API.Models;
using Inventory.API.Enums;

namespace Inventory.API.Services;

/// <summary>
/// Service for managing audit logs
/// </summary>
public class AuditService(
    AppDbContext context,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditService> logger,
    SafeSerializationService safeSerializer) : IInternalAuditService
{
    private readonly AppDbContext _context = context;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<AuditService> _logger = logger;
    private readonly SafeSerializationService _safeSerializer = safeSerializer;

    /// <summary>
    /// Logs an audit entry for entity changes
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="action">Action performed</param>
    /// <param name="actionType">Type of action (enum)</param>
    /// <param name="entityType">Type of entity</param>
    /// <param name="oldValues">Old values before change</param>
    /// <param name="newValues">New values after change</param>
    /// <param name="description">Additional description</param>
    /// <param name="requestId">Request ID for tracing</param>
    /// <param name="severity">Severity level</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if failed</param>
    public async Task LogEntityChangeAsync(
        string entityName,
        string entityId,
        string action,
        ActionType actionType,
        string entityType,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        string? requestId = null,
        string severity = "INFO",
        bool isSuccess = true,
        string? errorMessage = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var httpContext = _httpContextAccessor.HttpContext;

            // Skip audit logging if no authenticated user
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogDebug("Skipping audit log for {Action} on {EntityName} {EntityId} - no authenticated user",
                    action, entityName, entityId);
                return;
            }

            // Verify user exists in database and get correct userId
            var correctUserId = await VerifyAndGetUserIdAsync(userId);
            if (correctUserId == null)
            {
                _logger.LogWarning("Skipping audit log for {Action} on {EntityName} {EntityId} - user {UserId} not found in database",
                    action, entityName, entityId, userId);
                return;
            }
            userId = correctUserId;

            // Create changes JSON if both old and new values are provided
            var changesJson = AuditLog.CreateChangesJson(
                oldValues != null ? _safeSerializer.SafeSerialize(oldValues) : null,
                newValues != null ? _safeSerializer.SafeSerialize(newValues) : null
            );

            var auditLog = new AuditLog(
                entityName,
                entityId,
                action,
                actionType,
                entityType,
                userId,
                changesJson,
                requestId,
                oldValues != null ? _safeSerializer.SafeSerialize(oldValues) : null,
                newValues != null ? _safeSerializer.SafeSerialize(newValues) : null,
                description)
            {
                Username = username,
                Severity = severity,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                IpAddress = GetClientIpAddress(),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty,
                HttpMethod = httpContext?.Request.Method,
                Url = httpContext?.Request.Path.Value,
                StatusCode = httpContext?.Response.StatusCode
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log created for {Action} on {EntityName} {EntityId} by user {UserId}",
                action, entityName, entityId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for {Action} on {EntityName} {EntityId}",
                action, entityName, entityId);
        }
    }

    /// <summary>
    /// Logs an audit entry for entity changes (backward compatibility)
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="action">Action performed</param>
    /// <param name="oldValues">Old values before change</param>
    /// <param name="newValues">New values after change</param>
    /// <param name="description">Additional description</param>
    /// <param name="severity">Severity level</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if failed</param>
    public async Task LogEntityChangeAsync(
        string entityName,
        string entityId,
        string action,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        string severity = "INFO",
        bool isSuccess = true,
        string? errorMessage = null)
    {
        // Determine action type from string
        var actionType = action.ToUpperInvariant() switch
        {
            "CREATE" or "POST" => ActionType.Create,
            "READ" or "GET" => ActionType.Read,
            "UPDATE" or "PUT" or "PATCH" => ActionType.Update,
            "DELETE" => ActionType.Delete,
            "LOGIN" => ActionType.Login,
            "LOGOUT" => ActionType.Logout,
            "REFRESH" => ActionType.Refresh,
            "EXPORT" => ActionType.Export,
            "IMPORT" => ActionType.Import,
            "SEARCH" => ActionType.Search,
            _ => ActionType.Other
        };

        await LogEntityChangeAsync(
            entityName,
            entityId,
            action,
            actionType,
            entityName, // Use entityName as entityType for backward compatibility
            oldValues,
            newValues,
            description,
            null, // No requestId for backward compatibility
            severity,
            isSuccess,
            errorMessage);
    }

    /// <summary>
    /// Logs an audit entry for user actions
    /// </summary>
    /// <param name="action">Action performed</param>
    /// <param name="description">Description of the action</param>
    /// <param name="entityName">Optional entity name</param>
    /// <param name="entityId">Optional entity ID</param>
    /// <param name="metadata">Additional metadata</param>
    /// <param name="severity">Severity level</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if failed</param>
    public async Task LogUserActionAsync(
        string action,
        string? description = null,
        string? entityName = null,
        string? entityId = null,
        object? metadata = null,
        string severity = "INFO",
        bool isSuccess = true,
        string? errorMessage = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog
            {
                EntityName = entityName ?? "User",
                EntityId = entityId ?? userId,
                Action = action,
                UserId = userId,
                Username = username,
                Description = description,
                Metadata = metadata != null ? _safeSerializer.SafeSerialize(metadata) : null,
                Severity = severity,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetClientIpAddress(),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty,
                HttpMethod = httpContext?.Request.Method,
                Url = httpContext?.Request.Path.Value,
                StatusCode = httpContext?.Response.StatusCode
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User action logged: {Action} by user {UserId}",
                action, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user action: {Action}", action);
        }
    }

    /// <summary>
    /// Logs an audit entry for HTTP requests
    /// </summary>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="url">Request URL</param>
    /// <param name="statusCode">Response status code</param>
    /// <param name="duration">Request duration in milliseconds</param>
    /// <param name="description">Additional description</param>
    /// <param name="isSuccess">Whether the request was successful</param>
    /// <param name="errorMessage">Error message if failed</param>
    public async Task LogHttpRequestAsync(
        string httpMethod,
        string url,
        int statusCode,
        long duration,
        string? description = null,
        bool isSuccess = true,
        string? errorMessage = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog
            {
                EntityName = "HTTP",
                EntityId = Guid.NewGuid().ToString(),
                Action = "REQUEST",
                UserId = userId,
                Username = username,
                Description = description,
                Severity = statusCode >= 400 ? "ERROR" : "INFO",
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetClientIpAddress(),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty,
                HttpMethod = httpMethod,
                Url = url,
                StatusCode = statusCode,
                Duration = duration
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("HTTP request logged: {Method} {Url} - {StatusCode} ({Duration}ms)",
                httpMethod, url, statusCode, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log HTTP request: {Method} {Url}", httpMethod, url);
        }
    }

    /// <summary>
    /// Logs an audit entry with detailed change tracking
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="action">Action performed</param>
    /// <param name="actionType">Type of action (enum)</param>
    /// <param name="entityType">Type of entity</param>
    /// <param name="changes">Detailed changes object</param>
    /// <param name="requestId">Request ID for tracing</param>
    /// <param name="description">Additional description</param>
    /// <param name="severity">Severity level</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if failed</param>
    public async Task LogDetailedChangeAsync(
        string entityName,
        string entityId,
        string action,
        ActionType actionType,
        string entityType,
        object? changes = null,
        string? requestId = null,
        string? description = null,
        string severity = "INFO",
        bool isSuccess = true,
        string? errorMessage = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var httpContext = _httpContextAccessor.HttpContext;

            // Skip audit logging if no authenticated user
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogDebug("Skipping audit log for {Action} on {EntityName} {EntityId} - no authenticated user",
                    action, entityName, entityId);
                return;
            }

            // Verify user exists in database and get correct userId
            var correctUserId = await VerifyAndGetUserIdAsync(userId);
            if (correctUserId == null)
            {
                _logger.LogWarning("Skipping audit log for {Action} on {EntityName} {EntityId} - user {UserId} not found in database",
                    action, entityName, entityId, userId);
                return;
            }
            userId = correctUserId;

            var changesJson = changes != null ? _safeSerializer.SafeSerialize(changes) : null;

            var auditLog = new AuditLog(
                entityName,
                entityId,
                action,
                actionType,
                entityType,
                userId,
                changesJson,
                requestId,
                null, // No old values for detailed changes
                null, // No new values for detailed changes
                description)
            {
                Username = username,
                Severity = severity,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                IpAddress = GetClientIpAddress(),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty,
                HttpMethod = httpContext?.Request.Method,
                Url = httpContext?.Request.Path.Value,
                StatusCode = httpContext?.Response.StatusCode
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Detailed audit log created for {Action} on {EntityName} {EntityId} by user {UserId}",
                action, entityName, entityId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create detailed audit log for {Action} on {EntityName} {EntityId}",
                action, entityName, entityId);
        }
    }

    /// <summary>
    /// Gets audit logs with filtering and pagination
    /// </summary>
    /// <param name="entityName">Filter by entity name</param>
    /// <param name="action">Filter by action</param>
    /// <param name="actionType">Filter by action type</param>
    /// <param name="entityType">Filter by entity type</param>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="startDate">Filter by start date</param>
    /// <param name="endDate">Filter by end date</param>
    /// <param name="severity">Filter by severity</param>
    /// <param name="requestId">Filter by request ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated audit logs</returns>
    public async Task<(List<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        string? entityName = null,
        string? action = null,
        ActionType? actionType = null,
        string? entityType = null,
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null,
        string? requestId = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(entityName))
            query = query.Where(a => a.EntityName.Contains(entityName));

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action.Contains(action));

        if (actionType.HasValue)
            query = query.Where(a => a.ActionType == actionType.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType.Contains(entityType));

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(a => a.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(a => a.Severity == severity);

        if (!string.IsNullOrEmpty(requestId))
            query = query.Where(a => a.RequestId == requestId);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    /// <summary>
    /// Gets audit logs with filtering and pagination (backward compatibility)
    /// </summary>
    /// <param name="entityName">Filter by entity name</param>
    /// <param name="action">Filter by action</param>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="startDate">Filter by start date</param>
    /// <param name="endDate">Filter by end date</param>
    /// <param name="severity">Filter by severity</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated audit logs</returns>
    public async Task<(List<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        string? entityName = null,
        string? action = null,
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null,
        int page = 1,
        int pageSize = 50)
    {
        return await GetAuditLogsAsync(
            entityName,
            action,
            null, // No actionType filter
            null, // No entityType filter
            userId,
            startDate,
            endDate,
            severity,
            null, // No requestId filter
            page,
            pageSize);
    }

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    /// <param name="entityName">Entity name</param>
    /// <param name="entityId">Entity ID</param>
    /// <returns>Audit logs for the entity</returns>
    public async Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityName, string entityId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Number of days to look back</param>
    /// <returns>Audit logs for the user</returns>
    public async Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.UserId == userId && a.Timestamp >= startDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Cleans up old audit logs
    /// </summary>
    /// <param name="daysToKeep">Number of days to keep logs</param>
    /// <returns>Number of logs deleted</returns>
    public async Task<int> CleanupOldLogsAsync(int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var oldLogs = await _context.AuditLogs
            .Where(a => a.Timestamp < cutoffDate)
            .ToListAsync();

        if (oldLogs.Any())
        {
            _context.AuditLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} old audit logs older than {Days} days",
                oldLogs.Count, daysToKeep);
        }

        return oldLogs.Count;
    }

    private string GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // If no authenticated user, return null instead of "Anonymous"
        // This will be handled in the calling methods
        return userId ?? string.Empty;
    }

    private string? GetCurrentUsername()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
    }

    private string GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return "Unknown";

        // Check for forwarded IP first
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Verifies user exists in database and returns correct userId
    /// </summary>
    private async Task<string?> VerifyAndGetUserIdAsync(string userId)
    {
        // First try to find by ID
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (userExists)
        {
            return userId;
        }

        // Try to find by username if ID lookup failed
        var userByUsername = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userId);
        if (userByUsername != null)
        {
            _logger.LogDebug("Found user by username, updated userId from {OldId} to {NewId}", 
                userId, userByUsername.Id);
            return userByUsername.Id;
        }

        return null;
    }
}
