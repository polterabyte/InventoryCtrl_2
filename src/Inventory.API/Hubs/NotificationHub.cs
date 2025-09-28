using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Inventory.API.Models;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Inventory.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly AppDbContext _context;
    private static readonly ConcurrentDictionary<string, string> _userConnections = new();

    public NotificationHub(ILogger<NotificationHub> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.TryAdd(Context.ConnectionId, userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
                
                // Save connection to database
                await SaveConnectionToDatabase(userId);
                
                _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
                
                // Notify user about successful connection
                await Clients.Caller.SendAsync("connectionEstablished", new
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Connection attempt without valid user ID for connection {ConnectionId}", Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection establishment for connection {ConnectionId}", Context.ConnectionId);
            throw; // Re-throw to let SignalR handle the connection failure
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.TryRemove(Context.ConnectionId, out _);
                
                // Mark connection as inactive in database
                await MarkConnectionAsInactive(Context.ConnectionId);
                
                var logLevel = exception != null ? LogLevel.Warning : LogLevel.Information;
                _logger.Log(logLevel, exception, "User {UserId} disconnected from connection {ConnectionId}", userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("Disconnection attempt without valid user ID for connection {ConnectionId}", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection for connection {ConnectionId}", Context.ConnectionId);
            // Don't re-throw here as it's already a disconnection scenario
        }
    }

    public async Task JoinGroup(string groupName)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await UpdateConnectionActivity(Context.ConnectionId);
            _logger.LogInformation("User {UserId} joined group {GroupName}", GetUserId(), groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining group {GroupName} for user {UserId}", groupName, GetUserId());
            throw;
        }
    }

    public async Task LeaveGroup(string groupName)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await UpdateConnectionActivity(Context.ConnectionId);
            _logger.LogInformation("User {UserId} left group {GroupName}", GetUserId(), groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving group {GroupName} for user {UserId}", groupName, GetUserId());
            throw;
        }
    }

    public async Task SubscribeToNotifications(string notificationType)
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Notifications_{notificationType}");
                await UpdateConnectionSubscriptions(notificationType, true);
                _logger.LogInformation("User {UserId} subscribed to {NotificationType} notifications", userId, notificationType);
            }
            else
            {
                _logger.LogWarning("Subscription attempt without valid user ID for notification type {NotificationType}", notificationType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to {NotificationType} notifications for user {UserId}", notificationType, GetUserId());
            throw;
        }
    }

    public async Task UnsubscribeFromNotifications(string notificationType)
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Notifications_{notificationType}");
                await UpdateConnectionSubscriptions(notificationType, false);
                _logger.LogInformation("User {UserId} unsubscribed from {NotificationType} notifications", userId, notificationType);
            }
            else
            {
                _logger.LogWarning("Unsubscription attempt without valid user ID for notification type {NotificationType}", notificationType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from {NotificationType} notifications for user {UserId}", notificationType, GetUserId());
            throw;
        }
    }

    public static string? GetUserIdForConnection(string connectionId)
    {
        return _userConnections.TryGetValue(connectionId, out var userId) ? userId : null;
    }

    public static IEnumerable<string> GetConnectionsForUser(string userId)
    {
        return _userConnections.Where(kvp => kvp.Value == userId).Select(kvp => kvp.Key);
    }

    public static IEnumerable<string> GetAllConnections()
    {
        return _userConnections.Keys;
    }

    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private async Task SaveConnectionToDatabase(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].FirstOrDefault();
            // Truncate UserAgent if too long
            if (!string.IsNullOrEmpty(userAgent) && userAgent.Length > 500)
            {
                userAgent = userAgent.Substring(0, 500);
            }
            var ipAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();

            var connection = new SignalRConnection
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                UserName = user?.UserName,
                UserRole = userRole,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                ConnectedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.SignalRConnections.Add(connection);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save SignalR connection to database for user {UserId}", userId);
        }
    }

    private async Task MarkConnectionAsInactive(string connectionId)
    {
        try
        {
            var connection = await _context.SignalRConnections
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);

            if (connection != null)
            {
                connection.IsActive = false;
                connection.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark SignalR connection as inactive for connection {ConnectionId}", connectionId);
        }
    }

    private async Task UpdateConnectionActivity(string connectionId)
    {
        try
        {
            var connection = await _context.SignalRConnections
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);

            if (connection != null)
            {
                connection.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update SignalR connection activity for connection {ConnectionId}", connectionId);
        }
    }

    private async Task UpdateConnectionSubscriptions(string notificationType, bool isSubscribing)
    {
        try
        {
            var connection = await _context.SignalRConnections
                .FirstOrDefaultAsync(c => c.ConnectionId == Context.ConnectionId);

            if (connection != null)
            {
                var subscribedTypes = new List<string>();
                
                if (!string.IsNullOrEmpty(connection.SubscribedNotificationTypes))
                {
                    subscribedTypes = JsonSerializer.Deserialize<List<string>>(connection.SubscribedNotificationTypes) ?? new List<string>();
                }

                if (isSubscribing && !subscribedTypes.Contains(notificationType))
                {
                    subscribedTypes.Add(notificationType);
                }
                else if (!isSubscribing && subscribedTypes.Contains(notificationType))
                {
                    subscribedTypes.Remove(notificationType);
                }

                connection.SubscribedNotificationTypes = JsonSerializer.Serialize(subscribedTypes);
                connection.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update SignalR connection subscriptions for connection {ConnectionId}", Context.ConnectionId);
        }
    }
    
    /// <summary>
    /// Cleans up inactive connections that have been disconnected for more than 24 hours
    /// </summary>
    public static async Task CleanupInactiveConnections(AppDbContext context, ILogger logger)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddDays(-1);
            var inactiveConnections = await context.SignalRConnections
                .Where(c => !c.IsActive && c.LastActivityAt < cutoffTime)
                .ToListAsync();
            
            if (inactiveConnections.Any())
            {
                context.SignalRConnections.RemoveRange(inactiveConnections);
                await context.SaveChangesAsync();
                logger.LogInformation("Cleaned up {Count} inactive SignalR connections", inactiveConnections.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clean up inactive SignalR connections");
        }
    }
}
