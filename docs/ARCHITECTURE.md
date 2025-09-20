# Architecture Guide

Архитектура системы управления инвентарем на ASP.NET Core 8 + Blazor WebAssembly.

## 🏗 Обзор архитектуры

### Проектная структура
```
InventoryCtrl_2/
├── src/
│   ├── Inventory.API/          # ASP.NET Core Web API
│   │   ├── Controllers/        # API controllers
│   │   │   ├── AuthController.cs      # Authentication endpoints
│   │   │   ├── DashboardController.cs # Dashboard statistics
│   │   │   ├── ProductController.cs   # Product management
│   │   │   ├── CategoryController.cs  # Category management
│   │   │   └── UserController.cs      # User management
│   │   ├── Models/            # Entity Framework models
│   │   ├── Migrations/        # Database migrations
│   │   ├── Services/          # Business logic services
│   │   └── Program.cs         # Server configuration
│   │
│   ├── Inventory.Web.Client/   # Blazor WebAssembly client
│   │   ├── Pages/             # Razor pages
│   │   ├── Services/          # Client services
│   │   └── Program.cs         # Client configuration
│   │
│   ├── Inventory.UI/           # Razor Class Library
│   │   ├── Components/        # Reusable Razor components
│   │   ├── Layout/            # Layout components
│   │   ├── Pages/             # Page components
│   │   └── wwwroot/           # Static assets
│   │
│   └── Inventory.Shared/       # Common components
│       ├── Models/            # Common data models
│       ├── DTOs/              # Data Transfer Objects
│       ├── Interfaces/        # Service interfaces
│       ├── Services/          # API services
│       └── Constants/         # Constants and settings
```

## 🎯 Архитектурные принципы

### 1. Разделение ответственности
- **API Layer** — обработка HTTP запросов, валидация
- **Business Layer** — бизнес-логика и правила
- **Data Layer** — работа с базой данных
- **Presentation Layer** — пользовательский интерфейс

### 2. Переиспользование кода
- **Shared проект** — общие модели, DTOs, сервисы
- **UI Library** — переиспользуемые Blazor компоненты
- **Common interfaces** — единые контракты API

### 3. Подготовка к масштабированию
- **Multi-client support** — готовность к MAUI, Desktop
- **API-first approach** — четкие контракты
- **Modular design** — независимые компоненты

## 📊 Структура базы данных

### Основные сущности

#### Product — Товары
- Id (PK)
- Name, SKU, Description
- Quantity, Unit, MinStock, MaxStock
- IsActive (только для Admin)
- CategoryId (FK), ManufacturerId (FK)
- ProductModelId (FK), ProductGroupId (FK)
- CreatedAt, UpdatedAt

#### Category — Категории и подкатегории
- Id (PK)
- Name, Description
- ParentCategoryId (FK, nullable) — иерархия
- IsActive (только для Admin)

#### Manufacturer — Производители
- Id (PK)
- Name

#### ProductModel — Модели товаров
- Id (PK)
- Name
- ManufacturerId (FK)

#### ProductGroup — Группы товаров
- Id (PK)
- Name

#### Warehouse — Склады
- Id (PK)
- Name, Location
- IsActive (только для Admin)

#### InventoryTransaction — Операции с товаром
- Id (PK)
- ProductId (FK), WarehouseId (FK)
- Type (Income/Outcome/Install)
- Quantity, Date, Description
- UserId (FK)
- LocationId (FK, nullable) — для установки

#### User — Пользователи
- Id (PK) - string (GUID)
- UserName, Email
- PasswordHash (управляется Identity)
- Role (custom property)
- NormalizedUserName, NormalizedEmail

#### Location — Места установки
- Id (PK)
- Name, Description
- ParentLocationId (FK, nullable) — иерархия
- IsActive

## 🔧 Технологический стек

### Backend
- **ASP.NET Core 8.0** — веб-фреймворк
- **Entity Framework Core 8.0** — ORM для работы с БД
- **PostgreSQL** — основная база данных
- **ASP.NET Core Identity** — система аутентификации
- **JWT Authentication** — токенная аутентификация с refresh токенами
- **SignalR** — real-time коммуникация
- **Serilog** — структурированное логирование
- **Swagger/OpenAPI** — документация API
- **FluentValidation** — валидация входных данных
- **Rate Limiting** — защита от злоупотреблений

### Frontend
- **Blazor WebAssembly** — клиентская веб-платформа
- **Blazored.LocalStorage** — локальное хранение данных
- **Bootstrap** — CSS фреймворк для UI
- **Microsoft.AspNetCore.Components.Authorization** — авторизация
- **SignalR Client** — real-time уведомления

