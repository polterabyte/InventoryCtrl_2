# Fix Notification Service Registration
# This script adds the missing INotificationService registration to the client application

Write-Host "🔧 Fixing Notification Service Registration..." -ForegroundColor Yellow

# Read the current Program.cs file
$programPath = "src/Inventory.Web.Client/Program.cs"
$content = Get-Content $programPath -Raw

# Check if INotificationService is already registered
if ($content -match "INotificationService")
{
    Write-Host "✅ INotificationService is already registered" -ForegroundColor Green
    exit 0
}

# Add the missing service registration after the existing notification services
$oldText = @"
// Register notification and retry services
builder.Services.AddScoped<IUINotificationService, NotificationService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();
"@

$newText = @"
// Register notification and retry services
builder.Services.AddScoped<IUINotificationService, NotificationService>();
builder.Services.AddScoped<INotificationService, NotificationApiService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();
"@

$newContent = $content -replace [regex]::Escape($oldText), $newText

# Write the updated content back to the file
Set-Content -Path $programPath -Value $newContent -NoNewline

Write-Host "✅ Added INotificationService registration to Program.cs" -ForegroundColor Green

# Now we need to create the NotificationApiService implementation
Write-Host "🔧 Creating NotificationApiService implementation..." -ForegroundColor Yellow

$notificationApiServicePath = "src/Inventory.Web.Client/Services/NotificationApiService.cs"

$notificationApiServiceContent = @"
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Inventory.Web.Client.Services;

public class NotificationApiService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationApiService> _logger;

    public NotificationApiService(HttpClient httpClient, ILogger<NotificationApiService> logger)
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
            return JsonSerializer.Deserialize<ApiResponse<NotificationDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<NotificationDto> { Success = false };
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
            var response = await _httpClient.GetAsync($"/api/notifications/user/{userId}?page={page}&pageSize={pageSize}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<List<NotificationDto>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<List<NotificationDto>> { Success = false };
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
            return JsonSerializer.Deserialize<ApiResponse<NotificationDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<NotificationDto> { Success = false };
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
            var response = await _httpClient.PutAsync($"/api/notifications/{notificationId}/mark-read?userId={userId}", null);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<bool> { Success = false };
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
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<bool> { Success = false };
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
            var response = await _httpClient.PutAsync($"/api/notifications/{notificationId}/archive?userId={userId}", null);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<bool> { Success = false };
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
            var response = await _httpClient.DeleteAsync($"/api/notifications/{notificationId}?userId={userId}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<bool> { Success = false };
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
            var response = await _httpClient.GetAsync($"/api/notifications/stats?userId={userId}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<NotificationStatsDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<NotificationStatsDto> { Success = false };
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
            var response = await _httpClient.GetAsync($"/api/notifications/preferences?userId={userId}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<List<NotificationPreferenceDto>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<List<NotificationPreferenceDto>> { Success = false };
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
            var response = await _httpClient.PutAsJsonAsync($"/api/notifications/preferences?userId={userId}", request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<NotificationPreferenceDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<NotificationPreferenceDto> { Success = false };
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
            var response = await _httpClient.DeleteAsync($"/api/notifications/preferences?userId={userId}&eventType={eventType}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<bool> { Success = false };
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
            return JsonSerializer.Deserialize<ApiResponse<List<NotificationRuleDto>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<List<NotificationRuleDto>> { Success = false };
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
            return JsonSerializer.Deserialize<ApiResponse<NotificationRuleDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<NotificationRuleDto> { Success = false };
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
            return JsonSerializer.Deserialize<ApiResponse<NotificationRuleDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<NotificationRuleDto> { Success = false };
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
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<bool> { Success = false };
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
            return JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse<bool> { Success = false };
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
"@

Set-Content -Path $notificationApiServicePath -Value $notificationApiServiceContent

Write-Host "✅ Created NotificationApiService.cs" -ForegroundColor Green

Write-Host "🎉 Notification Service registration fix completed!" -ForegroundColor Green
Write-Host "The INotificationService is now properly registered and implemented." -ForegroundColor Cyan
