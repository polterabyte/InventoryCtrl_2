using System.Globalization;

namespace Inventory.Shared.Interfaces;

/// <summary>
/// Service for managing application culture and localization
/// </summary>
public interface ICultureService
{
    /// <summary>
    /// Gets the current culture
    /// </summary>
    CultureInfo CurrentCulture { get; }
    
    /// <summary>
    /// Gets the current UI culture
    /// </summary>
    CultureInfo CurrentUICulture { get; }
    
    /// <summary>
    /// Sets the culture for the application
    /// </summary>
    /// <param name="culture">Culture name (e.g., "en-US", "ru-RU")</param>
    Task SetCultureAsync(string culture);
    
    /// <summary>
    /// Gets all supported cultures
    /// </summary>
    IEnumerable<CultureInfo> GetSupportedCultures();
    
    /// <summary>
    /// Event triggered when culture changes
    /// </summary>
    event EventHandler<CultureInfo>? CultureChanged;
    
    /// <summary>
    /// Gets the user's preferred culture from browser or stored preference
    /// </summary>
    Task<string> GetPreferredCultureAsync();
}