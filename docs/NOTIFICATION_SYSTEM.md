# Smart Notifications System

The Smart Notifications System provides a comprehensive notification management solution for the Inventory Control application, supporting both real-time UI notifications and persistent database notifications.

## Features

### 🎯 Core Features
- **Persistent Notifications**: Store notifications in the database with full CRUD operations
- **Real-time UI Notifications**: Toast-style notifications for immediate user feedback
- **Smart Rule Engine**: Configurable rules for automatic notification triggers
- **User Preferences**: Granular control over notification types and delivery methods
- **Notification Templates**: Reusable templates for consistent messaging
- **Multi-channel Support**: In-app, email, and push notification capabilities (email/push pending)

### 🔔 Notification Types
- **INFO**: General information notifications
- **SUCCESS**: Success operation confirmations
- **WARNING**: Important alerts requiring attention
- **ERROR**: Error notifications requiring immediate action

### 📂 Categories
- **STOCK**: Inventory-related notifications (low stock, out of stock)
- **TRANSACTION**: Transaction-related notifications
- **SYSTEM**: System maintenance and error notifications
- **SECURITY**: Security-related alerts

## Architecture

### Database Schema

#### Notifications Table
```sql
CREATE TABLE Notifications (
    Id SERIAL PRIMARY KEY,
    Title VARCHAR(200) NOT NULL,
    Message VARCHAR(1000) NOT NULL,
    Type VARCHAR(50) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    ActionUrl VARCHAR(100),
    ActionText VARCHAR(50),
    IsRead BOOLEAN DEFAULT FALSE,
    IsArchived BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ReadAt TIMESTAMP,
    ExpiresAt TIMESTAMP,
    UserId VARCHAR(450),
    ProductId INTEGER,
    ProductName VARCHAR(200),
    TransactionId INTEGER
);
```

#### Notification Rules Table
```sql
CREATE TABLE NotificationRules (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    EventType VARCHAR(50) NOT NULL,
    NotificationType VARCHAR(50) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Condition TEXT NOT NULL,
    Template TEXT NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    Priority INTEGER DEFAULT 0,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP,
    CreatedBy VARCHAR(450)
);
```

#### Notification Preferences Table
```sql
CREATE TABLE NotificationPreferences (
    Id SERIAL PRIMARY KEY,
    UserId VARCHAR(450) NOT NULL,
    EventType VARCHAR(50) NOT NULL,
    EmailEnabled BOOLEAN DEFAULT TRUE,
    InAppEnabled BOOLEAN DEFAULT TRUE,
    PushEnabled BOOLEAN DEFAULT FALSE,
    MinPriority INTEGER,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP,
    UNIQUE(UserId, EventType)
);
```

### Service Layer

#### INotificationService (API)
- **CreateNotificationAsync**: Create new notifications
- **GetUserNotificationsAsync**: Retrieve user's notifications with pagination
- **MarkAsReadAsync**: Mark individual notifications as read
- **MarkAllAsReadAsync**: Mark all user notifications as read
- **ArchiveNotificationAsync**: Archive notifications
- **DeleteNotificationAsync**: Delete notifications
- **GetNotificationStatsAsync**: Get notification statistics
- **TriggerStockLowNotificationAsync**: Trigger low stock alerts
- **TriggerStockOutNotificationAsync**: Trigger out of stock alerts
- **TriggerTransactionNotificationAsync**: Trigger transaction notifications
- **TriggerSystemNotificationAsync**: Trigger system notifications

#### INotificationRuleEngine
- **GetActiveRulesForEventAsync**: Get active rules for specific events
- **EvaluateConditionAsync**: Evaluate rule conditions against data
- **ProcessTemplateAsync**: Process notification templates with data
- **GetUserPreferencesForEventAsync**: Get user preferences for events

## API Endpoints

### Notifications
- `GET /api/Notification` - Get user notifications
- `GET /api/Notification/{id}` - Get specific notification
- `POST /api/Notification` - Create notification
- `PUT /api/Notification/{id}/read` - Mark as read
- `PUT /api/Notification/mark-all-read` - Mark all as read
- `PUT /api/Notification/{id}/archive` - Archive notification
- `DELETE /api/Notification/{id}` - Delete notification
- `GET /api/Notification/stats` - Get notification statistics

### Preferences
- `GET /api/Notification/preferences` - Get user preferences
- `PUT /api/Notification/preferences` - Update preferences
- `DELETE /api/Notification/preferences/{eventType}` - Delete preference

### Rules (Admin/Manager only)
- `GET /api/Notification/rules` - Get notification rules
- `POST /api/Notification/rules` - Create rule
- `PUT /api/Notification/rules/{id}` - Update rule
- `DELETE /api/Notification/rules/{id}` - Delete rule
- `PUT /api/Notification/rules/{id}/toggle` - Toggle rule

## Usage Examples

### Creating a Notification
```csharp
var request = new CreateNotificationRequest
{
    Title = "Low Stock Alert",
    Message = "Product 'Widget A' is running low on stock",
    Type = "WARNING",
    Category = "STOCK",
    UserId = "user-123",
    ProductId = 456,
    ActionUrl = "/products/456",
    ActionText = "View Product"
};

var result = await notificationService.CreateNotificationAsync(request);
```