### Shared
- **.NET 8.0 Standard Library** — общие компоненты
- **HTTP Client** — API клиенты с retry механизмом
- **Общие модели и DTOs** — типизированные контракты
- **BaseApiService** — базовый класс для API сервисов

## 🚀 Особенности архитектуры

### Централизованное управление
- **Directory.Packages.props** — управление версиями пакетов
- **launchSettings.json** — конфигурация портов для development
- **global.json** — версия .NET SDK

### Безопасность
- **JWT аутентификация** с ролевой моделью и refresh токенами
- **Rate Limiting** — защита от злоупотреблений с настройкой по ролям
- **CORS настройки** для множественных клиентов
- **HTTPS обязателен** для production
- **Аудит действий** — полное логирование всех операций пользователей
- **Роли**: Admin (1000 req/min), Manager (500 req/min), User (100 req/min)

### Middleware Pipeline
- **Global Exception Middleware** — централизованная обработка ошибок
- **Audit Middleware** — автоматическое логирование HTTP запросов
- **Rate Limiting Middleware** — контроль нагрузки
- **JWT Authentication Middleware** — проверка токенов
- **CORS Middleware** — настройка межсайтовых запросов

### Масштабируемость
- **Shared проект** готов для .NET MAUI
- **UI компоненты** переиспользуемы
- **API контракты** четко определены
- **Модульная архитектура** для легкого расширения

## 🔄 Потоки данных

### API Request Flow
```
Client → Rate Limiting → CORS → Audit Middleware → API Controller → Service → Repository → Database
       ← Response ← DTO ← Model ← Entity ← Audit Log ←
```

### Authentication Flow
```
Login Request → AuthController → Identity → JWT Token → Local Storage
Token Validation → JWT Middleware → Authorized Access → SignalR Connection
```

### Real-time Notification Flow
```
Business Event → Notification Service → SignalR Hub → Client Groups → UI Update
```

### Data Synchronization
```
API Changes → Shared Models → UI Components → Local Storage → SignalR Notifications
```

### Audit Flow
```
User Action → Audit Middleware → Audit Service → Database Log → SignalR Notification
```

## 📡 Real-time Communication (SignalR)

### NotificationHub Architecture
```csharp
[Authorize]
public class NotificationHub : Hub
{
    // Управление подключениями пользователей
    // Группировка по ролям и типам уведомлений
    // Отслеживание активности в базе данных
}
```

### Типы уведомлений
- **Inventory Updates** — изменения в количестве товаров
- **System Alerts** — системные предупреждения
- **User Activities** — действия других пользователей
- **Low Stock Alerts** — предупреждения о низких остатках

### Группы пользователей
- **AllUsers** — все подключенные пользователи
- **User_{userId}** — персональные уведомления
- **Notifications_{type}** — подписки на типы уведомлений
- **Role_{role}** — уведомления по ролям

### Connection Management
- **Автоматическое подключение** при авторизации
- **Отслеживание активности** в SignalRConnections таблице
- **Переподключение** при потере соединения
- **Cleanup** неактивных соединений

## 📱 Multi-Client Architecture

### Текущие клиенты
- **Web Client** (Blazor WebAssembly) — основной интерфейс
- **API** — RESTful API для интеграций

### Планируемые клиенты
- **Mobile App** (.NET MAUI) — мобильное приложение
- **Desktop App** (.NET MAUI) — десктопное приложение
- **Console App** — административные утилиты

### Shared Components
- **Models** — общие модели данных
- **Services** — API клиенты
- **DTOs** — контракты данных
- **UI Components** — переиспользуемые компоненты
- **SignalR Client** — общий клиент для real-time коммуникации

## 🔍 Паттерны проектирования

### Dependency Injection
```csharp
// Регистрация сервисов через Extension Methods
builder.Services.AddCorsConfiguration();
builder.Services.AddCorsWithPorts();
builder.Services.AddAuditServices();
builder.Services.AddNotificationServices();

// Базовые сервисы
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRetryService, RetryService>();
```

### Service Layer Pattern
```csharp
// Базовый API сервис с общей функциональностью
public abstract class BaseApiService(HttpClient httpClient, string baseUrl, ILogger logger)
{
    protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint);
    protected async Task<PagedApiResponse<T>> GetPagedAsync<T>(string endpoint);
    // Общая обработка ошибок и логирование
}

// Специализированные сервисы
public class ProductApiService : BaseApiService, IProductService
{
    // Специфичная логика для продуктов
}
```

### Repository Pattern (через Entity Framework)
```csharp
// Абстракция доступа к данным через DbContext
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    // Автоматическое управление через EF Core
}
```

### SignalR Hub Pattern
```csharp
// Централизованное управление подключениями
[Authorize]
public class NotificationHub : Hub
{
    // Управление группами пользователей
    // Подписки на типы уведомлений
    // Отслеживание активности соединений
}
```

