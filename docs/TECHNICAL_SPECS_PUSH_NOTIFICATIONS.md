# Техническая спецификация: Push Notifications

## 🎯 Обзор

Данный документ содержит детальную техническую спецификацию для реализации browser push уведомлений в Inventory Control System v2.

## 📋 Цели

- Обеспечить доставку уведомлений даже когда пользователь не активен в браузере
- Улучшить пользовательский опыт через мгновенные уведомления
- Интегрировать с существующей системой уведомлений
- Обеспечить безопасность и приватность пользователей

## 🏗 Архитектура

### Компоненты системы

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Blazor UI     │    │   Push Service  │    │   Browser       │
│                 │    │                 │    │   Service       │
│ - Subscribe UI  │◄──►│ - VAPID Keys    │◄──►│   Worker        │
│ - Settings UI   │    │ - Send Push     │    │ - Handle Push   │
│                 │    │ - Retry Logic   │    │ - Show Toast    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Database      │    │   SignalR Hub   │    │   Push          │
│                 │    │                 │    │   Subscription  │
│ - Subscriptions │    │ - Real-time     │    │   Storage       │
│ - User Prefs    │    │   Updates       │    │ - Local Storage │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 📦 Зависимости

### NuGet пакеты

```xml
<!-- Backend -->
<PackageReference Include="Microsoft.AspNetCore.WebPush" Version="1.0.11" />
<PackageReference Include="WebPush" Version="1.0.11" />

<!-- Frontend -->
<!-- Уже включено в проект -->
<PackageReference Include="Microsoft.JSInterop" Version="9.0.0" />
```

### JavaScript библиотеки

```json
{
  "dependencies": {
    "web-push": "^3.6.6"
  }
}
```

## 🗄 Модели данных

### PushSubscription Entity

```csharp
public class PushSubscription
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string P256dhKey { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
}
```

### PushNotificationRequest DTO

```csharp
public class PushNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public bool RequireInteraction { get; set; } = false;
    public bool Silent { get; set; } = false;
    public int? Ttl { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Actions { get; set; } = new();
}
```

## 🔧 Сервисы

### IPushNotificationService

```csharp
public interface IPushNotificationService
{
    Task<bool> SubscribeAsync(string userId, PushSubscriptionDto subscription);
    Task<bool> UnsubscribeAsync(string userId, string endpoint);
    Task<bool> SendToUserAsync(string userId, PushNotificationRequest request);
    Task<bool> SendToAllAsync(PushNotificationRequest request);
    Task<bool> SendToRoleAsync(string role, PushNotificationRequest request);
    Task<List<PushSubscriptionDto>> GetUserSubscriptionsAsync(string userId);
    Task<bool> IsSubscribedAsync(string userId);
    Task<PushSubscriptionStats> GetStatsAsync();
}
```

### PushNotificationService Implementation

```csharp
public class PushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly WebPushClient _webPushClient;
    private readonly VapidDetails _vapidDetails;

    public PushNotificationService(
        AppDbContext context,
        ILogger<PushNotificationService> logger,
        IHubContext<NotificationHub> hubContext,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
        _webPushClient = new WebPushClient();
        
        _vapidDetails = new VapidDetails(
            subject: configuration["PushNotifications:Subject"],
            publicKey: configuration["PushNotifications:PublicKey"],
            privateKey: configuration["PushNotifications:PrivateKey"]
        );
    }

    public async Task<bool> SubscribeAsync(string userId, PushSubscriptionDto subscription)
    {
        try
        {
            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == subscription.Endpoint);

            if (existing != null)
            {
                existing.IsActive = true;
                existing.LastUsedAt = DateTime.UtcNow;
            }
            else
            {
                var newSubscription = new PushSubscription
                {
                    UserId = userId,
                    Endpoint = subscription.Endpoint,
                    P256dhKey = subscription.Keys.P256dh,
                    AuthKey = subscription.Keys.Auth,
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    UserAgent = subscription.UserAgent ?? string.Empty,
                    Browser = subscription.Browser ?? string.Empty,
                    DeviceType = subscription.DeviceType ?? string.Empty
                };

                _context.PushSubscriptions.Add(newSubscription);
            }

            await _context.SaveChangesAsync();
            
            // Уведомить через SignalR о новой подписке
            await _hubContext.Clients.User(userId).SendAsync("PushSubscriptionUpdated", true);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe user {UserId} to push notifications", userId);
            return false;
        }
    }

    public async Task<bool> SendToUserAsync(string userId, PushNotificationRequest request)
    {
        try
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            var successCount = 0;
            foreach (var subscription in subscriptions)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dhKey,
                        subscription.AuthKey
                    );

                    var payload = JsonSerializer.Serialize(new
                    {
                        title = request.Title,
                        body = request.Body,
                        icon = request.Icon,
                        badge = request.Badge,
                        image = request.Image,
                        tag = request.Tag,
                        requireInteraction = request.RequireInteraction,
                        silent = request.Silent,
                        data = request.Data,
                        actions = request.Actions
                    });

                    await _webPushClient.SendNotificationAsync(
                        pushSubscription,
                        payload,
                        _vapidDetails
                    );

                    subscription.LastUsedAt = DateTime.UtcNow;
                    successCount++;
                }
                catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // Подписка больше не действительна
                    subscription.IsActive = false;
                    _logger.LogWarning("Push subscription for user {UserId} is no longer valid", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
                }
            }

            await _context.SaveChangesAsync();
            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notifications to user {UserId}", userId);
            return false;
        }
    }

    // Другие методы...
}
```

