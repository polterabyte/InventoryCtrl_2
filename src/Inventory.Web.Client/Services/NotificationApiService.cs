using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http.Json;

namespace Inventory.Web.Client.Services;

public class NotificationClientService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationClientService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public NotificationClientService(HttpClient httpClient, ILogger<NotificationClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/notifications", request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<NotificationDto>>(content, JsonOptions) ?? new ApiResponse<NotificationDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification");
            return new ApiResponse<NotificationDto> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/notifications?page={page}&pageSize={pageSize}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Empty response from notifications API");
                return new ApiResponse<List<NotificationDto>> 
                { 
                    Success = false, 
                    ErrorMessage = "Empty response from server" 
                };
            }
            
            return JsonSerializer.Deserialize<ApiResponse<List<NotificationDto>>>(content, JsonOptions) ?? new ApiResponse<List<NotificationDto>> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user notifications");
            return new ApiResponse<List<NotificationDto>> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<NotificationDto>> GetNotificationAsync(int notificationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/notifications/{notificationId}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<NotificationDto>>(content, JsonOptions) ?? new ApiResponse<NotificationDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification");
            return new ApiResponse<NotificationDto> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, string userId)
    {
        try
        {
            var response = await _httpClient.PutAsync($"/api/notifications/{notificationId}/read", null);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions) ?? new ApiResponse<bool> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification as read");
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> MarkAllAsReadAsync(string userId)
    {
        try
        {
            var response = await _httpClient.PutAsync($"/api/notifications/mark-all-read?userId={userId}", null);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions) ?? new ApiResponse<bool> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read");
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> ArchiveNotificationAsync(int notificationId, string userId)
    {
        try
        {
            var response = await _httpClient.PutAsync($"/api/notifications/{notificationId}/archive", null);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions) ?? new ApiResponse<bool> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive notification");
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> DeleteNotificationAsync(int notificationId, string userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/notifications/{notificationId}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions) ?? new ApiResponse<bool> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification");
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<NotificationStatsDto>> GetNotificationStatsAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/notifications/stats");
            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Empty response from notification stats API");
                return new ApiResponse<NotificationStatsDto> 
                { 
                    Success = false, 
                    ErrorMessage = "Empty response from server" 
                };
            }
            
            return JsonSerializer.Deserialize<ApiResponse<NotificationStatsDto>>(content, JsonOptions) ?? new ApiResponse<NotificationStatsDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification stats");
            return new ApiResponse<NotificationStatsDto> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<List<NotificationPreferenceDto>>> GetUserPreferencesAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/notifications/preferences");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<List<NotificationPreferenceDto>>>(content, JsonOptions) ?? new ApiResponse<List<NotificationPreferenceDto>> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user preferences");
            return new ApiResponse<List<NotificationPreferenceDto>> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<NotificationPreferenceDto>> UpdatePreferenceAsync(string userId, UpdateNotificationPreferenceRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("/api/notifications/preferences", request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<NotificationPreferenceDto>>(content, JsonOptions) ?? new ApiResponse<NotificationPreferenceDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update preference");
            return new ApiResponse<NotificationPreferenceDto> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> DeletePreferenceAsync(string userId, string eventType)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/notifications/preferences/{eventType}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions) ?? new ApiResponse<bool> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete preference");
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<List<NotificationRuleDto>>> GetNotificationRulesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/notifications/rules");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<List<NotificationRuleDto>>>(content, JsonOptions) ?? new ApiResponse<List<NotificationRuleDto>> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification rules");
            return new ApiResponse<List<NotificationRuleDto>> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<NotificationRuleDto>> CreateNotificationRuleAsync(CreateNotificationRuleRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/notifications/rules", request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<NotificationRuleDto>>(content, JsonOptions) ?? new ApiResponse<NotificationRuleDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification rule");
            return new ApiResponse<NotificationRuleDto> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<NotificationRuleDto>> UpdateNotificationRuleAsync(int ruleId, CreateNotificationRuleRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/notifications/rules/{ruleId}", request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<NotificationRuleDto>>(content, JsonOptions) ?? new ApiResponse<NotificationRuleDto> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification rule");
            return new ApiResponse<NotificationRuleDto> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> DeleteNotificationRuleAsync(int ruleId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/notifications/rules/{ruleId}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions) ?? new ApiResponse<bool> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification rule");
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> ToggleNotificationRuleAsync(int ruleId)
    {
        try
        {
            var response = await _httpClient.PutAsync($"/api/notifications/rules/{ruleId}/toggle", null);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions) ?? new ApiResponse<bool> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle notification rule");
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task TriggerStockLowNotificationAsync(object product)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/api/notifications/trigger/stock-low", product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger stock low notification");
        }
    }

    public async Task TriggerStockOutNotificationAsync(object product)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/api/notifications/trigger/stock-out", product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger stock out notification");
        }
    }

    public async Task TriggerTransactionNotificationAsync(object transaction)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/api/notifications/trigger/transaction", transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger transaction notification");
        }
    }

    public async Task TriggerSystemNotificationAsync(string title, string message, string userId, string? actionUrl = null)
    {
        try
        {
            var request = new { title, message, userId, actionUrl };
            await _httpClient.PostAsJsonAsync("/api/notifications/trigger/system", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger system notification");
        }
    }

    public async Task<ApiResponse<bool>> SendBulkNotificationAsync(List<string> userIds, CreateNotificationRequest request)
    {
        try
        {
            var bulkRequest = new { userIds, request };
            var response = await _httpClient.PostAsJsonAsync("/api/notifications/bulk", bulkRequest);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<bool> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk notification");
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> CleanupExpiredNotificationsAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/notifications/cleanup", null);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<bool> { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired notifications");
            return new ApiResponse<bool> { Success = false, ErrorMessage = ex.Message };
        }
    }
}
