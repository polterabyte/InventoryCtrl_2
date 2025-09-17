using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Inventory.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Product = Inventory.API.Models.Product;
using InventoryTransaction = Inventory.API.Models.InventoryTransaction;

namespace Inventory.API.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationRuleEngine _ruleEngine;
    private readonly ISignalRNotificationService _signalRService;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger, INotificationRuleEngine ruleEngine, ISignalRNotificationService signalRService)
    {
        _context = context;
        _logger = logger;
        _ruleEngine = ruleEngine;
        _signalRService = signalRService;
    }

    public async Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationRequest request)
    {
        try
        {
            var notification = new DbNotification
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Category = request.Category,
                ActionUrl = request.ActionUrl,
                ActionText = request.ActionText,
                UserId = request.UserId,
                ProductId = request.ProductId,
                TransactionId = request.TransactionId,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
                notification.Id, request.UserId);

            // Send real-time notification via SignalR
            var notificationDto = MapToDto(notification);
            if (!string.IsNullOrEmpty(request.UserId))
            {
                await _signalRService.SendNotificationToUserAsync(request.UserId, notificationDto);
            }

            return new ApiResponse<NotificationDto>
            {
                Success = true,
                Data = notificationDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}", request.UserId);
            return new ApiResponse<NotificationDto>
            {
                Success = false,
                ErrorMessage = "Failed to create notification"
            };
        }
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsArchived)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => MapToDto(n))
                .ToListAsync();

            return new ApiResponse<List<NotificationDto>>
            {
                Success = true,
                Data = notifications
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            return new ApiResponse<List<NotificationDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve notifications"
            };
        }
    }

    public async Task<ApiResponse<NotificationDto>> GetNotificationAsync(int notificationId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
            {
                return new ApiResponse<NotificationDto>
                {
                    Success = false,
                    ErrorMessage = "Notification not found"
                };
            }

            return new ApiResponse<NotificationDto>
            {
                Success = true,
                Data = MapToDto(notification)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification {NotificationId}", notificationId);
            return new ApiResponse<NotificationDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve notification"
            };
        }
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, string userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Notification not found"
                };
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked notification {NotificationId} as read for user {UserId}", 
                notificationId, userId);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", notificationId);
            return new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to mark notification as read"
            };
        }
    }

    public async Task<ApiResponse<bool>> MarkAllAsReadAsync(string userId)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked all notifications as read for user {UserId}", userId);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
            return new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to mark all notifications as read"
            };
        }
    }

    public async Task<ApiResponse<bool>> ArchiveNotificationAsync(int notificationId, string userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Notification not found"
                };
            }

            notification.IsArchived = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Archived notification {NotificationId} for user {UserId}", 
                notificationId, userId);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive notification {NotificationId}", notificationId);
            return new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to archive notification"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteNotificationAsync(int notificationId, string userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Notification not found"
                };
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted notification {NotificationId} for user {UserId}", 
                notificationId, userId);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", notificationId);
            return new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to delete notification"
            };
        }
    }

    public async Task<ApiResponse<NotificationStatsDto>> GetNotificationStatsAsync(string userId)
    {
        try
        {
            var totalNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId);

            var unreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var archivedNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.IsArchived);

            var lastNotification = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            var stats = new NotificationStatsDto
            {
                TotalNotifications = totalNotifications,
                UnreadNotifications = unreadNotifications,
                ArchivedNotifications = archivedNotifications,
                LastNotificationDate = lastNotification
            };

            return new ApiResponse<NotificationStatsDto>
            {
                Success = true,
                Data = stats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification stats for user {UserId}", userId);
            return new ApiResponse<NotificationStatsDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve notification statistics"
            };
        }
    }

    public async Task<ApiResponse<List<NotificationPreferenceDto>>> GetUserPreferencesAsync(string userId)
    {
        try
        {
            var preferences = await _context.NotificationPreferences
                .Where(p => p.UserId == userId)
                .Select(p => new NotificationPreferenceDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    EventType = p.EventType,
                    EmailEnabled = p.EmailEnabled,
                    InAppEnabled = p.InAppEnabled,
                    PushEnabled = p.PushEnabled,
                    MinPriority = p.MinPriority,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return new ApiResponse<List<NotificationPreferenceDto>>
            {
                Success = true,
                Data = preferences
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get preferences for user {UserId}", userId);
            return new ApiResponse<List<NotificationPreferenceDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve notification preferences"
            };
        }
    }

    public async Task<ApiResponse<NotificationPreferenceDto>> UpdatePreferenceAsync(string userId, UpdateNotificationPreferenceRequest request)
    {
        try
        {
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.EventType == request.EventType);

            if (preference == null)
            {
                preference = new NotificationPreference
                {
                    UserId = userId,
                    EventType = request.EventType,
                    CreatedAt = DateTime.UtcNow
                };
                _context.NotificationPreferences.Add(preference);
            }

            preference.EmailEnabled = request.EmailEnabled;
            preference.InAppEnabled = request.InAppEnabled;
            preference.PushEnabled = request.PushEnabled;
            preference.MinPriority = request.MinPriority;
            preference.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dto = new NotificationPreferenceDto
            {
                Id = preference.Id,
                UserId = preference.UserId,
                EventType = preference.EventType,
                EmailEnabled = preference.EmailEnabled,
                InAppEnabled = preference.InAppEnabled,
                PushEnabled = preference.PushEnabled,
                MinPriority = preference.MinPriority,
                CreatedAt = preference.CreatedAt,
                UpdatedAt = preference.UpdatedAt
            };

            return new ApiResponse<NotificationPreferenceDto>
            {
                Success = true,
                Data = dto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update preference for user {UserId}, event {EventType}", 
                userId, request.EventType);
            return new ApiResponse<NotificationPreferenceDto>
            {
                Success = false,
                ErrorMessage = "Failed to update notification preference"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeletePreferenceAsync(string userId, string eventType)
    {
        try
        {
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.EventType == eventType);

            if (preference == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Preference not found"
                };
            }

            _context.NotificationPreferences.Remove(preference);
            await _context.SaveChangesAsync();

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete preference for user {UserId}, event {EventType}", 
                userId, eventType);
            return new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to delete notification preference"
            };
        }
    }

    public async Task<ApiResponse<List<NotificationRuleDto>>> GetNotificationRulesAsync()
    {
        try
        {
            var rules = await _context.NotificationRules
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Name)
                .Select(r => new NotificationRuleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    EventType = r.EventType,
                    NotificationType = r.NotificationType,
                    Category = r.Category,
                    Condition = r.Condition,
                    Template = r.Template,
                    IsActive = r.IsActive,
                    Priority = r.Priority,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    CreatedBy = r.CreatedBy
                })
                .ToListAsync();

            return new ApiResponse<List<NotificationRuleDto>>
            {
                Success = true,
                Data = rules
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification rules");
            return new ApiResponse<List<NotificationRuleDto>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve notification rules"
            };
        }
    }

    public async Task<ApiResponse<NotificationRuleDto>> CreateNotificationRuleAsync(CreateNotificationRuleRequest request)
    {
        try
        {
            var rule = new NotificationRule
            {
                Name = request.Name,
                Description = request.Description,
                EventType = request.EventType,
                NotificationType = request.NotificationType,
                Category = request.Category,
                Condition = request.Condition,
                Template = request.Template,
                IsActive = request.IsActive,
                Priority = request.Priority,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationRules.Add(rule);
            await _context.SaveChangesAsync();

            var dto = new NotificationRuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                EventType = rule.EventType,
                NotificationType = rule.NotificationType,
                Category = rule.Category,
                Condition = rule.Condition,
                Template = rule.Template,
                IsActive = rule.IsActive,
                Priority = rule.Priority,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt,
                CreatedBy = rule.CreatedBy
            };

            return new ApiResponse<NotificationRuleDto>
            {
                Success = true,
                Data = dto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification rule");
            return new ApiResponse<NotificationRuleDto>
            {
                Success = false,
                ErrorMessage = "Failed to create notification rule"
            };
        }
    }

    public async Task<ApiResponse<NotificationRuleDto>> UpdateNotificationRuleAsync(int ruleId, CreateNotificationRuleRequest request)
    {
        try
        {
            var rule = await _context.NotificationRules.FindAsync(ruleId);
            if (rule == null)
            {
                return new ApiResponse<NotificationRuleDto>
                {
                    Success = false,
                    ErrorMessage = "Notification rule not found"
                };
            }

            rule.Name = request.Name;
            rule.Description = request.Description;
            rule.EventType = request.EventType;
            rule.NotificationType = request.NotificationType;
            rule.Category = request.Category;
            rule.Condition = request.Condition;
            rule.Template = request.Template;
            rule.IsActive = request.IsActive;
            rule.Priority = request.Priority;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dto = new NotificationRuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                EventType = rule.EventType,
                NotificationType = rule.NotificationType,
                Category = rule.Category,
                Condition = rule.Condition,
                Template = rule.Template,
                IsActive = rule.IsActive,
                Priority = rule.Priority,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt,
                CreatedBy = rule.CreatedBy
            };

            return new ApiResponse<NotificationRuleDto>
            {
                Success = true,
                Data = dto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification rule {RuleId}", ruleId);
            return new ApiResponse<NotificationRuleDto>
            {
                Success = false,
                ErrorMessage = "Failed to update notification rule"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteNotificationRuleAsync(int ruleId)
    {
        try
        {
            var rule = await _context.NotificationRules.FindAsync(ruleId);
            if (rule == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Notification rule not found"
                };
            }

            _context.NotificationRules.Remove(rule);
            await _context.SaveChangesAsync();

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification rule {RuleId}", ruleId);
            return new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to delete notification rule"
            };
        }
    }

    public async Task<ApiResponse<bool>> ToggleNotificationRuleAsync(int ruleId)
    {
        try
        {
            var rule = await _context.NotificationRules.FindAsync(ruleId);
            if (rule == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "Notification rule not found"
                };
            }

            rule.IsActive = !rule.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle notification rule {RuleId}", ruleId);
            return new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to toggle notification rule"
            };
        }
    }

    public async Task TriggerStockLowNotificationAsync(object productObj)
    {
        try
        {
            var product = (Product)productObj;
            var eventType = "STOCK_LOW";
            var rules = await _ruleEngine.GetActiveRulesForEventAsync(eventType);
            
            foreach (var rule in rules)
            {
                var data = new { Product = product };
                if (await _ruleEngine.EvaluateConditionAsync(rule.Condition, data))
                {
                    var message = await _ruleEngine.ProcessTemplateAsync(rule.Template, data);
                    
                    // Get users who should receive this notification
                    var preferences = await _ruleEngine.GetUserPreferencesForEventAsync(eventType);
                    
                    foreach (var preference in preferences.Where(p => p.InAppEnabled))
                    {
                        var notification = new CreateNotificationRequest
                        {
                            Title = $"Low Stock Alert: {product.Name}",
                            Message = message,
                            Type = rule.NotificationType,
                            Category = rule.Category,
                            UserId = preference.UserId,
                            ProductId = product.Id,
                            ActionUrl = $"/products/{product.Id}",
                            ActionText = "View Product"
                        };
                        
                        var result = await CreateNotificationAsync(notification);
                        if (result.Success && result.Data != null)
                        {
                            // Real-time notification is already sent in CreateNotificationAsync
                            _logger.LogInformation("Sent real-time stock low notification to user {UserId}", preference.UserId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger stock low notification for product {ProductId}", ((Product)productObj).Id);
        }
    }

    public async Task TriggerStockOutNotificationAsync(object productObj)
    {
        try
        {
            var product = (Product)productObj;
            var eventType = "STOCK_OUT";
            var rules = await _ruleEngine.GetActiveRulesForEventAsync(eventType);
            
            foreach (var rule in rules)
            {
                var data = new { Product = product };
                if (await _ruleEngine.EvaluateConditionAsync(rule.Condition, data))
                {
                    var message = await _ruleEngine.ProcessTemplateAsync(rule.Template, data);
                    
                    var preferences = await _ruleEngine.GetUserPreferencesForEventAsync(eventType);
                    
                    foreach (var preference in preferences.Where(p => p.InAppEnabled))
                    {
                        var notification = new CreateNotificationRequest
                        {
                            Title = $"Out of Stock: {product.Name}",
                            Message = message,
                            Type = rule.NotificationType,
                            Category = rule.Category,
                            UserId = preference.UserId,
                            ProductId = product.Id,
                            ActionUrl = $"/products/{product.Id}",
                            ActionText = "View Product"
                        };
                        
                        var result = await CreateNotificationAsync(notification);
                        if (result.Success && result.Data != null)
                        {
                            // Real-time notification is already sent in CreateNotificationAsync
                            _logger.LogInformation("Sent real-time stock out notification to user {UserId}", preference.UserId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger stock out notification for product {ProductId}", ((Product)productObj).Id);
        }
    }

    public async Task TriggerTransactionNotificationAsync(object transactionObj)
    {
        try
        {
            var transaction = (InventoryTransaction)transactionObj;
            var eventType = "TRANSACTION_CREATED";
            var rules = await _ruleEngine.GetActiveRulesForEventAsync(eventType);
            
            foreach (var rule in rules)
            {
                var data = new { Transaction = transaction };
                if (await _ruleEngine.EvaluateConditionAsync(rule.Condition, data))
                {
                    var message = await _ruleEngine.ProcessTemplateAsync(rule.Template, data);
                    
                    var preferences = await _ruleEngine.GetUserPreferencesForEventAsync(eventType);
                    
                    foreach (var preference in preferences.Where(p => p.InAppEnabled))
                    {
                        var notification = new CreateNotificationRequest
                        {
                            Title = $"New Transaction: {transaction.Product?.Name ?? "Unknown Product"}",
                            Message = message,
                            Type = rule.NotificationType,
                            Category = rule.Category,
                            UserId = preference.UserId,
                            ProductId = transaction.ProductId,
                            TransactionId = transaction.Id,
                            ActionUrl = $"/transactions/{transaction.Id}",
                            ActionText = "View Transaction"
                        };
                        
                        var result = await CreateNotificationAsync(notification);
                        if (result.Success && result.Data != null)
                        {
                            // Real-time notification is already sent in CreateNotificationAsync
                            _logger.LogInformation("Sent real-time transaction notification to user {UserId}", preference.UserId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger transaction notification for transaction {TransactionId}", ((InventoryTransaction)transactionObj).Id);
        }
    }

    public async Task TriggerSystemNotificationAsync(string title, string message, string userId, string? actionUrl = null)
    {
        try
        {
            var notification = new CreateNotificationRequest
            {
                Title = title,
                Message = message,
                Type = "INFO",
                Category = "SYSTEM",
                UserId = userId,
                ActionUrl = actionUrl,
                ActionText = actionUrl != null ? "View Details" : null
            };
            
            var result = await CreateNotificationAsync(notification);
            if (result.Success && result.Data != null)
            {
                // Real-time notification is already sent in CreateNotificationAsync
                _logger.LogInformation("Sent real-time system notification to user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger system notification for user {UserId}", userId);
        }
    }

    public async Task<ApiResponse<bool>> SendBulkNotificationAsync(List<string> userIds, CreateNotificationRequest request)
    {
        try
        {
            var notifications = userIds.Select(userId => new DbNotification
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Category = request.Category,
                ActionUrl = request.ActionUrl,
                ActionText = request.ActionText,
                UserId = userId,
                ProductId = request.ProductId,
                TransactionId = request.TransactionId,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            // Send real-time notifications via SignalR
            var notificationDtos = notifications.Select(MapToDto).ToList();
            await _signalRService.SendNotificationToUsersAsync(userIds, notificationDtos.First());

            _logger.LogInformation("Sent bulk notification to {UserCount} users", userIds.Count);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk notification");
            return new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to send bulk notification"
            };
        }
    }

    public async Task<ApiResponse<bool>> CleanupExpiredNotificationsAsync()
    {
        try
        {
            var expiredNotifications = await _context.Notifications
                .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            _context.Notifications.RemoveRange(expiredNotifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired notifications", expiredNotifications.Count);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired notifications");
            return new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Failed to cleanup expired notifications"
            };
        }
    }

    private static NotificationDto MapToDto(DbNotification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Category = notification.Category,
            ActionUrl = notification.ActionUrl,
            ActionText = notification.ActionText,
            IsRead = notification.IsRead,
            IsArchived = notification.IsArchived,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            ExpiresAt = notification.ExpiresAt,
            UserId = notification.UserId,
            UserName = notification.UserName,
            ProductId = notification.ProductId,
            ProductName = notification.ProductName,
            TransactionId = notification.TransactionId
        };
    }
}
