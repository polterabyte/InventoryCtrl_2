# Smart Notifications System v2

Система умных уведомлений обеспечивает комплексное решение для управления уведомлениями в приложении Inventory Control с поддержкой real-time коммуникации через SignalR, персистентных уведомлений в базе данных и расширенной системы правил.

## 🚀 Основные возможности

### 🎯 Ключевые функции
- **Real-time уведомления** — мгновенные уведомления через SignalR Hub
- **Персистентные уведомления** — хранение уведомлений в базе данных с полными CRUD операциями
- **Toast уведомления** — всплывающие уведомления для немедленной обратной связи
- **Система правил** — настраиваемые правила для автоматических триггеров уведомлений
- **Пользовательские предпочтения** — детальный контроль над типами уведомлений и способами доставки
- **Шаблоны уведомлений** — переиспользуемые шаблоны для согласованных сообщений
- **Многоканальная поддержка** — in-app, email, и push уведомления (email/push в планах)
- **SignalR Hub** — управление подключениями и real-time коммуникацией
- **Группировка пользователей** — отправка уведомлений группам пользователей по ролям

### 🔔 Типы уведомлений
- **INFO** — общие информационные уведомления
- **SUCCESS** — подтверждения успешных операций
- **WARNING** — важные предупреждения, требующие внимания
- **ERROR** — уведомления об ошибках, требующие немедленных действий

### 📂 Категории
- **STOCK** — уведомления, связанные с инвентарем (низкий запас, отсутствие товара)
- **TRANSACTION** — уведомления, связанные с транзакциями
- **SYSTEM** — уведомления о техническом обслуживании системы и ошибках
- **SECURITY** — предупреждения безопасности
- **REALTIME** — real-time уведомления через SignalR

## 🏗 Архитектура

### SignalR Hub Architecture

#### NotificationHub
```csharp
[Authorize]
public class NotificationHub : Hub
{
    // Управление подключениями пользователей
    // Группировка по ролям и типам уведомлений
    // Отслеживание активности в базе данных
}
```

#### Основные функции Hub:
- **Connection Management** — управление подключениями пользователей
- **Group Management** — группировка пользователей по ролям и типам уведомлений
- **Real-time Communication** — отправка мгновенных уведомлений
- **Connection Tracking** — отслеживание активности подключений в базе данных
- **Subscription Management** — управление подписками на типы уведомлений

#### Группы SignalR:
- **User_{userId}** — персональные уведомления пользователя
- **AllUsers** — уведомления для всех пользователей
- **Notifications_{type}** — уведомления определенного типа
- **Role_{roleName}** — уведомления для пользователей определенной роли

#### Типы событий SignalR:
- **ReceiveNotification** — новые уведомления
- **InventoryUpdated** — обновления инвентаря
- **UserActivity** — активность других пользователей
- **SystemAlert** — системные предупреждения
- **ConnectionEstablished** — подтверждение подключения

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

#### SignalR Connections Table
```sql
CREATE TABLE SignalRConnections (
    Id SERIAL PRIMARY KEY,
    ConnectionId VARCHAR(255) NOT NULL UNIQUE,
    UserId VARCHAR(450) NOT NULL,
    UserName VARCHAR(256),
    UserRole VARCHAR(50),
    UserAgent TEXT,
    IpAddress VARCHAR(45),
    ConnectedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    LastActivityAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN DEFAULT TRUE,
    SubscribedNotificationTypes TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP
);
```

### Service Layer

#### INotificationService (API)
- **CreateNotificationAsync** — создание новых уведомлений
- **GetUserNotificationsAsync** — получение уведомлений пользователя с пагинацией
- **MarkAsReadAsync** — отметка отдельных уведомлений как прочитанных
- **MarkAllAsReadAsync** — отметка всех уведомлений пользователя как прочитанных
- **ArchiveNotificationAsync** — архивирование уведомлений
- **DeleteNotificationAsync** — удаление уведомлений
- **GetNotificationStatsAsync** — получение статистики уведомлений
- **TriggerStockLowNotificationAsync** — триггер уведомлений о низком запасе
- **TriggerStockOutNotificationAsync** — триггер уведомлений об отсутствии товара
- **TriggerTransactionNotificationAsync** — триггер уведомлений о транзакциях
- **TriggerSystemNotificationAsync** — триггер системных уведомлений
- **SendBulkNotificationAsync** — массовая отправка уведомлений
- **CleanupExpiredNotificationsAsync** — очистка истекших уведомлений

