using System.ComponentModel;

namespace Inventory.Web.Client.Services.Models;

/// <summary>
/// Represents the category of a theme (Light or Dark)
/// </summary>
public enum ThemeCategory
{
    /// <summary>
    /// Light theme category
    /// </summary>
    [Description("Light")]
    Light = 0,
    
    /// <summary>
    /// Dark theme category
    /// </summary>
    [Description("Dark")]
    Dark = 1
}

/// <summary>
/// Contains information about a theme
/// </summary>
public class ThemeInfo
{
    /// <summary>
    /// The unique identifier for the theme (e.g., "material", "standard")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The user-friendly display name for the theme
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// The category of the theme (Light or Dark)
    /// </summary>
    public ThemeCategory Category { get; set; }
    
    /// <summary>
    /// Whether this is the default theme
    /// </summary>
    public bool IsDefault { get; set; }
    
    /// <summary>
    /// Optional description of the theme
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Event arguments for theme change events
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// The name of the new theme
    /// </summary>
    public string ThemeName { get; set; } = string.Empty;
    
    /// <summary>
    /// The previous theme name
    /// </summary>
    public string PreviousThemeName { get; set; } = string.Empty;
    
    /// <summary>
    /// The category of the new theme
    /// </summary>
    public ThemeCategory Category { get; set; }
    
    public ThemeChangedEventArgs(string themeName, string previousThemeName, ThemeCategory category)
    {
        ThemeName = themeName;
        PreviousThemeName = previousThemeName;
        Category = category;
    }
}