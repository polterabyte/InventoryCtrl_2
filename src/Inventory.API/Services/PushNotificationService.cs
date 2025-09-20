using Microsoft.EntityFrameworkCore;
using WebPush;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Options;
using Inventory.API.Configuration;

namespace Inventory.API.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly VapidConfiguration _vapidConfig;
    private readonly WebPushClient _webPushClient;

    public PushNotificationService(
        AppDbContext context,
        ILogger<PushNotificationService> logger,
        IOptions<VapidConfiguration> vapidConfig)
    {
        _context = context;
        _logger = logger;
        _vapidConfig = vapidConfig.Value;
        _webPushClient = new WebPushClient();
        
        // Configure VAPID details
        _webPushClient.SetVapidDetails(
            _vapidConfig.Subject,
            _vapidConfig.PublicKey,
            _vapidConfig.PrivateKey
        );
    }

    public async Task<bool> SubscribeAsync(string userId, CreatePushSubscriptionDto subscription)
    {
        try
        {
            // Check if subscription already exists
            var existingSubscription = await _context.PushSubscriptions
                .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.Endpoint == subscription.Endpoint);

            if (existingSubscription != null)
            {
                // Update existing subscription
                existingSubscription.P256dh = subscription.P256dh;
                existingSubscription.Auth = subscription.Auth;
                existingSubscription.LastUsedAt = DateTime.UtcNow;
                existingSubscription.IsActive = true;
            }
            else
            {
                // Create new subscription
                var newSubscription = new Models.PushSubscription
                {
                    UserId = userId,
                    Endpoint = subscription.Endpoint,
                    P256dh = subscription.P256dh,
                    Auth = subscription.Auth,
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.PushSubscriptions.Add(newSubscription);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Push subscription created/updated for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe user {UserId} to push notifications", userId);
            return false;
        }
    }

    public async Task<bool> UnsubscribeAsync(string userId, string endpoint)
    {
        try
        {
            var subscription = await _context.PushSubscriptions
                .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.Endpoint == endpoint);

            if (subscription != null)
            {
                subscription.IsActive = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Push subscription deactivated for user {UserId}", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe user {UserId} from push notifications", userId);
            return false;
        }
    }

    public async Task<bool> UnsubscribeAllAsync(string userId)
    {
        try
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(ps => ps.UserId == userId && ps.IsActive)
                .ToListAsync();

            foreach (var subscription in subscriptions)
            {
                subscription.IsActive = false;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("All push subscriptions deactivated for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe all for user {UserId}", userId);
            return false;
        }
    }

    public async Task<List<PushSubscriptionDto>> GetUserSubscriptionsAsync(string userId)
    {
        try
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(ps => ps.UserId == userId && ps.IsActive)
                .Select(ps => new PushSubscriptionDto
                {
                    Id = ps.Id,
                    UserId = ps.UserId,
                    Endpoint = ps.Endpoint,
                    P256dh = ps.P256dh,
                    Auth = ps.Auth,
                    CreatedAt = ps.CreatedAt,
                    LastUsedAt = ps.LastUsedAt,
                    IsActive = ps.IsActive,
                    UserAgent = ps.UserAgent,
                    IpAddress = ps.IpAddress
                })
                .ToListAsync();

            return subscriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscriptions for user {UserId}", userId);
            return new List<PushSubscriptionDto>();
        }
    }

    public async Task<bool> SendNotificationAsync(string userId, PushNotificationDto notification)
    {
        try
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(ps => ps.UserId == userId && ps.IsActive)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                _logger.LogWarning("No active push subscriptions found for user {UserId}", userId);
                return false;
            }

            var successCount = 0;
            foreach (var subscription in subscriptions)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth
                    );

                    var payload = System.Text.Json.JsonSerializer.Serialize(notification);
                    await _webPushClient.SendNotificationAsync(pushSubscription, payload);
                    
                    // Update last used timestamp
                    subscription.LastUsedAt = DateTime.UtcNow;
                    successCount++;
                }
                catch (WebPushException ex)
                {
                    _logger.LogWarning(ex, "Failed to send push notification to subscription {SubscriptionId} for user {UserId}. Status: {StatusCode}", 
                        subscription.Id, userId, ex.StatusCode);
                    
                    // If subscription is invalid, mark as inactive
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone || 
                        ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        subscription.IsActive = false;
                    }
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Sent push notification to {SuccessCount}/{TotalCount} subscriptions for user {UserId}", 
                successCount, subscriptions.Count, userId);
            
            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendNotificationToAllAsync(PushNotificationDto notification)
    {
        try
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(ps => ps.IsActive)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                _logger.LogWarning("No active push subscriptions found");
                return false;
            }

            var successCount = 0;
            foreach (var subscription in subscriptions)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth
                    );

                    var payload = System.Text.Json.JsonSerializer.Serialize(notification);
                    await _webPushClient.SendNotificationAsync(pushSubscription, payload);
                    
                    subscription.LastUsedAt = DateTime.UtcNow;
                    successCount++;
                }
                catch (WebPushException ex)
                {
                    _logger.LogWarning(ex, "Failed to send push notification to subscription {SubscriptionId}. Status: {StatusCode}", 
                        subscription.Id, ex.StatusCode);
                    
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone || 
                        ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        subscription.IsActive = false;
                    }
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Sent push notification to {SuccessCount}/{TotalCount} subscriptions", 
                successCount, subscriptions.Count);
            
            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to all users");
            return false;
        }
    }

    public async Task<bool> SendNotificationToSubscriptionsAsync(List<string> userIds, PushNotificationDto notification)
    {
        try
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(ps => userIds.Contains(ps.UserId) && ps.IsActive)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                _logger.LogWarning("No active push subscriptions found for specified users");
                return false;
            }

            var successCount = 0;
            foreach (var subscription in subscriptions)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth
                    );

                    var payload = System.Text.Json.JsonSerializer.Serialize(notification);
                    await _webPushClient.SendNotificationAsync(pushSubscription, payload);
                    
                    subscription.LastUsedAt = DateTime.UtcNow;
                    successCount++;
                }
                catch (WebPushException ex)
                {
                    _logger.LogWarning(ex, "Failed to send push notification to subscription {SubscriptionId}. Status: {StatusCode}", 
                        subscription.Id, ex.StatusCode);
                    
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone || 
                        ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        subscription.IsActive = false;
                    }
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Sent push notification to {SuccessCount}/{TotalCount} subscriptions for specified users", 
                successCount, subscriptions.Count);
            
            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to specified users");
            return false;
        }
    }

    public async Task<bool> IsUserSubscribedAsync(string userId)
    {
        try
        {
            return await _context.PushSubscriptions
                .AnyAsync(ps => ps.UserId == userId && ps.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check subscription status for user {UserId}", userId);
            return false;
        }
    }

    public async Task<int> GetActiveSubscriptionsCountAsync()
    {
        try
        {
            return await _context.PushSubscriptions
                .CountAsync(ps => ps.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active subscriptions count");
            return 0;
        }
    }
}
