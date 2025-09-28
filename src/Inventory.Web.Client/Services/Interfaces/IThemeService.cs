using Inventory.Web.Client.Services.Models;

namespace Inventory.Web.Client.Services.Interfaces;

/// <summary>
/// Service interface for managing application themes
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently active theme name
    /// </summary>
    /// <returns>The name of the current theme</returns>
    string GetCurrentTheme();
    
    /// <summary>
    /// Sets the active theme and persists the selection
    /// </summary>
    /// <param name="themeName">The name of the theme to apply</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SetThemeAsync(string themeName);
    
    /// <summary>
    /// Gets all available themes
    /// </summary>
    /// <returns>An array of available theme information</returns>
    ThemeInfo[] GetAvailableThemes();
    
    /// <summary>
    /// Gets theme information for a specific theme
    /// </summary>
    /// <param name="themeName">The name of the theme</param>
    /// <returns>Theme information if found, null otherwise</returns>
    ThemeInfo? GetThemeInfo(string themeName);
    
    /// <summary>
    /// Initializes the theme service and loads the persisted theme
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InitializeThemeAsync();
    
    /// <summary>
    /// Detects and returns the system/browser preferred theme
    /// </summary>
    /// <returns>The system preferred theme name</returns>
    Task<string> GetSystemPreferredThemeAsync();
    
    /// <summary>
    /// Validates if a theme name is valid
    /// </summary>
    /// <param name="themeName">The theme name to validate</param>
    /// <returns>True if the theme is valid, false otherwise</returns>
    bool IsValidTheme(string themeName);
    
    /// <summary>
    /// Event triggered when the theme changes
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}