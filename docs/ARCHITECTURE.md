# Architecture Guide

Архитектура системы управления инвентарем на ASP.NET Core 9 + Blazor WebAssembly.

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
- **ASP.NET Core 9.0** — веб-фреймворк
- **Entity Framework Core** — ORM для работы с БД
- **PostgreSQL** — основная база данных
- **ASP.NET Core Identity** — система аутентификации
- **JWT Authentication** — токенная аутентификация
- **Serilog** — структурированное логирование
- **Swagger/OpenAPI** — документация API

### Frontend
- **Blazor WebAssembly** — клиентская веб-платформа
- **Blazored.LocalStorage** — локальное хранение данных
- **Bootstrap** — CSS фреймворк для UI
- **Microsoft.AspNetCore.Components.Authorization** — авторизация

### Shared
- **.NET 9.0 Standard Library** — общие компоненты
- **HTTP Client** — API клиенты
- **Общие модели и DTOs** — типизированные контракты

## 🚀 Особенности архитектуры

### Централизованное управление
- **Directory.Packages.props** — управление версиями пакетов
- **ports.json** — централизованная конфигурация портов
- **global.json** — версия .NET SDK

### Безопасность
- **JWT аутентификация** с ролевой моделью
- **CORS настройки** для множественных клиентов
- **HTTPS обязателен** для production
- **Роли**: Admin, User, Manager

### Масштабируемость
- **Shared проект** готов для .NET MAUI
- **UI компоненты** переиспользуемы
- **API контракты** четко определены
- **Модульная архитектура** для легкого расширения

## 🔄 Потоки данных

### API Request Flow
```
Client → API Controller → Service → Repository → Database
       ← Response ← DTO ← Model ← Entity ←
```

### Authentication Flow
```
Login Request → AuthController → Identity → JWT Token
Token Validation → Middleware → Authorized Access
```

### Data Synchronization
```
API Changes → Shared Models → UI Components → Local Storage
```

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

## 🔍 Паттерны проектирования

### Dependency Injection
```csharp
// Регистрация сервисов
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
```

### Repository Pattern
```csharp
// Абстракция доступа к данным
public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
}
```

### CQRS (Command Query Responsibility Segregation)
```csharp
// Разделение команд и запросов
public class GetProductsQuery { }
public class CreateProductCommand { }
```

## 📈 Производительность

### Оптимизации
- **Connection pooling** — пул соединений с БД
- **Caching** — кэширование часто используемых данных
- **Lazy loading** — ленивая загрузка связанных данных
- **Pagination** — пагинация больших наборов данных

### Мониторинг
- **Serilog** — структурированное логирование
- **Health checks** — проверка состояния системы
- **Performance counters** — метрики производительности

## 🔒 Безопасность

### Аутентификация
- **JWT tokens** — токены доступа
- **Refresh tokens** — обновление токенов
- **Password hashing** — хеширование паролей

### Авторизация
- **Role-based access** — доступ на основе ролей
- **Policy-based authorization** — политики доступа
- **Resource-based authorization** — доступ к ресурсам

### Защита данных
- **HTTPS** — шифрование трафика
- **CORS** — защита от межсайтовых запросов
- **Input validation** — валидация входных данных

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

## 📚 Дополнительные ресурсы

- **[API.md](API.md)** — документация API endpoints
- **[TESTING.md](TESTING.md)** — руководство по тестированию
- **[DEVELOPMENT.md](DEVELOPMENT.md)** — руководство разработчика
- **[css/README.md](css/README.md)** — CSS архитектура

---

> 💡 **Совет**: Архитектура спроектирована для легкого расширения и поддержки множественных клиентов.
