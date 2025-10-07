using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Inventory.API.Enums;

namespace Inventory.API.Models;

/// <summary>
/// Audit log for tracking changes to entities and user actions
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    
    /// <summary>
    /// Name of the entity being audited (e.g., "Product", "User", "Category")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the entity being audited
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// Action performed (CREATE, UPDATE, DELETE, LOGIN, LOGOUT, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of action performed (enum for better categorization)
    /// </summary>
    [Required]
    public ActionType ActionType { get; set; } = ActionType.Other;
    
    /// <summary>
    /// Type of entity being audited (e.g., "Product", "User", "Category")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed changes made (JSON format with before/after values)
    /// </summary>
    public string? Changes { get; set; }
    
    /// <summary>
    /// Request ID for tracing related operations
    /// </summary>
    [MaxLength(100)]
    public string? RequestId { get; set; }
    
    /// <summary>
    /// ID of the user who performed the action
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Username for easier identification
    /// </summary>
    [MaxLength(100)]
    public string? Username { get; set; }
    
    /// <summary>
    /// Old values before the change (JSON format)
    /// </summary>
    public string? OldValues { get; set; }
    
    /// <summary>
    /// New values after the change (JSON format)
    /// </summary>
    public string? NewValues { get; set; }
    
    /// <summary>
    /// Additional description of the action
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// IP address of the client
    /// </summary>
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// User agent string from the client
    /// </summary>
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP method used (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    [MaxLength(10)]
    public string? HttpMethod { get; set; }
    
    /// <summary>
    /// URL/endpoint that was accessed
    /// </summary>
    [MaxLength(500)]
    public string? Url { get; set; }
    
    /// <summary>
    /// HTTP status code returned
    /// </summary>
    public int? StatusCode { get; set; }
    
    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public long? Duration { get; set; }
    
    /// <summary>
    /// Additional metadata (JSON format)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Severity level of the action (INFO, WARNING, ERROR, CRITICAL)
    /// </summary>
    [MaxLength(20)]
    public string Severity { get; set; } = "INFO";
    
    /// <summary>
    /// Whether this action was successful
    /// </summary>
    public bool IsSuccess { get; set; } = true;
    
    /// <summary>
    /// Error message if the action failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Default constructor for Entity Framework
    /// </summary>
    public AuditLog() { }
    
    /// <summary>
    /// Constructor for creating audit log entries
    /// </summary>
    public AuditLog(
        string entityName, 
        string entityId, 
        string action, 
        string userId, 
        string? oldValues = null, 
        string? newValues = null, 
        string? description = null)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        UserId = userId;
        OldValues = oldValues;
        NewValues = newValues;
        Description = description;
        Timestamp = DateTime.UtcNow;
        
        // Set default values for new fields
        EntityType = entityName;
        ActionType = GetActionTypeFromString(action);
    }
    
    /// <summary>
    /// Enhanced constructor with new fields
    /// </summary>
    public AuditLog(
        string entityName,
        string entityId,
        string action,
        ActionType actionType,
        string entityType,
        string userId,
        string? changes = null,
        string? requestId = null,
        string? oldValues = null,
        string? newValues = null,
        string? description = null)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        ActionType = actionType;
        EntityType = entityType;
        UserId = userId;
        Changes = changes;
        RequestId = requestId;
        OldValues = oldValues;
        NewValues = newValues;
        Description = description;
        Timestamp = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Helper method to determine ActionType from string action
    /// </summary>
    private static ActionType GetActionTypeFromString(string action)
    {
        return action.ToUpperInvariant() switch
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
    }
    
    /// <summary>
    /// Helper method to create changes JSON from old and new values
    /// </summary>
    public static string CreateChangesJson(string? oldValues, string? newValues)
    {
        if (string.IsNullOrEmpty(oldValues) && string.IsNullOrEmpty(newValues))
            return string.Empty;
            
        var changes = new
        {
            OldValues = oldValues,
            NewValues = newValues,
            ChangedAt = DateTime.UtcNow
        };
        
        return JsonSerializer.Serialize(changes, DefaultJsonOptions);
    }

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = false
    };
}
