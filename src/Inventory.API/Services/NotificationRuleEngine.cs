using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.Models;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Inventory.API.Services;

public class NotificationRuleEngine : INotificationRuleEngine
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationRuleEngine> _logger;

    public NotificationRuleEngine(AppDbContext context, ILogger<NotificationRuleEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<NotificationRule>> GetActiveRulesForEventAsync(string eventType)
    {
        try
        {
            var rules = await _context.NotificationRules
                .Where(r => r.IsActive && r.EventType == eventType)
                .OrderBy(r => r.Priority)
                .ToListAsync();

            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active rules for event {EventType}", eventType);
            return new List<NotificationRule>();
        }
    }

    public Task<bool> EvaluateConditionAsync(string condition, object data)
    {
        try
        {
            // Parse JSON condition
            var conditionObj = JsonSerializer.Deserialize<Dictionary<string, object>>(condition);
            if (conditionObj == null)
            {
                _logger.LogWarning("Invalid condition format: {Condition}", condition);
                return Task.FromResult(false);
            }

            // Simple condition evaluation based on common patterns
            foreach (var kvp in conditionObj)
            {
                var propertyPath = kvp.Key;
                var expectedValue = kvp.Value;

                var actualValue = GetPropertyValue(data, propertyPath);
                if (actualValue == null)
                {
                    _logger.LogWarning("Property {PropertyPath} not found in data", propertyPath);
                    return Task.FromResult(false);
                }

                if (!EvaluateCondition(actualValue, expectedValue))
                {
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate condition: {Condition}", condition);
            return Task.FromResult(false);
        }
    }

    public Task<string> ProcessTemplateAsync(string template, object data)
    {
        try
        {
            var result = template;
            
            // Find all placeholders in format {{PropertyPath}}
            var placeholders = Regex.Matches(template, @"\{\{([^}]+)\}\}");
            
            foreach (Match match in placeholders)
            {
                var propertyPath = match.Groups[1].Value;
                var value = GetPropertyValue(data, propertyPath);
                
                if (value != null)
                {
                    var stringValue = value.ToString() ?? string.Empty;
                    result = result.Replace(match.Value, stringValue);
                }
                else
                {
                    _logger.LogWarning("Property {PropertyPath} not found in template data", propertyPath);
                    result = result.Replace(match.Value, "[N/A]");
                }
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process template: {Template}", template);
            return Task.FromResult(template);
        }
    }

    public async Task<List<NotificationPreference>> GetUserPreferencesForEventAsync(string eventType)
    {
        try
        {
            var preferences = await _context.NotificationPreferences
                .Where(p => p.EventType == eventType)
                .ToListAsync();

            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user preferences for event {EventType}", eventType);
            return new List<NotificationPreference>();
        }
    }

    private object? GetPropertyValue(object obj, string propertyPath)
    {
        try
        {
            var parts = propertyPath.Split('.');
            var current = obj;

            foreach (var part in parts)
            {
                if (current == null) return null;

                var property = current.GetType().GetProperty(part);
                if (property == null) return null;

                current = property.GetValue(current);
            }

            return current;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get property value for path {PropertyPath}", propertyPath);
            return null;
        }
    }

    private bool EvaluateCondition(object actualValue, object expectedValue)
    {
        try
        {
            // Handle different comparison types
            if (expectedValue is JsonElement jsonElement)
            {
                return EvaluateJsonCondition(actualValue, jsonElement);
            }

            // Simple equality comparison
            return actualValue.Equals(expectedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate condition between {ActualValue} and {ExpectedValue}", 
                actualValue, expectedValue);
            return false;
        }
    }

    private bool EvaluateJsonCondition(object actualValue, JsonElement expectedValue)
    {
        try
        {
            switch (expectedValue.ValueKind)
            {
                case JsonValueKind.String:
                    return actualValue.ToString() == expectedValue.GetString();
                
                case JsonValueKind.Number:
                    if (actualValue is int intValue)
                        return intValue == expectedValue.GetInt32();
                    if (actualValue is decimal decimalValue)
                        return decimalValue == expectedValue.GetDecimal();
                    if (actualValue is double doubleValue)
                        return doubleValue == expectedValue.GetDouble();
                    break;
                
                case JsonValueKind.True:
                    return actualValue is bool boolValueTrue && boolValueTrue;
                
                case JsonValueKind.False:
                    return actualValue is bool boolValueFalse && !boolValueFalse;
                
                case JsonValueKind.Object:
                    return EvaluateObjectCondition(actualValue, expectedValue);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate JSON condition");
            return false;
        }
    }

    private bool EvaluateObjectCondition(object actualValue, JsonElement expectedValue)
    {
        try
        {
            // Handle comparison operators like { "operator": ">=", "value": 10 }
            if (expectedValue.TryGetProperty("operator", out var operatorElement) &&
                expectedValue.TryGetProperty("value", out var valueElement))
            {
                var operatorStr = operatorElement.GetString();
                var expectedVal = valueElement;

                return operatorStr switch
                {
                    "==" => EvaluateComparison(actualValue, expectedVal, (a, b) => a == b),
                    "!=" => EvaluateComparison(actualValue, expectedVal, (a, b) => a != b),
                    ">" => EvaluateComparison(actualValue, expectedVal, (a, b) => Convert.ToDecimal(a) > Convert.ToDecimal(b)),
                    ">=" => EvaluateComparison(actualValue, expectedVal, (a, b) => Convert.ToDecimal(a) >= Convert.ToDecimal(b)),
                    "<" => EvaluateComparison(actualValue, expectedVal, (a, b) => Convert.ToDecimal(a) < Convert.ToDecimal(b)),
                    "<=" => EvaluateComparison(actualValue, expectedVal, (a, b) => Convert.ToDecimal(a) <= Convert.ToDecimal(b)),
                    "contains" => actualValue.ToString()?.Contains(expectedVal.GetString() ?? "") == true,
                    "startsWith" => actualValue.ToString()?.StartsWith(expectedVal.GetString() ?? "") == true,
                    "endsWith" => actualValue.ToString()?.EndsWith(expectedVal.GetString() ?? "") == true,
                    _ => false
                };
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate object condition");
            return false;
        }
    }

    private bool EvaluateComparison(object actualValue, JsonElement expectedValue, Func<object, object, bool> comparison)
    {
        try
        {
            object expected = expectedValue.ValueKind switch
            {
                JsonValueKind.String => expectedValue.GetString() ?? string.Empty,
                JsonValueKind.Number => expectedValue.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => expectedValue.ToString() ?? string.Empty
            };

            return comparison(actualValue, expected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate comparison");
            return false;
        }
    }
}
