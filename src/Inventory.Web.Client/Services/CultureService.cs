using Inventory.Shared.Interfaces;
using Blazored.LocalStorage;
using Microsoft.JSInterop;
using System.Globalization;
using System.Collections.Concurrent;

namespace Inventory.Web.Client.Services;

/// <summary>
/// High-performance service for managing application culture and localization in Blazor WebAssembly
/// Features caching, lazy loading, and optimized culture switching
/// </summary>
public class CultureService : ICultureService, IDisposable
{
    private const string CULTURE_KEY = "app-culture";
    private const string CULTURE_CACHE_KEY = "culture-cache";
    
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;
    private readonly ConcurrentDictionary<string, CultureInfo> _cultureCache;
    private readonly Timer _cacheCleanupTimer;
    
    private CultureInfo _currentCulture;
    private CultureInfo _currentUICulture;
    private string? _cachedPreferredCulture;
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private bool _disposed = false;
    
    private readonly CultureInfo[] _supportedCultures = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("ru-RU")
    };

    public event EventHandler<CultureInfo>? CultureChanged;

    public CultureService(ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
        _currentCulture = CultureInfo.CurrentCulture;
        _currentUICulture = CultureInfo.CurrentUICulture;
        _cultureCache = new ConcurrentDictionary<string, CultureInfo>();
        
        // Initialize cache with supported cultures
        foreach (var culture in _supportedCultures)
        {
            _cultureCache.TryAdd(culture.Name, culture);
        }
        
        // Setup cache cleanup timer (runs every 30 minutes)
        _cacheCleanupTimer = new Timer(CleanupCache, null, 
            TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
    }

    public CultureInfo CurrentCulture => _currentCulture;
    public CultureInfo CurrentUICulture => _currentUICulture;

    public async Task SetCultureAsync(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return;

        // Use cached culture info for better performance
        if (!_cultureCache.TryGetValue(culture, out var cultureInfo))
        {
            try
            {
                cultureInfo = new CultureInfo(culture);
                _cultureCache.TryAdd(culture, cultureInfo);
            }
            catch (CultureNotFoundException)
            {
                return; // Invalid culture, ignore
            }
        }
        
        // Validate that the culture is supported
        if (!_supportedCultures.Any(c => c.Name == cultureInfo.Name))
            return;

        // Only update if culture actually changed
        if (_currentCulture.Name == cultureInfo.Name)
            return;

        _currentCulture = cultureInfo;
        _currentUICulture = cultureInfo;

        // Set the culture for the current thread
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        // Store the culture preference asynchronously for better performance
        _ = Task.Run(async () =>
        {
            try
            {
                await _localStorage.SetItemAsStringAsync(CULTURE_KEY, culture);
                _cachedPreferredCulture = culture;
                _lastCacheUpdate = DateTime.UtcNow;
            }
            catch
            {
                // Silently handle localStorage errors
            }
        });

        // Trigger culture changed event
        CultureChanged?.Invoke(this, cultureInfo);
    }

    public IEnumerable<CultureInfo> GetSupportedCultures()
    {
        return _supportedCultures;
    }

    public async Task<string> GetPreferredCultureAsync()
    {
        try
        {
            // Use cached value if recent (within 5 minutes)
            if (!string.IsNullOrWhiteSpace(_cachedPreferredCulture) && 
                DateTime.UtcNow - _lastCacheUpdate < TimeSpan.FromMinutes(5))
            {
                return _cachedPreferredCulture;
            }

            // Try to get stored preference
            var storedCulture = await _localStorage.GetItemAsStringAsync(CULTURE_KEY);
            if (!string.IsNullOrWhiteSpace(storedCulture) && 
                _supportedCultures.Any(c => c.Name == storedCulture))
            {
                _cachedPreferredCulture = storedCulture;
                _lastCacheUpdate = DateTime.UtcNow;
                return storedCulture;
            }

            // If no stored preference, try to get browser language
            var browserLanguage = await GetBrowserLanguageAsync();
            if (!string.IsNullOrWhiteSpace(browserLanguage))
            {
                // Try to find exact match first
                var exactMatch = _supportedCultures.FirstOrDefault(c => 
                    c.Name.Equals(browserLanguage, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null)
                {
                    _cachedPreferredCulture = exactMatch.Name;
                    _lastCacheUpdate = DateTime.UtcNow;
                    return exactMatch.Name;
                }

                // Try to find language match (e.g., "en" for "en-GB")
                var languageCode = browserLanguage.Split('-')[0];
                var languageMatch = _supportedCultures.FirstOrDefault(c => 
                    c.TwoLetterISOLanguageName.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
                if (languageMatch != null)
                {
                    _cachedPreferredCulture = languageMatch.Name;
                    _lastCacheUpdate = DateTime.UtcNow;
                    return languageMatch.Name;
                }
            }
        }
        catch
        {
            // If any error occurs, fall back to default
        }

        // Default to first supported culture (English)
        var defaultCulture = _supportedCultures[0].Name;
        _cachedPreferredCulture = defaultCulture;
        _lastCacheUpdate = DateTime.UtcNow;
        return defaultCulture;
    }

    private async Task<string?> GetBrowserLanguageAsync()
    {
        try
        {
            // Use a more robust approach with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            return await _jsRuntime.InvokeAsync<string>("eval", cts.Token, 
                "navigator.language || navigator.userLanguage || 'en-US'");
        }
        catch
        {
            return null;
        }
    }
    
    private void CleanupCache(object? state)
    {
        try
        {
            // Keep only supported cultures in cache
            var keysToRemove = _cultureCache.Keys
                .Where(key => !_supportedCultures.Any(c => c.Name == key))
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                _cultureCache.TryRemove(key, out _);
            }
            
            // Clear cached preference if it's old (older than 1 hour)
            if (DateTime.UtcNow - _lastCacheUpdate > TimeSpan.FromHours(1))
            {
                _cachedPreferredCulture = null;
                _lastCacheUpdate = DateTime.MinValue;
            }
        }
        catch
        {
            // Silently handle cleanup errors
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cacheCleanupTimer?.Dispose();
            _cultureCache.Clear();
            _disposed = true;
        }
    }
}