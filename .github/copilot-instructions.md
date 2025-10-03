# Inventory Control System v2 - AI Agent Guide

Это руководство поможет ИИ-ассистентам быстро освоиться в кодовой базе и эффективно помогать разработчикам.

## 🏗 Архитектурный обзор

### Структура проекта
```
src/
├── Inventory.API/          # ASP.NET Core Web API, EF Core, JWT Auth
├── Inventory.Web.Client/   # Blazor WebAssembly клиент
├── Inventory.UI/           # Переиспользуемые UI компоненты
├── Inventory.Web.Assets/   # RCL для общих статических ресурсов
└── Inventory.Shared/       # Общие модели и интерфейсы
```

### Ключевые технологии
- Backend: ASP.NET Core 8.0, EF Core + PostgreSQL, SignalR
- Frontend: Blazor WebAssembly, Radzen, SignalR Client
- Security: JWT + Refresh Tokens, Rate Limiting по ролям
- Testing: xUnit, bUnit, FluentAssertions

## 🔑 Критические паттерны и конвенции

### Entity Framework и база данных
- Используйте DbContext через DI (`AppDbContext`)
- Пароли и строки подключения только через ENV/User Secrets
- Сохраняйте структуру миграций в `/Migrations`
- Обязательно используйте `async/await` с БД-операциями
- Следите за foreign key constraints и каскадным удалением

### SignalR и real-time уведомления
```csharp
// Используйте NotificationHub для real-time событий
await hubContext.Clients.Group($"user_{userId}")
    .SendAsync("ReceiveNotification", title, message);

// Клиент использует C# SignalR Client (не JS)
await hubConnection.StartAsync();
hubConnection.On<string, string>("ReceiveNotification", OnNotificationReceived);
```

### Аутентификация и безопасность
- JWT токены через `AuthService`
- Refresh токены для автоматического обновления
- Rate Limiting по ролям:
  - Admin: 1000 req/min
  - Manager: 500 req/min
  - User: 100 req/min

## 🚀 Development Workflow

### Установка и запуск
```powershell
# Быстрый запуск (рекомендуется)
.\deploy\quick-deploy.ps1

# Для разработки с SSL
.\deploy\deploy-ssl-simple.ps1
```

### Конфигурация
- Переменные окружения в `.env` или User Secrets
- Обязательные ENV/Secrets:
  ```
  ConnectionStrings__DefaultConnection
  Jwt__Key
  CORS_ALLOWED_ORIGINS
  ADMIN_EMAIL, ADMIN_USERNAME, ADMIN_PASSWORD
  ApiUrl
  ```

### Тестирование
```powershell
# Запуск всех тестов
.\test\run-tests.ps1

# Конкретные тесты
.\test\run-tests.ps1 -Project unit|integration|component
```

## 📊 Entity Model

### Базовые сущности и связи
- **Product**: `Id`, `Name`, `SKU`, связи с Category/Manufacturer
- **Category**: `Id`, `Name`, self-referencing `ParentCategoryId`
- **InventoryTransaction**: `ProductId`, `WarehouseId`, `Quantity`, `Type`
- **User**: Стандартный ASP.NET Identity с дополнительным `Role`

### Аудит и мониторинг
- Все модели имеют `CreatedAt`, `UpdatedAt`
- `IsActive` флаг для soft-delete
- Полное логирование действий через `AuditService`
- Real-time уведомления об изменениях

## 🛠 Правила и лучшие практики

### Безопасность
- НЕ хранить секреты в коде
- Использовать User Secrets для локальной разработки
- Всегда проверять авторизацию в API endpoints
- Избегать хардкод URL/IP - брать из конфигурации

### Error Handling
```csharp
// Используйте глобальный Exception Handler
app.UseExceptionHandler("/error");

// В API используйте ApiResponse<T>
return ApiResponse<T>.Success(data);
return ApiResponse<T>.Error("Описание ошибки");
```

### Rate Limiting
```csharp
// Всегда учитывайте Rate Limiting при вызовах API
services.AddRateLimiting(options => {
    options.AddPolicy("AdminPolicy", 1000);
    options.AddPolicy("UserPolicy", 100);
});
```

### Testing Best Practices
- Unit тесты для бизнес-логики
- Integration тесты с реальной PostgreSQL
- Создавайте уникальную БД для каждого Integration теста
- Используйте bUnit для тестирования Blazor компонентов

## 📡 Важные интеграционные точки

### API Client Interfaces
```csharp
// Основной паттерн для API клиентов
public interface IProductService
{
    Task<ApiResponse<ProductDto>> GetByIdAsync(int id);
    Task<PagedApiResponse<ProductDto>> GetPagedAsync(int page, int pageSize);
}
```

### SignalR Events
```csharp
// События уведомлений
ReceiveNotification(title, message)
ProductQuantityChanged(productId, newQuantity)
InventoryAlert(type, message)
```

### External Dependencies
- PostgreSQL для данных
- Redis для кэширования (опционально)
- SMTP для почты (через конфигурацию)