#### INotificationRuleEngine
- **GetActiveRulesForEventAsync** — получение активных правил для конкретных событий
- **EvaluateConditionAsync** — оценка условий правил против данных
- **ProcessTemplateAsync** — обработка шаблонов уведомлений с данными
- **GetUserPreferencesForEventAsync** — получение пользовательских предпочтений для событий

#### SignalR Hub Services
- **Connection Management** — управление подключениями пользователей
- **Group Management** — управление группами пользователей
- **Real-time Broadcasting** — вещание уведомлений в реальном времени
- **Subscription Management** — управление подписками на типы уведомлений

## 🔌 API Endpoints

### Notifications
- `GET /api/Notification` — получение уведомлений пользователя
- `GET /api/Notification/{id}` — получение конкретного уведомления
- `POST /api/Notification` — создание уведомления
- `PUT /api/Notification/{id}/read` — отметка как прочитанное
- `PUT /api/Notification/mark-all-read` — отметка всех как прочитанные
- `PUT /api/Notification/{id}/archive` — архивирование уведомления
- `DELETE /api/Notification/{id}` — удаление уведомления
- `GET /api/Notification/stats` — получение статистики уведомлений

### Preferences
- `GET /api/Notification/preferences` — получение пользовательских предпочтений
- `PUT /api/Notification/preferences` — обновление предпочтений
- `DELETE /api/Notification/preferences/{eventType}` — удаление предпочтения

### Rules (только Admin/Manager)
- `GET /api/Notification/rules` — получение правил уведомлений
- `POST /api/Notification/rules` — создание правила
- `PUT /api/Notification/rules/{id}` — обновление правила
- `DELETE /api/Notification/rules/{id}` — удаление правила
- `PUT /api/Notification/rules/{id}/toggle` — переключение правила

### Bulk Operations (только Admin/Manager)
- `POST /api/Notification/bulk` — массовая отправка уведомлений
- `POST /api/Notification/cleanup` — очистка истекших уведомлений (только Admin)

## 📝 Примеры использования

### Создание уведомления
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
// Триггер уведомления о низком запасе
await notificationService.TriggerStockLowNotificationAsync(product);

// Триггер уведомления о транзакции
await notificationService.TriggerTransactionNotificationAsync(transaction);

// Триггер системного уведомления
await notificationService.TriggerSystemNotificationAsync(
    "System Maintenance", 
    "The system will be down for maintenance from 2-4 AM", 
    "user-123",
    "/maintenance"
);
```

### SignalR Integration

#### Подключение к SignalR Hub
```csharp
// В Blazor компоненте
@inject IJSRuntime JSRuntime

private async Task InitializeSignalR()
{
    var connection = await JSRuntime.InvokeAsync<IJSObjectReference>(
        "import", "/js/signalr-connection.js");
    
    await connection.InvokeVoidAsync("startConnection", authToken);
}

// JavaScript интеграция
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub", {
        accessTokenFactory: () => token
    })
    .build();

connection.on("ReceiveNotification", (notification) => {
    // Обработка уведомлений
    showToastNotification(notification);
});

connection.on("InventoryUpdated", (data) => {
    // Обновление данных инвентаря
    refreshInventoryData(data);
});
```

#### Отправка real-time уведомлений через SignalR
```csharp
// В контроллере или сервисе
public class NotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    
    public async Task SendRealTimeNotificationAsync(string userId, string title, string message)
    {
        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
        {
            Title = title,
            Message = message,
            Type = "INFO",
            Timestamp = DateTime.UtcNow
        });
    }
    
    public async Task BroadcastToAllUsersAsync(string title, string message)
    {
        await _hubContext.Clients.Group("AllUsers").SendAsync("ReceiveNotification", new
        {
            Title = title,
            Message = message,
            Type = "SYSTEM",
            Timestamp = DateTime.UtcNow
        });
    }
}
```

### Массовые уведомления
```csharp
var bulkRequest = new BulkNotificationRequest
{
    UserIds = new List<string> { "user-1", "user-2", "user-3" },
    Notification = new CreateNotificationRequest
    {
        Title = "System Update",
        Message = "System will be updated tonight at 2 AM",
        Type = "INFO",
        Category = "SYSTEM"
    }
};

