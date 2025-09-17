namespace Inventory.API.Enums;

/// <summary>
/// Enum representing different types of actions that can be audited
/// </summary>
public enum ActionType
{
    /// <summary>
    /// Create action - new entity created
    /// </summary>
    Create = 1,
    
    /// <summary>
    /// Read action - entity accessed/viewed
    /// </summary>
    Read = 2,
    
    /// <summary>
    /// Update action - entity modified
    /// </summary>
    Update = 3,
    
    /// <summary>
    /// Delete action - entity removed
    /// </summary>
    Delete = 4,
    
    /// <summary>
    /// Login action - user authentication
    /// </summary>
    Login = 5,
    
    /// <summary>
    /// Logout action - user session ended
    /// </summary>
    Logout = 6,
    
    /// <summary>
    /// Refresh action - token refresh
    /// </summary>
    Refresh = 7,
    
    /// <summary>
    /// Export action - data exported
    /// </summary>
    Export = 8,
    
    /// <summary>
    /// Import action - data imported
    /// </summary>
    Import = 9,
    
    /// <summary>
    /// Search action - search performed
    /// </summary>
    Search = 10,
    
    /// <summary>
    /// Other action - custom action
    /// </summary>
    Other = 99
}