### Triggering Smart Notifications
```csharp
// Trigger low stock notification
await notificationService.TriggerStockLowNotificationAsync(product);

// Trigger transaction notification
await notificationService.TriggerTransactionNotificationAsync(transaction);

// Trigger system notification
await notificationService.TriggerSystemNotificationAsync(
    "System Maintenance", 
    "The system will be down for maintenance from 2-4 AM", 
    "user-123",
    "/maintenance"
);
```

### Creating Notification Rules
```csharp
var rule = new CreateNotificationRuleRequest
{
    Name = "High Value Transaction Alert",
    Description = "Triggers for transactions over $1000",
    EventType = "TRANSACTION_CREATED",
    NotificationType = "INFO",
    Category = "TRANSACTION",
    Condition = """{"Transaction.Amount": {"operator": ">=", "value": 1000}}""",
    Template = "High-value transaction: {{Transaction.ProductName}} - ${{Transaction.Amount}}",
    IsActive = true,
    Priority = 5
};

var result = await notificationService.CreateNotificationRuleAsync(rule);
```

## Rule Engine

The notification rule engine supports flexible condition evaluation using JSON-based rules:

### Condition Format
```json
{
    "PropertyPath": {
        "operator": "comparison_operator",
        "value": "expected_value"
    }
}
```

### Supported Operators
- `==`: Equals
- `!=`: Not equals
- `>`: Greater than
- `>=`: Greater than or equal
- `<`: Less than
- `<=`: Less than or equal
- `contains`: String contains
- `startsWith`: String starts with
- `endsWith`: String ends with

### Template Variables
Templates support variable substitution using `{{PropertyPath}}` syntax:
- `{{Product.Name}}` - Product name
- `{{Product.SKU}}` - Product SKU
- `{{Product.Quantity}}` - Current quantity
- `{{Transaction.Type}}` - Transaction type
- `{{Transaction.Quantity}}` - Transaction quantity

## UI Components

### NotificationBell Component
A dropdown notification bell that can be added to the main layout:

```razor
<NotificationBell />
```

### NotificationCenter Component
A full notification center for managing all notifications:

```razor
<NotificationCenter />
```

## Configuration

### Default Notification Rules
The system comes with pre-configured rules for common scenarios:
- Stock low alerts
- Stock out alerts
- High-value transaction alerts
- System error alerts

### User Preferences
Users can configure their notification preferences for each event type:
- Enable/disable email notifications
- Enable/disable in-app notifications
- Enable/disable push notifications
- Set minimum priority threshold

## Testing

The notification system includes comprehensive unit tests covering:
- Notification CRUD operations
- Rule engine functionality
- User preference management
- Smart notification triggers
- Error handling scenarios

Run tests with:
```bash
dotnet test test/Inventory.UnitTests/Services/NotificationServiceTests.cs
```

## Future Enhancements

### Planned Features
- **SignalR Integration**: Real-time notifications via SignalR
- **Email Notifications**: SMTP email notification support
- **Push Notifications**: Browser push notification support
- **Notification Scheduling**: Scheduled notification delivery
- **Notification Analytics**: Usage statistics and analytics
- **Bulk Operations**: Bulk notification management
- **Notification Groups**: Group-based notification targeting

### Integration Points
- **Dashboard Integration**: Notification widgets on dashboard
- **Mobile App Support**: Notification support for mobile applications
- **Third-party Integrations**: Slack, Teams, Discord notifications
- **Webhook Support**: External system integration via webhooks

## Security Considerations

- **User Isolation**: Notifications are isolated by user ID
- **Role-based Access**: Admin/Manager roles required for rule management
- **Input Validation**: All inputs are validated and sanitized
- **Rate Limiting**: API endpoints are rate-limited to prevent abuse
- **Audit Logging**: All notification operations are logged for audit purposes

## Performance Considerations

- **Database Indexing**: Proper indexes on frequently queried columns
- **Pagination**: Large notification lists are paginated
- **Caching**: Notification rules and templates are cached
- **Cleanup**: Expired notifications are automatically cleaned up
- **Async Operations**: All operations are asynchronous for better performance

## Troubleshooting

### Common Issues

1. **Notifications not triggering**
   - Check if notification rules are active
   - Verify rule conditions are correct
   - Ensure user preferences are configured

2. **Template variables not substituting**
   - Verify property paths in templates
   - Check data object structure matches template expectations

3. **Performance issues**
   - Check database indexes
   - Consider pagination for large notification lists
   - Review rule complexity

### Debugging

Enable debug logging to troubleshoot notification issues:
```json
{
  "Logging": {
    "LogLevel": {
      "Inventory.API.Services.NotificationService": "Debug",
      "Inventory.API.Services.NotificationRuleEngine": "Debug"
    }
  }
}
```

## Contributing

When adding new notification features:
1. Update the database schema if needed
2. Add corresponding DTOs and models
3. Implement service methods
4. Add API endpoints
5. Create UI components
6. Write comprehensive tests
7. Update documentation

## License

This notification system is part of the Inventory Control application and follows the same licensing terms.