### Middleware Pattern
```csharp
// Цепочка обработки запросов
public class AuditMiddleware
{
    // Автоматическое логирование всех HTTP запросов
    // Извлечение метаданных (IP, User-Agent, время выполнения)
    // Интеграция с AuditService
}
```

## 📈 Производительность

### Оптимизации
- **Connection pooling** — пул соединений с PostgreSQL
- **Caching** — кэширование часто используемых данных
- **Lazy loading** — ленивая загрузка связанных данных
- **Pagination** — пагинация больших наборов данных
- **Rate Limiting** — защита от перегрузки системы
- **Retry механизм** — автоматические повторы при сбоях
- **Async/Await** — неблокирующие операции

### Мониторинг и логирование
- **Serilog** — структурированное логирование с контекстом
- **Audit Middleware** — автоматическое логирование HTTP запросов
- **Performance tracking** — отслеживание времени выполнения запросов
- **SignalR Connection tracking** — мониторинг real-time соединений
- **Error tracking** — централизованная обработка ошибок

### Масштабирование
- **Stateless design** — готовность к горизонтальному масштабированию
- **Database indexing** — оптимизация запросов к БД
- **Connection management** — эффективное управление соединениями
- **Resource cleanup** — автоматическая очистка неиспользуемых ресурсов

## 🔒 Безопасность

### Аутентификация
- **JWT tokens** — токены доступа с коротким временем жизни
- **Refresh tokens** — безопасное обновление токенов
- **Password hashing** — хеширование паролей через ASP.NET Identity
- **Token validation** — проверка токенов на каждом запросе

### Авторизация
- **Role-based access** — доступ на основе ролей (Admin/Manager/User)
- **Policy-based authorization** — гибкие политики доступа
- **Resource-based authorization** — доступ к конкретным ресурсам
- **SignalR authorization** — защита real-time соединений

### Защита данных
- **HTTPS** — обязательное шифрование трафика
- **CORS** — настройка межсайтовых запросов
- **Input validation** — валидация через FluentValidation
- **Rate limiting** — защита от DDoS и злоупотреблений
- **SQL injection protection** — защита через Entity Framework
- **XSS protection** — защита от межсайтового скриптинга

### Аудит и мониторинг
- **Audit logging** — полное логирование действий пользователей
- **Request tracking** — отслеживание всех HTTP запросов
- **Connection monitoring** — мониторинг SignalR соединений
- **Error logging** — детальное логирование ошибок

## 🧪 Тестирование

### Стратегия тестирования
- **Unit Tests** — тестирование бизнес-логики
- **Integration Tests** — тестирование с реальной БД
- **Component Tests** — тестирование UI компонентов

### Инструменты
- **xUnit** — фреймворк тестирования
- **Moq** — мокирование зависимостей
- **FluentAssertions** — читаемые утверждения
- **bUnit** — тестирование Blazor компонентов

## 🚀 Новые возможности v2

### Real-time Features
- **Live notifications** — мгновенные уведомления о изменениях
- **Collaborative editing** — работа нескольких пользователей одновременно
- **Live dashboard** — обновления статистики в реальном времени
- **Connection status** — отображение статуса подключения

### Enhanced Security
- **Advanced rate limiting** — защита по ролям пользователей
- **Comprehensive auditing** — детальное логирование всех действий
- **JWT with refresh tokens** — улучшенная система аутентификации
- **Connection tracking** — мониторинг всех подключений

### Developer Experience
- **Centralized configuration** — управление через launchSettings.json и docker-compose
- **Package version management** — централизованные версии пакетов
- **Comprehensive testing** — unit, integration, component тесты
- **Auto-generated documentation** — Swagger/OpenAPI

### Scalability Improvements
- **Stateless design** — готовность к горизонтальному масштабированию
- **Modular architecture** — легкое добавление новых модулей
- **Multi-client ready** — поддержка различных типов клиентов
- **Performance monitoring** — встроенные метрики производительности

## 📚 Дополнительные ресурсы

- **[API.md](API.md)** — документация API endpoints
- **[TESTING.md](TESTING.md)** — руководство по тестированию
- **[DEVELOPMENT.md](DEVELOPMENT.md)** — руководство разработчика
- **[NOTIFICATION_SYSTEM.md](NOTIFICATION_SYSTEM.md)** — система уведомлений
- **[SIGNALR_NOTIFICATIONS.md](SIGNALR_NOTIFICATIONS.md)** — real-time коммуникация
- **[css/README.md](css/README.md)** — CSS архитектура

---

> 💡 **Совет**: Архитектура v2 спроектирована для enterprise-уровня с поддержкой real-time коммуникации, расширенной безопасности и готовностью к масштабированию.
