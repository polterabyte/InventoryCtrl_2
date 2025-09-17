using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Inventory.Shared.Services;

public class NotificationApiService : BaseApiService, INotificationApiService
{
    public NotificationApiService(HttpClient httpClient, ILogger<NotificationApiService> logger) 
        : base(httpClient, ApiEndpoints.BaseUrl, logger)
    {
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(int page = 1, int pageSize = 20)
    {
        return await GetAsync<List<NotificationDto>>($"{ApiEndpoints.Notifications}?page={page}&pageSize={pageSize}");
    }

    public async Task<ApiResponse<NotificationDto>> GetNotificationAsync(int notificationId)
    {
        return await GetAsync<NotificationDto>(ApiEndpoints.NotificationById.Replace("{id}", notificationId.ToString()));
    }

    public async Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationRequest request)
    {
        return await PostAsync<NotificationDto>(ApiEndpoints.Notifications, request);
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId)
    {
        return await PutAsync<bool>($"{ApiEndpoints.NotificationById.Replace("{id}", notificationId.ToString())}/read", null);
    }

    public async Task<ApiResponse<bool>> MarkAllAsReadAsync()
    {
        return await PutAsync<bool>($"{ApiEndpoints.Notifications}/mark-all-read", null);
    }

    public async Task<ApiResponse<bool>> ArchiveNotificationAsync(int notificationId)
    {
        return await PutAsync<bool>($"{ApiEndpoints.NotificationById.Replace("{id}", notificationId.ToString())}/archive", null);
    }

    public async Task<ApiResponse<bool>> DeleteNotificationAsync(int notificationId)
    {
        return await DeleteAsync(ApiEndpoints.NotificationById.Replace("{id}", notificationId.ToString()));
    }

    public async Task<ApiResponse<NotificationStatsDto>> GetNotificationStatsAsync()
    {
        return await GetAsync<NotificationStatsDto>(ApiEndpoints.NotificationStats);
    }

    public async Task<ApiResponse<List<NotificationPreferenceDto>>> GetUserPreferencesAsync()
    {
        return await GetAsync<List<NotificationPreferenceDto>>(ApiEndpoints.NotificationPreferences);
    }

    public async Task<ApiResponse<NotificationPreferenceDto>> UpdatePreferenceAsync(UpdateNotificationPreferenceRequest request)
    {
        return await PutAsync<NotificationPreferenceDto>(ApiEndpoints.NotificationPreferences, request);
    }

    public async Task<ApiResponse<bool>> DeletePreferenceAsync(string eventType)
    {
        return await DeleteAsync($"{ApiEndpoints.NotificationPreferences}/{eventType}");
    }

    public async Task<ApiResponse<List<NotificationRuleDto>>> GetNotificationRulesAsync()
    {
        return await GetAsync<List<NotificationRuleDto>>(ApiEndpoints.NotificationRules);
    }

    public async Task<ApiResponse<NotificationRuleDto>> CreateNotificationRuleAsync(CreateNotificationRuleRequest request)
    {
        return await PostAsync<NotificationRuleDto>(ApiEndpoints.NotificationRules, request);
    }

    public async Task<ApiResponse<NotificationRuleDto>> UpdateNotificationRuleAsync(int ruleId, CreateNotificationRuleRequest request)
    {
        return await PutAsync<NotificationRuleDto>($"{ApiEndpoints.NotificationRuleById.Replace("{id}", ruleId.ToString())}", request);
    }

    public async Task<ApiResponse<bool>> DeleteNotificationRuleAsync(int ruleId)
    {
        return await DeleteAsync(ApiEndpoints.NotificationRuleById.Replace("{id}", ruleId.ToString()));
    }

    public async Task<ApiResponse<bool>> ToggleNotificationRuleAsync(int ruleId)
    {
        return await PutAsync<bool>($"{ApiEndpoints.NotificationRuleById.Replace("{id}", ruleId.ToString())}/toggle", null);
    }
}

public interface INotificationApiService
{
    Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(int page = 1, int pageSize = 20);
    Task<ApiResponse<NotificationDto>> GetNotificationAsync(int notificationId);
    Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationRequest request);
    Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId);
    Task<ApiResponse<bool>> MarkAllAsReadAsync();
    Task<ApiResponse<bool>> ArchiveNotificationAsync(int notificationId);
    Task<ApiResponse<bool>> DeleteNotificationAsync(int notificationId);
    Task<ApiResponse<NotificationStatsDto>> GetNotificationStatsAsync();
    Task<ApiResponse<List<NotificationPreferenceDto>>> GetUserPreferencesAsync();
    Task<ApiResponse<NotificationPreferenceDto>> UpdatePreferenceAsync(UpdateNotificationPreferenceRequest request);
    Task<ApiResponse<bool>> DeletePreferenceAsync(string eventType);
    Task<ApiResponse<List<NotificationRuleDto>>> GetNotificationRulesAsync();
    Task<ApiResponse<NotificationRuleDto>> CreateNotificationRuleAsync(CreateNotificationRuleRequest request);
    Task<ApiResponse<NotificationRuleDto>> UpdateNotificationRuleAsync(int ruleId, CreateNotificationRuleRequest request);
    Task<ApiResponse<bool>> DeleteNotificationRuleAsync(int ruleId);
    Task<ApiResponse<bool>> ToggleNotificationRuleAsync(int ruleId);
}
