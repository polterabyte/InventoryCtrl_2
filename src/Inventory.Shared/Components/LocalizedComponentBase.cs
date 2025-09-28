using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Inventory.Shared.Interfaces;
using Inventory.Shared.Resources;
using System.Globalization;
using System.Collections.Concurrent;

namespace Inventory.Shared.Components;

/// <summary>
/// High-performance base class for components that require localization support
/// Features caching, debounced re-renders, and optimized culture change handling
/// </summary>
public abstract class LocalizedComponentBase : ComponentBase, IDisposable
{
    [Inject] protected IStringLocalizer<SharedResources> Localizer { get; set; } = default!;
    [Inject] protected ICultureService CultureService { get; set; } = default!;

    private readonly ConcurrentDictionary<string, string> _stringCache = new();
    private readonly object _reRenderLock = new();
    private Timer? _debounceTimer;
    private bool _disposed = false;
    private bool _pendingStateChange = false;
    private CultureInfo? _lastCulture;

    protected override void OnInitialized()
    {
        // Subscribe to culture changes
        CultureService.CultureChanged += OnCultureChanged;
        _lastCulture = CultureService.CurrentCulture;
        base.OnInitialized();
    }

    /// <summary>
    /// Called when the application culture changes
    /// Uses debouncing to prevent excessive re-renders during rapid culture switches
    /// </summary>
    /// <param name="sender">The culture service</param>
    /// <param name="newCulture">The new culture</param>
    protected virtual void OnCultureChanged(object? sender, CultureInfo newCulture)
    {
        // Only react if culture actually changed
        if (_lastCulture?.Name == newCulture.Name)
            return;
            
        _lastCulture = newCulture;
        
        // Clear cache when culture changes
        _stringCache.Clear();
        
        // Debounce re-renders to improve performance during rapid culture changes
        lock (_reRenderLock)
        {
            _pendingStateChange = true;
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer((_) =>
            {
                if (_pendingStateChange && !_disposed)
                {
                    _pendingStateChange = false;
                    InvokeAsync(() =>
                    {
                        if (!_disposed)
                        {
                            StateHasChanged();
                        }
                    });
                }
            }, null, TimeSpan.FromMilliseconds(100), Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Gets a localized string for the given key with caching for better performance
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <returns>The localized string</returns>
    protected string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
            
        var cacheKey = $"{CultureService.CurrentCulture.Name}:{key}";
        
        return _stringCache.GetOrAdd(cacheKey, _ =>
        {
            var localizedString = Localizer[key];
            return localizedString.ResourceNotFound ? key : localizedString.Value;
        });
    }

    /// <summary>
    /// Gets a localized string for the given key with format arguments
    /// Format strings are not cached due to variable arguments
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <param name="arguments">Format arguments</param>
    /// <returns>The formatted localized string</returns>
    protected string GetString(string key, params object[] arguments)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
            
        if (arguments?.Length == 0)
            return GetString(key);
            
        var localizedString = Localizer[key, arguments];
        return localizedString.ResourceNotFound ? key : localizedString.Value;
    }

    /// <summary>
    /// Formats a date according to the current culture with caching
    /// </summary>
    /// <param name="date">The date to format</param>
    /// <param name="format">Optional format string</param>
    /// <returns>Formatted date string</returns>
    protected string FormatDate(DateTime date, string? format = null)
    {
        var culture = CultureService.CurrentCulture;
        var cacheKey = $"{culture.Name}:date:{date.Ticks}:{format ?? "default"}";
        
        return _stringCache.GetOrAdd(cacheKey, _ =>
            format == null 
                ? date.ToString(culture)
                : date.ToString(format, culture));
    }

    /// <summary>
    /// Formats a number according to the current culture with caching
    /// </summary>
    /// <param name="number">The number to format</param>
    /// <param name="format">Optional format string</param>
    /// <returns>Formatted number string</returns>
    protected string FormatNumber(decimal number, string? format = null)
    {
        var culture = CultureService.CurrentCulture;
        var cacheKey = $"{culture.Name}:number:{number}:{format ?? "default"}";
        
        return _stringCache.GetOrAdd(cacheKey, _ =>
            format == null 
                ? number.ToString(culture)
                : number.ToString(format, culture));
    }

    /// <summary>
    /// Formats a currency amount according to the current culture with caching
    /// </summary>
    /// <param name="amount">The amount to format</param>
    /// <returns>Formatted currency string</returns>
    protected string FormatCurrency(decimal amount)
    {
        var culture = CultureService.CurrentCulture;
        var cacheKey = $"{culture.Name}:currency:{amount}";
        
        return _stringCache.GetOrAdd(cacheKey, _ =>
            amount.ToString("C", culture));
    }

    /// <summary>
    /// Gets the current culture's text direction (LTR/RTL)
    /// </summary>
    protected string TextDirection => 
        CultureService.CurrentCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr";

    /// <summary>
    /// Gets the current culture's language code
    /// </summary>
    protected string LanguageCode => CultureService.CurrentCulture.TwoLetterISOLanguageName;
    
    /// <summary>
    /// Gets whether the current culture uses right-to-left text direction
    /// </summary>
    protected bool IsRightToLeft => CultureService.CurrentCulture.TextInfo.IsRightToLeft;
    
    /// <summary>
    /// Clears the localization cache - useful when you need fresh translations
    /// </summary>
    protected void ClearLocalizationCache()
    {
        _stringCache.Clear();
    }
    
    /// <summary>
    /// Gets cache statistics for debugging purposes
    /// </summary>
    protected (int Count, long MemoryEstimate) GetCacheStats()
    {
        var count = _stringCache.Count;
        var memoryEstimate = _stringCache.Sum(kvp => 
            (kvp.Key?.Length ?? 0) * 2 + (kvp.Value?.Length ?? 0) * 2); // Rough estimate in bytes
        return (count, memoryEstimate);
    }

    public virtual void Dispose()
    {
        if (!_disposed)
        {
            CultureService.CultureChanged -= OnCultureChanged;
            _debounceTimer?.Dispose();
            _stringCache.Clear();
            _disposed = true;
        }
    }
}