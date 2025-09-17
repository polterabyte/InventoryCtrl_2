using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using System.Security.Claims;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<List<NotificationDto>>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var result = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user notifications");
            return StatusCode(500, new ApiResponse<List<NotificationDto>>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(int id)
    {
        try
        {
            var result = await _notificationService.GetNotificationAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification {NotificationId}", id);
            return StatusCode(500, new ApiResponse<NotificationDto>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<NotificationDto>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            // Set user ID if not provided
            if (string.IsNullOrEmpty(request.UserId))
            {
                request.UserId = userId;
            }

            var result = await _notificationService.CreateNotificationAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetNotification), new { id = result.Data!.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification");
            return StatusCode(500, new ApiResponse<NotificationDto>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var result = await _notificationService.MarkAsReadAsync(id, userId);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var result = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPut("{id}/archive")]
    public async Task<IActionResult> ArchiveNotification(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var result = await _notificationService.ArchiveNotificationAsync(id, userId);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive notification {NotificationId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var result = await _notificationService.DeleteNotificationAsync(id, userId);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetNotificationStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<NotificationStatsDto>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var result = await _notificationService.GetNotificationStatsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification stats");
            return StatusCode(500, new ApiResponse<NotificationStatsDto>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetUserPreferences()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<List<NotificationPreferenceDto>>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var result = await _notificationService.GetUserPreferencesAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user preferences");
            return StatusCode(500, new ApiResponse<List<NotificationPreferenceDto>>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreference([FromBody] UpdateNotificationPreferenceRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<NotificationPreferenceDto>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var result = await _notificationService.UpdatePreferenceAsync(userId, request);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update preference");
            return StatusCode(500, new ApiResponse<NotificationPreferenceDto>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpDelete("preferences/{eventType}")]
    public async Task<IActionResult> DeletePreference(string eventType)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    ErrorMessage = "User not authenticated"
                });
            }

            var result = await _notificationService.DeletePreferenceAsync(userId, eventType);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete preference for event {EventType}", eventType);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpGet("rules")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetNotificationRules()
    {
        try
        {
            var result = await _notificationService.GetNotificationRulesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification rules");
            return StatusCode(500, new ApiResponse<List<NotificationRuleDto>>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPost("rules")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateNotificationRule([FromBody] CreateNotificationRuleRequest request)
    {
        try
        {
            var result = await _notificationService.CreateNotificationRuleAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetNotificationRules), result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification rule");
            return StatusCode(500, new ApiResponse<NotificationRuleDto>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPut("rules/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateNotificationRule(int id, [FromBody] CreateNotificationRuleRequest request)
    {
        try
        {
            var result = await _notificationService.UpdateNotificationRuleAsync(id, request);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification rule {RuleId}", id);
            return StatusCode(500, new ApiResponse<NotificationRuleDto>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpDelete("rules/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteNotificationRule(int id)
    {
        try
        {
            var result = await _notificationService.DeleteNotificationRuleAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification rule {RuleId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPut("rules/{id}/toggle")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ToggleNotificationRule(int id)
    {
        try
        {
            var result = await _notificationService.ToggleNotificationRuleAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle notification rule {RuleId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SendBulkNotification([FromBody] BulkNotificationRequest request)
    {
        try
        {
            var result = await _notificationService.SendBulkNotificationAsync(request.UserIds, request.Notification);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk notification");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpPost("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CleanupExpiredNotifications()
    {
        try
        {
            var result = await _notificationService.CleanupExpiredNotificationsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired notifications");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

public class BulkNotificationRequest
{
    public List<string> UserIds { get; set; } = new();
    public CreateNotificationRequest Notification { get; set; } = new();
}
