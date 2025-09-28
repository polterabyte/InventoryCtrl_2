using Blazored.LocalStorage;
using Inventory.Web.Client.Services.Interfaces;
using Inventory.Web.Client.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Service for managing application themes with persistence and system preference detection
/// </summary>
public class ThemeService : IThemeService
{
    private const string ThemeStorageKey = "InventoryCtrl_Theme";
    private const string DefaultTheme = "material";
    
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<ThemeService> _logger;
    private readonly IJSRuntime _jsRuntime;
    
    private string _currentTheme = DefaultTheme;
    private bool _initialized = false;
    
    // Available Radzen themes as defined in the design document
    private readonly ThemeInfo[] _availableThemes = 
    [
        new() { Name = "material", DisplayName = "Material Design", Category = ThemeCategory.Light, IsDefault = true, Description = "Google's Material Design 2 light theme" },
        new() { Name = "material-dark", DisplayName = "Material Design Dark", Category = ThemeCategory.Dark, Description = "Google's Material Design 2 dark theme" },
        new() { Name = "standard", DisplayName = "Standard", Category = ThemeCategory.Light, Description = "Clean, minimal light theme" },
        new() { Name = "standard-dark", DisplayName = "Standard Dark", Category = ThemeCategory.Dark, Description = "Clean, minimal dark theme" },
        new() { Name = "default", DisplayName = "Default", Category = ThemeCategory.Light, Description = "Default Radzen light theme" },
        new() { Name = "dark", DisplayName = "Dark", Category = ThemeCategory.Dark, Description = "Default Radzen dark theme" },
        new() { Name = "humanistic", DisplayName = "Humanistic", Category = ThemeCategory.Light, Description = "Humanistic design approach" },
        new() { Name = "humanistic-dark", DisplayName = "Humanistic Dark", Category = ThemeCategory.Dark, Description = "Humanistic dark variant" },
        new() { Name = "software", DisplayName = "Software", Category = ThemeCategory.Light, Description = "Software-focused theme" },
        new() { Name = "software-dark", DisplayName = "Software Dark", Category = ThemeCategory.Dark, Description = "Software-focused dark variant" }
    ];
    
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    
    public ThemeService(ILocalStorageService localStorage, ILogger<ThemeService> logger, IJSRuntime jsRuntime)
    {
        _localStorage = localStorage;
        _logger = logger;
        _jsRuntime = jsRuntime;
    }
    
    /// <summary>
    /// Gets the currently active theme name
    /// </summary>
    public string GetCurrentTheme()
    {
        return _currentTheme;
    }
    
    /// <summary>
    /// Sets the active theme and persists the selection
    /// </summary>
    public async Task SetThemeAsync(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
        {
            _logger.LogWarning("Attempted to set empty or null theme name");
            return;
        }
        
        if (!IsValidTheme(themeName))
        {
            _logger.LogWarning("Attempted to set invalid theme: {ThemeName}", themeName);
            return;
        }
        
        var previousTheme = _currentTheme;
        
        try
        {
            // Update current theme
            _currentTheme = themeName;
            
            // Persist to local storage
            await _localStorage.SetItemAsync(ThemeStorageKey, themeName);
            
            // Get theme info for event
            var themeInfo = GetThemeInfo(themeName);
            var category = themeInfo?.Category ?? ThemeCategory.Light;
            
            // Trigger theme changed event
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(themeName, previousTheme, category));
            
            _logger.LogInformation("Theme changed from {PreviousTheme} to {NewTheme}", previousTheme, themeName);
        }
        catch (Exception ex)
        {
            // Rollback on error
            _currentTheme = previousTheme;
            _logger.LogError(ex, "Failed to set theme to {ThemeName}, rolled back to {PreviousTheme}", themeName, previousTheme);
            throw;
        }
    }
    
    /// <summary>
    /// Gets all available themes
    /// </summary>
    public ThemeInfo[] GetAvailableThemes()
    {
        return _availableThemes;
    }
    
    /// <summary>
    /// Gets theme information for a specific theme
    /// </summary>
    public ThemeInfo? GetThemeInfo(string themeName)
    {
        return _availableThemes.FirstOrDefault(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Initializes the theme service and loads the persisted theme
    /// </summary>
    public async Task InitializeThemeAsync()
    {
        if (_initialized)
        {
            _logger.LogDebug("Theme service already initialized");
            return;
        }
        
        try
        {
            _logger.LogInformation("Initializing theme service");
            
            // Try to load persisted theme
            var persistedTheme = await _localStorage.GetItemAsync<string>(ThemeStorageKey);
            
            if (!string.IsNullOrWhiteSpace(persistedTheme) && IsValidTheme(persistedTheme))
            {
                _currentTheme = persistedTheme;
                _logger.LogInformation("Loaded persisted theme: {ThemeName}", persistedTheme);
            }
            else
            {
                // Try to detect system preference if no valid persisted theme
                var systemTheme = await GetSystemPreferredThemeAsync();
                _currentTheme = systemTheme;
                
                // Save the detected theme
                await _localStorage.SetItemAsync(ThemeStorageKey, _currentTheme);
                _logger.LogInformation("No valid persisted theme found, using system preference: {ThemeName}", _currentTheme);
            }
            
            _initialized = true;
            _logger.LogInformation("Theme service initialized with theme: {ThemeName}", _currentTheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize theme service, using default theme: {DefaultTheme}", DefaultTheme);
            _currentTheme = DefaultTheme;
            _initialized = true;
        }
    }
    
    /// <summary>
    /// Detects and returns the system/browser preferred theme
    /// </summary>
    public async Task<string> GetSystemPreferredThemeAsync()
    {
        try
        {
            // Check if browser prefers dark mode
            var prefersDarkMode = await _jsRuntime.InvokeAsync<bool>("eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
            
            // Return appropriate default theme based on system preference
            return prefersDarkMode ? "material-dark" : "material";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect system theme preference, using default");
            return DefaultTheme;
        }
    }
    
    /// <summary>
    /// Validates if a theme name is valid
    /// </summary>
    public bool IsValidTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            return false;
            
        return _availableThemes.Any(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
    }
}