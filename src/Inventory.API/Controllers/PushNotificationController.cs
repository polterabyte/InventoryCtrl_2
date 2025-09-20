using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using System.Security.Claims;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushNotificationController : ControllerBase
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<PushNotificationController> _logger;

    public PushNotificationController(
        IPushNotificationService pushNotificationService,
        ILogger<PushNotificationController> logger)
    {
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] CreatePushSubscriptionDto subscription)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            // Add user agent and IP address for tracking
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var success = await _pushNotificationService.SubscribeAsync(userId, subscription);
            
            if (success)
            {
                _logger.LogInformation("User {UserId} subscribed to push notifications", userId);
                return Ok(new { success = true, message = "Successfully subscribed to push notifications" });
            }

            return BadRequest(new { success = false, message = "Failed to subscribe to push notifications" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to push notifications");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            var success = await _pushNotificationService.UnsubscribeAsync(userId, request.Endpoint);
            
            if (success)
            {
                _logger.LogInformation("User {UserId} unsubscribed from push notifications", userId);
                return Ok(new { success = true, message = "Successfully unsubscribed from push notifications" });
            }

            return BadRequest(new { success = false, message = "Failed to unsubscribe from push notifications" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from push notifications");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("unsubscribe-all")]
    public async Task<IActionResult> UnsubscribeAll()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            var success = await _pushNotificationService.UnsubscribeAllAsync(userId);
            
            if (success)
            {
                _logger.LogInformation("User {UserId} unsubscribed from all push notifications", userId);
                return Ok(new { success = true, message = "Successfully unsubscribed from all push notifications" });
            }

            return BadRequest(new { success = false, message = "Failed to unsubscribe from all push notifications" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from all push notifications");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            var subscriptions = await _pushNotificationService.GetUserSubscriptionsAsync(userId);
            return Ok(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting push subscriptions");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("is-subscribed")]
    public async Task<IActionResult> IsSubscribed()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            var isSubscribed = await _pushNotificationService.IsUserSubscribedAsync(userId);
            return Ok(new { isSubscribed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subscription status");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("send")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            var success = false;
            
            if (request.UserIds != null && request.UserIds.Any())
            {
                success = await _pushNotificationService.SendNotificationToSubscriptionsAsync(request.UserIds, request.Notification);
            }
            else
            {
                success = await _pushNotificationService.SendNotificationToAllAsync(request.Notification);
            }

            if (success)
            {
                return Ok(new { success = true, message = "Notification sent successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to send notification" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var activeSubscriptions = await _pushNotificationService.GetActiveSubscriptionsCountAsync();
            return Ok(new { activeSubscriptions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting push notification stats");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

public class UnsubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
}

public class SendNotificationRequest
{
    public PushNotificationDto Notification { get; set; } = new();
    public List<string>? UserIds { get; set; }
}