var result = await notificationService.SendBulkNotificationAsync(bulkRequest.UserIds, bulkRequest.Notification);
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

## 🎨 UI Components

### NotificationBell Component
Выпадающий колокольчик уведомлений, который можно добавить в основной макет:

```razor
<NotificationBell />
```

**Возможности:**
- Индикатор количества непрочитанных уведомлений
- Быстрый доступ к последним 5 уведомлениям
- Кнопка "Mark All Read"
- Ссылка на полный центр уведомлений
- Автоматическое обновление через SignalR

### NotificationCenter Component
Полный центр уведомлений для управления всеми уведомлениями:

```razor
<NotificationCenter />
```

**Возможности:**
- Полный список уведомлений с пагинацией
- Фильтрация по типу и статусу
- Массовые операции (отметить все как прочитанные)
- Удаление отдельных уведомлений
- Действия с уведомлениями (переход по ссылкам)
- Real-time обновления через SignalR

### ToastNotification Component
Всплывающие toast уведомления для немедленной обратной связи:

```razor
<ToastNotification Notification="@notification" 
                   OnDismiss="@HandleDismiss" 
                   OnRetry="@HandleRetry" />
```

**Возможности:**
- Различные типы (Success, Error, Warning, Info)
- Автоматическое скрытие через заданное время
- Кнопка повтора для ошибок
- Анимации появления и исчезновения
- Адаптивный дизайн для мобильных устройств

### SignalR Connection Component
Компонент для управления SignalR подключением:

```razor
<SignalRConnection HubUrl="/notificationHub" 
                   OnConnected="@HandleConnected"
                   OnDisconnected="@HandleDisconnected" />
```

**Возможности:**
- Автоматическое подключение к SignalR Hub
- Обработка событий подключения/отключения
- Управление подписками на типы уведомлений
- Retry логика при сбоях подключения

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

## 🚀 Новые возможности v2

### ✅ Реализованные функции
- **SignalR Integration** — real-time уведомления через SignalR Hub
- **Connection Management** — отслеживание подключений пользователей
- **Group Management** — группировка пользователей по ролям и типам
- **Bulk Operations** — массовая отправка уведомлений
- **Enhanced UI Components** — улучшенные компоненты уведомлений
- **Real-time Broadcasting** — вещание уведомлений в реальном времени
- **Subscription Management** — управление подписками на типы уведомлений

### 🔮 Планируемые функции
- **Email Notifications** — поддержка SMTP email уведомлений
- **Push Notifications** — поддержка browser push уведомлений
- **Notification Scheduling** — запланированная доставка уведомлений
- **Notification Analytics** — статистика использования и аналитика
- **Notification Groups** — целевая отправка по группам пользователей
- **Advanced Templates** — расширенные шаблоны с условиями
- **Notification History** — история всех уведомлений с поиском

### 🔗 Точки интеграции
- **Dashboard Integration** — виджеты уведомлений на дашборде
- **Mobile App Support** — поддержка уведомлений для мобильных приложений
- **Third-party Integrations** — интеграция со Slack, Teams, Discord
- **Webhook Support** — интеграция внешних систем через webhooks
- **API Integrations** — интеграция с внешними API для уведомлений

## 🔒 Безопасность

- **User Isolation** — уведомления изолированы по ID пользователя
- **Role-based Access** — роли Admin/Manager требуются для управления правилами
- **Input Validation** — все входные данные валидируются и санитизируются
- **Rate Limiting** — API endpoints защищены от злоупотреблений
- **Audit Logging** — все операции с уведомлениями логируются для аудита
- **SignalR Security** — JWT аутентификация для SignalR подключений
- **Connection Tracking** — отслеживание и мониторинг SignalR подключений
- **Group Security** — безопасная группировка пользователей по ролям

