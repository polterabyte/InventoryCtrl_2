namespace Inventory.API.Models;

/// <summary>
/// Audit log for tracking changes to entities
/// </summary>
public class AuditLog(
    string entityName, 
    string entityId, 
    string action, 
    string userId, 
    string? oldValues = null, 
    string? newValues = null, 
    string? description = null)
{
    public int Id { get; set; }
    public string EntityName { get; set; } = entityName;
    public string EntityId { get; set; } = entityId;
    public string Action { get; set; } = action;
    public string UserId { get; set; } = userId;
    public string? OldValues { get; set; } = oldValues;
    public string? NewValues { get; set; } = newValues;
    public string? Description { get; set; } = description;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    // Navigation properties
    public User User { get; set; } = null!;
}
