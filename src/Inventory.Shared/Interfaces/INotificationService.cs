using Inventory.Shared.DTOs;
using Inventory.Shared.Models;

namespace Inventory.Shared.Interfaces;

public interface INotificationService
{
    // Notification CRUD operations
    Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationRequest request);
    Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20);
    Task<ApiResponse<NotificationDto>> GetNotificationAsync(int notificationId);
    Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, string userId);
    Task<ApiResponse<bool>> MarkAllAsReadAsync(string userId);
    Task<ApiResponse<bool>> ArchiveNotificationAsync(int notificationId, string userId);
    Task<ApiResponse<bool>> DeleteNotificationAsync(int notificationId, string userId);
    Task<ApiResponse<NotificationStatsDto>> GetNotificationStatsAsync(string userId);
    
    // Notification preferences
    Task<ApiResponse<List<NotificationPreferenceDto>>> GetUserPreferencesAsync(string userId);
    Task<ApiResponse<NotificationPreferenceDto>> UpdatePreferenceAsync(string userId, UpdateNotificationPreferenceRequest request);
    Task<ApiResponse<bool>> DeletePreferenceAsync(string userId, string eventType);
    
    // Notification rules
    Task<ApiResponse<List<NotificationRuleDto>>> GetNotificationRulesAsync();
    Task<ApiResponse<NotificationRuleDto>> CreateNotificationRuleAsync(CreateNotificationRuleRequest request);
    Task<ApiResponse<NotificationRuleDto>> UpdateNotificationRuleAsync(int ruleId, CreateNotificationRuleRequest request);
    Task<ApiResponse<bool>> DeleteNotificationRuleAsync(int ruleId);
    Task<ApiResponse<bool>> ToggleNotificationRuleAsync(int ruleId);
    
    // Smart notification triggers
    Task TriggerStockLowNotificationAsync(object product);
    Task TriggerStockOutNotificationAsync(object product);
    Task TriggerTransactionNotificationAsync(object transaction);
    Task TriggerSystemNotificationAsync(string title, string message, string userId, string? actionUrl = null);
    
    // Bulk operations
    Task<ApiResponse<bool>> SendBulkNotificationAsync(List<string> userIds, CreateNotificationRequest request);
    Task<ApiResponse<bool>> CleanupExpiredNotificationsAsync();
}

public interface INotificationRuleEngine
{
    Task<List<NotificationRule>> GetActiveRulesForEventAsync(string eventType);
    Task<bool> EvaluateConditionAsync(string condition, object data);
    Task<string> ProcessTemplateAsync(string template, object data);
    Task<List<NotificationPreference>> GetUserPreferencesForEventAsync(string eventType);
}

public interface INotificationTemplateService
{
    Task<string> GetSubjectTemplateAsync(string eventType);
    Task<string> GetBodyTemplateAsync(string eventType);
    Task<string> ProcessTemplateAsync(string template, object data);
    Task<ApiResponse<bool>> CreateTemplateAsync(NotificationTemplate template);
    Task<ApiResponse<bool>> UpdateTemplateAsync(int templateId, NotificationTemplate template);
    Task<ApiResponse<bool>> DeleteTemplateAsync(int templateId);
}