## ⚡ Производительность

- **Database Indexing** — правильные индексы на часто запрашиваемых колонках
- **Pagination** — большие списки уведомлений пагинируются
- **Caching** — правила и шаблоны уведомлений кэшируются
- **Cleanup** — истекшие уведомления автоматически очищаются
- **Async Operations** — все операции асинхронные для лучшей производительности
- **SignalR Optimization** — оптимизация SignalR подключений и групп
- **Connection Pooling** — пулинг подключений для эффективного использования ресурсов
- **Real-time Efficiency** — эффективная доставка real-time уведомлений

## 🛠 Troubleshooting

### Общие проблемы

1. **Уведомления не срабатывают**
   - Проверьте, активны ли правила уведомлений
   - Убедитесь, что условия правил корректны
   - Проверьте настройки пользовательских предпочтений

2. **Переменные шаблонов не подставляются**
   - Проверьте пути свойств в шаблонах
   - Убедитесь, что структура объекта данных соответствует ожиданиям шаблона

3. **Проблемы с производительностью**
   - Проверьте индексы базы данных
   - Рассмотрите пагинацию для больших списков уведомлений
   - Проверьте сложность правил

### Новые проблемы v2

4. **SignalR подключения не работают**
   - Проверьте правильность URL для SignalR Hub
   - Убедитесь в корректности JWT токена для аутентификации
   - Проверьте настройки CORS для SignalR
   - Убедитесь, что SignalR сервис зарегистрирован в DI контейнере

5. **Real-time уведомления не доставляются**
   - Проверьте, что пользователь подключен к SignalR Hub
   - Убедитесь, что пользователь находится в правильной группе
   - Проверьте логи SignalR на наличие ошибок
   - Убедитесь, что события правильно отправляются через Hub

6. **Проблемы с группировкой пользователей**
   - Проверьте, что пользователи правильно добавляются в группы
   - Убедитесь, что роли пользователей корректно определяются
   - Проверьте права доступа для управления группами

7. **Массовые уведомления не отправляются**
   - Проверьте список пользователей в запросе
   - Убедитесь, что у вас есть права Admin/Manager
   - Проверьте ограничения Rate Limiting
   - Убедитесь, что все пользователи в списке существуют

### Отладка

Включите отладочное логирование для диагностики проблем с уведомлениями:
```json
{
  "Logging": {
    "LogLevel": {
      "Inventory.API.Services.NotificationService": "Debug",
      "Inventory.API.Services.NotificationRuleEngine": "Debug",
      "Inventory.API.Hubs.NotificationHub": "Debug",
      "Microsoft.AspNetCore.SignalR": "Debug"
    }
  }
}
```

### Команды отладки

```powershell
# Проверить SignalR подключения
docker logs inventoryctrl-api-1 -f | findstr "SignalR"

# Проверить уведомления
docker logs inventoryctrl-api-1 -f | findstr "Notification"

# Проверить подключения к базе данных
docker exec inventoryctrl-db-1 psql -U postgres -c "SELECT * FROM SignalRConnections WHERE IsActive = true;"
```

## 🤝 Вклад в развитие

При добавлении новых функций уведомлений:
1. Обновите схему базы данных при необходимости
2. Добавьте соответствующие DTOs и модели
3. Реализуйте методы сервисов
4. Добавьте API endpoints
5. Создайте UI компоненты
6. Напишите комплексные тесты
7. Обновите документацию

### Новые возможности v2
- **SignalR Integration** — интеграция с real-time коммуникацией
- **Enhanced Security** — расширенная безопасность с JWT и группировкой
- **Improved Performance** — улучшенная производительность и оптимизация
- **Better UX** — улучшенный пользовательский опыт с real-time уведомлениями

## 📄 Лицензия

Эта система уведомлений является частью приложения Inventory Control и следует тем же условиям лицензирования.

---

> 💡 **Совет**: Система уведомлений v2 обеспечивает enterprise-уровень функциональности с real-time коммуникацией, расширенной безопасностью и улучшенным пользовательским опытом. Используйте новые возможности для создания современного приложения управления инвентарем.