## 🌐 API Endpoints

### PushSubscriptionController

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushSubscriptionController : ControllerBase
{
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<PushSubscriptionController> _logger;

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto subscription)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _pushService.SubscribeAsync(userId, subscription);
            return Ok(new ApiResponse<bool>
            {
                Success = result,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe user to push notifications");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
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
                return Unauthorized();
            }

            var result = await _pushService.UnsubscribeAsync(userId, request.Endpoint);
            return Ok(new ApiResponse<bool>
            {
                Success = result,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe user from push notifications");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var isSubscribed = await _pushService.IsSubscribedAsync(userId);
            var subscriptions = await _pushService.GetUserSubscriptionsAsync(userId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    IsSubscribed = isSubscribed,
                    Subscriptions = subscriptions
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get push subscription status");
            return StatusCode(500, new ApiResponse<object>
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
```

## 🎨 Frontend компоненты

### PushNotificationManager.razor

```razor
@using Microsoft.JSInterop
@inject IJSRuntime JSRuntime
@inject IPushNotificationApiService PushApiService
@inject ILogger<PushNotificationManager> Logger

<div class="push-notification-settings">
    <div class="form-check">
        <input class="form-check-input" type="checkbox" @bind="isPushEnabled" 
               @onchange="HandlePushToggle" id="pushNotifications">
        <label class="form-check-label" for="pushNotifications">
            Разрешить push уведомления
        </label>
    </div>
    
    @if (isPushEnabled && !isSubscribed)
    {
        <button class="btn btn-primary btn-sm mt-2" @onclick="SubscribeToPush">
            Подписаться на уведомления
        </button>
    }
    
    @if (isSubscribed)
    {
        <div class="alert alert-success mt-2">
            <i class="fas fa-check-circle"></i>
            Push уведомления включены
        </div>
        <button class="btn btn-outline-danger btn-sm mt-2" @onclick="UnsubscribeFromPush">
            Отключить уведомления
        </button>
    }
    
    @if (browserSupport == PushSupport.NotSupported)
    {
        <div class="alert alert-warning mt-2">
            <i class="fas fa-exclamation-triangle"></i>
            Ваш браузер не поддерживает push уведомления
        </div>
    }
</div>

@code {
    private bool isPushEnabled = false;
    private bool isSubscribed = false;
    private PushSupport browserSupport = PushSupport.Unknown;
    private string? currentEndpoint;

    protected override async Task OnInitializedAsync()
    {
        await CheckBrowserSupport();
        await CheckSubscriptionStatus();
    }

    private async Task CheckBrowserSupport()
    {
        try
        {
            browserSupport = await JSRuntime.InvokeAsync<PushSupport>("PushNotifications.checkSupport");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check browser support for push notifications");
            browserSupport = PushSupport.NotSupported;
        }
    }

    private async Task CheckSubscriptionStatus()
    {
        try
        {
            var response = await PushApiService.GetSubscriptionStatusAsync();
            if (response.Success && response.Data != null)
            {
                isSubscribed = response.Data.IsSubscribed;
                currentEndpoint = response.Data.Subscriptions?.FirstOrDefault()?.Endpoint;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check push subscription status");
        }
    }

    private async Task HandlePushToggle(ChangeEventArgs e)
    {
        isPushEnabled = (bool)e.Value!;
        if (isPushEnabled && browserSupport == PushSupport.Supported)
        {
            await SubscribeToPush();
        }
    }

    private async Task SubscribeToPush()
    {
        try
        {
            var subscription = await JSRuntime.InvokeAsync<PushSubscriptionDto>("PushNotifications.subscribe");
            if (subscription != null)
            {
                var response = await PushApiService.SubscribeAsync(subscription);
                if (response.Success)
                {
                    isSubscribed = true;
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to subscribe to push notifications");
        }
    }

    private async Task UnsubscribeFromPush()
    {
        try
        {
            if (!string.IsNullOrEmpty(currentEndpoint))
            {
                var response = await PushApiService.UnsubscribeAsync(currentEndpoint);
                if (response.Success)
                {
                    isSubscribed = false;
                    currentEndpoint = null;
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unsubscribe from push notifications");
        }
    }

    public enum PushSupport
    {
        Unknown,
        Supported,
        NotSupported
    }
}
```

## 📱 Service Worker

### sw.js

```javascript
const CACHE_NAME = 'inventory-control-v1';
const VAPID_PUBLIC_KEY = 'YOUR_VAPID_PUBLIC_KEY';

// Install event
self.addEventListener('install', (event) => {
    console.log('Service Worker installing...');
    self.skipWaiting();
});

// Activate event
self.addEventListener('activate', (event) => {
    console.log('Service Worker activating...');
    event.waitUntil(self.clients.claim());
});

// Push event
self.addEventListener('push', (event) => {
    console.log('Push notification received:', event);
    
    let notificationData = {
        title: 'Inventory Control',
        body: 'You have a new notification',
        icon: '/icon-192.png',
        badge: '/icon-192.png',
        tag: 'inventory-notification',
        requireInteraction: false,
        data: {}
    };

    if (event.data) {
        try {
            const pushData = event.data.json();
            notificationData = { ...notificationData, ...pushData };
        } catch (e) {
            console.error('Failed to parse push data:', e);
        }
    }

    event.waitUntil(
        self.registration.showNotification(notificationData.title, notificationData)
    );
});

// Notification click event
self.addEventListener('notificationclick', (event) => {
    console.log('Notification clicked:', event);
    
    event.notification.close();

    const clickAction = event.action;
    const notificationData = event.notification.data;

    event.waitUntil(
        self.clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then((clientList) => {
                // Если есть открытое окно, фокусируемся на нем
                for (const client of clientList) {
                    if (client.url.includes(self.location.origin) && 'focus' in client) {
                        return client.focus();
                    }
                }
                
                // Если нет открытого окна, открываем новое
                if (self.clients.openWindow) {
                    let url = '/';
                    
                    // Если есть данные о действии, переходим на соответствующую страницу
                    if (notificationData && notificationData.actionUrl) {
                        url = notificationData.actionUrl;
                    }
                    
                    return self.clients.openWindow(url);
                }
            })
    );
});

// Background sync (для будущих функций)
self.addEventListener('sync', (event) => {
    console.log('Background sync event:', event);
    
    if (event.tag === 'notification-sync') {
        event.waitUntil(doBackgroundSync());
    }
});

async function doBackgroundSync() {
    // Логика синхронизации уведомлений
    console.log('Performing background sync...');
}
```

## 🔐 Конфигурация

### appsettings.json

```json
{
  "PushNotifications": {
    "Subject": "mailto:admin@inventorycontrol.com",
    "PublicKey": "YOUR_VAPID_PUBLIC_KEY",
    "PrivateKey": "YOUR_VAPID_PRIVATE_KEY",
    "Enabled": true,
    "MaxRetries": 3,
    "RetryDelay": 5000
  }
}
```

### Program.cs регистрация сервисов

```csharp
// Регистрация push notification сервиса
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

// Конфигурация VAPID
builder.Services.Configure<PushNotificationOptions>(
    builder.Configuration.GetSection("PushNotifications"));

// Добавление CORS для push notifications
builder.Services.AddCors(options =>
{
    options.AddPolicy("PushNotifications", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

## 🧪 Тестирование

### Unit тесты

```csharp
public class PushNotificationServiceTests : TestBase
{
    [Fact]
    public async Task SubscribeAsync_ValidSubscription_ShouldReturnTrue()
    {
        // Arrange
        var userId = "test-user";
        var subscription = new PushSubscriptionDto
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            Keys = new PushKeysDto
            {
                P256dh = "test-p256dh-key",
                Auth = "test-auth-key"
            }
        };

        // Act
        var result = await _pushService.SubscribeAsync(userId, subscription);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendToUserAsync_ValidUser_ShouldSendNotification()
    {
        // Arrange
        var userId = "test-user";
        var request = new PushNotificationRequest
        {
            Title = "Test Notification",
            Body = "This is a test notification"
        };

        // Act
        var result = await _pushService.SendToUserAsync(userId, request);

        // Assert
        Assert.True(result);
    }
}
```

### Integration тесты

```csharp
public class PushSubscriptionControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Subscribe_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var subscription = new PushSubscriptionDto
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            Keys = new PushKeysDto
            {
                P256dh = "test-p256dh-key",
                Auth = "test-auth-key"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pushsubscription/subscribe", subscription);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

## 📊 Мониторинг и метрики

### Метрики для отслеживания

```csharp
public class PushNotificationMetrics
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int NotificationsSent { get; set; }
    public int NotificationsDelivered { get; set; }
    public int NotificationsFailed { get; set; }
    public double DeliveryRate { get; set; }
    public Dictionary<string, int> BrowserStats { get; set; } = new();
    public Dictionary<string, int> DeviceTypeStats { get; set; } = new();
}
```

## 🔄 Интеграция с существующей системой

### Обновление NotificationService

```csharp
public class NotificationService : INotificationService
{
    private readonly IPushNotificationService _pushService;
    // ... другие зависимости

    public async Task<bool> CreateNotificationAsync(CreateNotificationRequest request)
    {
        // Создаем уведомление в базе данных
        var notification = await CreateNotificationInDatabase(request);
        
        // Отправляем через SignalR
        await SendViaSignalR(notification);
        
        // Отправляем push уведомление
        await SendPushNotification(notification);
        
        return true;
    }

    private async Task SendPushNotification(NotificationDto notification)
    {
        try
        {
            var pushRequest = new PushNotificationRequest
            {
                Title = notification.Title,
                Body = notification.Message,
                Icon = "/icon-192.png",
                Badge = "/icon-192.png",
                Tag = $"notification-{notification.Id}",
                Data = new Dictionary<string, object>
                {
                    ["notificationId"] = notification.Id,
                    ["type"] = notification.Type,
                    ["category"] = notification.Category,
                    ["actionUrl"] = notification.ActionUrl ?? ""
                }
            };

            await _pushService.SendToUserAsync(notification.UserId, pushRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification for notification {NotificationId}", notification.Id);
        }
    }
}
```

## 🚀 Развертывание

### Шаги развертывания

1. **Генерация VAPID ключей**:
```bash
npx web-push generate-vapid-keys
```

2. **Обновление конфигурации**:
   - Добавить VAPID ключи в appsettings.json
   - Настроить CORS для push notifications

3. **Миграция базы данных**:
```bash
dotnet ef migrations add AddPushSubscriptions
dotnet ef database update
```

4. **Развертывание Service Worker**:
   - Копировать sw.js в wwwroot
   - Обновить index.html для регистрации SW

5. **Тестирование**:
   - Проверить подписку на push уведомления
   - Тестировать доставку уведомлений
   - Проверить обработку кликов

## 📝 Заключение

Данная спецификация обеспечивает полную реализацию browser push уведомлений с интеграцией в существующую систему уведомлений. Реализация включает безопасность, мониторинг, тестирование и развертывание.

---

*Документ создан: $(Get-Date)*  
*Версия: 1.0*  
*Статус: Ready for Implementation*
