using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Services;

/// <summary>
/// Service for safe JSON serialization that prevents circular references and handles Entity Framework entities
/// </summary>
public class SafeSerializationService
{
    private readonly ILogger<SafeSerializationService> _logger;
    private readonly JsonSerializerOptions _safeOptions;

    public SafeSerializationService(ILogger<SafeSerializationService> logger)
    {
        _logger = logger;
        
        // Configure safe JSON serialization options
        _safeOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            MaxDepth = 10, // Limit depth to prevent deep recursion
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Safely serializes an object to JSON, handling circular references and EF entities
    /// </summary>
    /// <param name="obj">Object to serialize</param>
    /// <param name="maxDepth">Maximum serialization depth</param>
    /// <returns>JSON string or fallback representation</returns>
    public string SafeSerialize(object? obj, int maxDepth = 5)
    {
        if (obj == null)
        {
            return "null";
        }

        try
        {
            // First, try with safe options
            return JsonSerializer.Serialize(obj, _safeOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Primary serialization failed, attempting fallback for type {Type}", obj.GetType().Name);
            return CreateFallbackSerialization(obj, maxDepth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Serialization failed completely for type {Type}", obj.GetType().Name);
            return CreateErrorSerialization(obj, ex);
        }
    }

    /// <summary>
    /// Creates an audit-safe representation of an entity by excluding navigation properties
    /// </summary>
    /// <param name="entity">Entity to create safe representation for</param>
    /// <returns>Dictionary with safe properties</returns>
    public Dictionary<string, object?> CreateAuditSafeRepresentation(object entity)
    {
        var result = new Dictionary<string, object?>();
        
        if (entity == null)
        {
            return result;
        }

        try
        {
            var type = entity.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                try
                {
                    // Skip properties that are likely to cause circular references
                    if (ShouldSkipProperty(property, entity))
                    {
                        continue;
                    }

                    var value = property.GetValue(entity);
                    result[property.Name] = GetSafePropertyValue(value);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to get property {Property} from {Type}", property.Name, type.Name);
                    result[property.Name] = $"<Error: {ex.Message}>";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit safe representation for {Type}", entity.GetType().Name);
            result["_error"] = $"Serialization failed: {ex.Message}";
            result["_type"] = entity.GetType().Name;
        }

        return result;
    }

    /// <summary>
    /// Determines if a property should be skipped during serialization
    /// </summary>
    private bool ShouldSkipProperty(PropertyInfo property, object entity)
    {
        // Skip properties with JsonIgnore attribute
        if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
        {
            return true;
        }

        // Skip Entity Framework navigation properties
        if (IsNavigationProperty(property, entity))
        {
            return true;
        }

        // Skip complex collection types that might cause issues
        if (IsComplexCollection(property.PropertyType))
        {
            return true;
        }

        // Skip properties that throw exceptions when accessed
        try
        {
            _ = property.GetValue(entity);
            return false;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Checks if a property is an Entity Framework navigation property
    /// </summary>
    private bool IsNavigationProperty(PropertyInfo property, object entity)
    {
        // Check if the entity is tracked by EF (has an EntityEntry)
        try
        {
            var entityType = entity.GetType();
            
            // Look for common EF navigation property patterns
            if (property.PropertyType.IsClass && 
                property.PropertyType != typeof(string) &&
                !property.PropertyType.IsValueType &&
                !property.PropertyType.IsPrimitive)
            {
                // Check if it's a collection navigation property
                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && 
                    property.PropertyType != typeof(string))
                {
                    return true;
                }

                // Check if it's a reference navigation property that might cause circular reference
                var value = property.GetValue(entity);
                if (value != null && HasCircularReferenceRisk(value, entity))
                {
                    return true;
                }
            }
        }
        catch
        {
            // If we can't determine safely, skip it
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a property type is a complex collection that might cause serialization issues
    /// </summary>
    private bool IsComplexCollection(Type type)
    {
        if (!typeof(IEnumerable).IsAssignableFrom(type) || type == typeof(string))
        {
            return false;
        }

        // Get the element type
        var elementType = type.IsArray ? type.GetElementType() : 
                         type.IsGenericType ? type.GetGenericArguments().FirstOrDefault() : 
                         null;

        if (elementType == null)
        {
            return true; // Unknown collection type, skip to be safe
        }

        // Skip collections of complex objects that might have navigation properties
        return elementType.IsClass && 
               elementType != typeof(string) && 
               !elementType.IsPrimitive && 
               !elementType.IsValueType;
    }

    /// <summary>
    /// Checks if an object has circular reference risk
    /// </summary>
    private bool HasCircularReferenceRisk(object value, object parent)
    {
        if (value == null || parent == null)
        {
            return false;
        }

        // Simple reference equality check
        if (ReferenceEquals(value, parent))
        {
            return true;
        }

        // Check if the value has properties that reference the parent type
        var valueType = value.GetType();
        var parentType = parent.GetType();

        var properties = valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (prop.PropertyType == parentType || prop.PropertyType.IsAssignableFrom(parentType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a safe value for a property, handling complex types
    /// </summary>
    private object? GetSafePropertyValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Handle primitive types and strings
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || 
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) ||
            type.IsEnum)
        {
            return value;
        }

        // Handle nullable types
        if (Nullable.GetUnderlyingType(type) != null)
        {
            return value;
        }

        // For complex types, return a simplified representation
        if (type.IsClass)
        {
            return $"<{type.Name}>";
        }

        return value.ToString();
    }

    /// <summary>
    /// Creates a fallback serialization when normal serialization fails
    /// </summary>
    private string CreateFallbackSerialization(object obj, int maxDepth)
    {
        try
        {
            var safeRepresentation = CreateAuditSafeRepresentation(obj);
            return JsonSerializer.Serialize(safeRepresentation, _safeOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback serialization also failed for {Type}", obj.GetType().Name);
            return CreateErrorSerialization(obj, ex);
        }
    }

    /// <summary>
    /// Creates an error representation when serialization fails completely
    /// </summary>
    private string CreateErrorSerialization(object obj, Exception ex)
    {
        try
        {
            var errorInfo = new
            {
                _serializationError = true,
                _type = obj.GetType().Name,
                _message = "Serialization failed",
                _error = ex.Message,
                _timestamp = DateTime.UtcNow
            };
            
            return JsonSerializer.Serialize(errorInfo, _safeOptions);
        }
        catch
        {
            // Last resort - return a simple string representation
            return $"{{\"_serializationError\":true,\"_type\":\"{obj.GetType().Name}\",\"_message\":\"Complete serialization failure\"}}";
        }
    }
}