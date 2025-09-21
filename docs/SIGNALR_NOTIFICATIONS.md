# Real-time Notifications with SignalR

This document describes the implementation of real-time notifications using SignalR in the Inventory Control System.

## Overview

The system now supports real-time notifications that are instantly delivered to connected users when inventory changes occur. This includes:

- Stock level alerts (low stock, out of stock)
- Transaction notifications (new transactions, approvals)
- System notifications (maintenance, updates)
- User-specific notifications

## Architecture

### Backend Components

1. **NotificationHub** (`src/Inventory.API/Hubs/NotificationHub.cs`)
   - SignalR hub for real-time communication
   - Handles user connections and disconnections
   - Manages user groups and subscriptions
   - Tracks connections in database

2. **SignalRNotificationService** (`src/Inventory.API/Services/SignalRNotificationService.cs`)
   - Service for sending real-time notifications
   - Supports sending to individual users, groups, or all users
   - Handles different notification types

3. **Enhanced NotificationService** (`src/Inventory.API/Services/NotificationService.cs`)
   - Extended to integrate with SignalR
   - Automatically sends real-time notifications when creating notifications
   - Maintains backward compatibility

4. **SignalRConnection Model** (`src/Inventory.API/Models/SignalRConnection.cs`)
   - Database model for tracking SignalR connections
   - Stores user information, connection details, and subscriptions

### Frontend Components

1. **C# SignalR Client (Blazor WASM)** (`src/Inventory.Web.Client/Services/SignalRService.cs`)
   - Использует `Microsoft.AspNetCore.SignalR.Client`
   - Управляет состоянием подключения, автопереподключением и подписками
   - Интегрирован с Blazor без JS interop

2. **RealTimeNotificationComponent** (`src/Inventory.UI/Components/RealTimeNotificationComponent.razor`)
   - Blazor component for displaying real-time notifications
   - Shows connection status and notification toasts
   - Supports different notification types with styling

## Configuration

### Backend Configuration

SignalR is configured in `Program.cs`:

```csharp
// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

// Add SignalR notification service
builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();

// Map SignalR hubs
app.MapHub<NotificationHub>("/notificationHub");
```

### Frontend Configuration

SignalR клиент инициализируется в C# через `SignalRService` при загрузке приложения/компонента. Требуется:

- URL хаба формируется из секции клиента `ApiSettings` (`/api`, `/notificationHub`) и `window.location.origin`
- JWT access token (из LocalStorage/AuthenticationStateProvider)
- CORS на сервере должен включать фронтовые origin через `CORS_ALLOWED_ORIGINS`

## Usage

### Sending Notifications

#### From Backend Services

```csharp
// Inject ISignalRNotificationService
private readonly ISignalRNotificationService _signalRService;

// Send to specific user
await _signalRService.SendNotificationToUserAsync(userId, notification);

// Send to multiple users
await _signalRService.SendNotificationToUsersAsync(userIds, notification);

// Send to all users
await _signalRService.SendNotificationToAllAsync(notification);

// Send stock alert
await _signalRService.SendStockAlertAsync(productName, currentStock, threshold, "LOW");

// Send transaction notification
await _signalRService.SendTransactionNotificationAsync(userId, "IN", productName, quantity);
```

#### Automatic Notifications

The system automatically sends real-time notifications when:

- Creating notifications via `NotificationService.CreateNotificationAsync()`
- Triggering stock alerts via `TriggerStockLowNotificationAsync()`
- Triggering transaction notifications via `TriggerTransactionNotificationAsync()`
- Sending system notifications via `TriggerSystemNotificationAsync()`

### Frontend Integration

#### Adding to Layout

The `RealTimeNotificationComponent` is already included in the main layout:

```razor
@* Real-time notifications *@
<RealTimeNotificationComponent />
```

#### Customizing Notifications

You can customize the notification component:

```razor
<RealTimeNotificationComponent 
    ShowNotifications="true"
    MaxVisibleNotifications="5"
    AutoDismissDelay="5000"
    OnNotificationReceived="@HandleNotification"
    OnConnectionStateChanged="@HandleConnectionState" />
```

#### JavaScript API

JS‑клиент удалён. Взаимодействие осуществляется через C# сервис `SignalRService`:

```csharp
@inject SignalRService SignalR

protected override async Task OnInitializedAsync()
{
    await SignalR.ConnectAsync();
    await SignalR.SubscribeAsync("STOCK");
}

// Состояние подключения
var state = SignalR.ConnectionState;

// Отключение
await SignalR.DisconnectAsync();
```

## Notification Types

### Stock Notifications

- **STOCK_LOW**: Triggered when product stock falls below threshold
- **STOCK_OUT**: Triggered when product is out of stock

### Transaction Notifications

- **TRANSACTION_CREATED**: New transaction created
- **TRANSACTION_APPROVED**: Transaction approved
- **TRANSACTION_REJECTED**: Transaction rejected

### System Notifications

- **SYSTEM_MAINTENANCE**: System maintenance notifications
- **SYSTEM_UPDATE**: System update notifications
- **SECURITY_ALERT**: Security-related notifications

## Database Schema

### SignalRConnections Table

```sql
CREATE TABLE SignalRConnections (
    Id SERIAL PRIMARY KEY,
    ConnectionId VARCHAR(450) NOT NULL,
    UserId VARCHAR(450) NOT NULL,
    UserName VARCHAR(100),
    UserRole VARCHAR(50),
    UserAgent VARCHAR(100),
    IpAddress VARCHAR(45),
    ConnectedAt TIMESTAMP NOT NULL,
    LastActivityAt TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    SubscribedGroups TEXT, -- JSON array
    SubscribedNotificationTypes TEXT -- JSON array
);
```

## Security

- SignalR connections require JWT authentication
- Users can only receive notifications they're authorized to see
- Connection tracking helps monitor and manage active sessions
- Automatic cleanup of inactive connections

## Performance Considerations

- Connection pooling and management
- Automatic reconnection with exponential backoff
- Efficient group management
- Database connection tracking for monitoring

## Troubleshooting

### Common Issues

1. **Connection Failed**
   - Check JWT token validity
   - Verify API base URL configuration
   - Check network connectivity

2. **Notifications Not Received**
   - Verify user is subscribed to notification type
   - Check SignalR connection state
   - Verify notification rules are active

3. **Connection Drops Frequently**
   - Check network stability
   - Verify server configuration
   - Check for proxy/firewall issues

### Debugging

Enable detailed SignalR logging in development:

```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
```

Check browser console and client logs (ILogger in `SignalRService`) for SignalR connection details and errors.

## Future Enhancements

- Push notifications for mobile devices
- Email integration for critical notifications
- Notification preferences management
- Advanced filtering and routing
- Analytics and reporting
- Batch notification processing
