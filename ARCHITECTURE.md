# Архитектура проекта InventoryCtrl_2

## Обзор
Проект реорганизован для поддержки множественных клиентских приложений, включая веб-приложение (Blazor WebAssembly) и будущее мобильное приложение.

## Структура проекта

```
InventoryCtrl_2/
├── src/
│   ├── Inventory.Server/          # ASP.NET Core Web API
│   │   ├── Controllers/           # API контроллеры
│   │   ├── Models/               # Модели Entity Framework
│   │   ├── Migrations/           # Миграции базы данных
│   │   └── Program.cs            # Конфигурация сервера
│   │
│   ├── Inventory.Client/          # Blazor WebAssembly клиент
│   │   ├── Pages/                # Razor страницы
│   │   ├── Layout/               # Макеты
│   │   ├── Services/             # Клиентские сервисы
│   │   └── Program.cs            # Конфигурация клиента
│   │
│   └── Inventory.Shared/          # Общие компоненты
│       ├── Models/               # Общие модели данных
│       ├── DTOs/                 # Data Transfer Objects
│       ├── Interfaces/           # Интерфейсы сервисов
│       ├── Services/             # API сервисы
│       ├── Constants/            # Константы и настройки
│       ├── Components/           # Общие Razor компоненты
│       │   ├── Forms/            # Формы (Login, Register, Product)
│       │   ├── Layout/           # Макеты и навигация
│       │   └── ProductCard.razor # Компоненты отображения
│       └── LoginRequest.cs       # Совместимость (legacy)
```

## Shared проект - Общие компоненты

### Models/ - Модели данных
- `Product.cs` - Товар
- `Category.cs` - Категория
- `Manufacturer.cs` - Производитель
- `ProductModel.cs` - Модель товара
- `ProductGroup.cs` - Группа товаров
- `Warehouse.cs` - Склад
- `Location.cs` - Местоположение
- `InventoryTransaction.cs` - Транзакция инвентаря

### DTOs/ - Data Transfer Objects
- `AuthDto.cs` - Авторизация (LoginRequest, RegisterRequest, etc.)
- `ProductDto.cs` - DTO для товаров
- `CategoryDto.cs` - DTO для категорий
- `ApiResponse<T>.cs` - Общий ответ API
- `PagedResponse<T>.cs` - Постраничный ответ

### Interfaces/ - Интерфейсы сервисов
- `IAuthService.cs` - Сервис авторизации
- `IProductService.cs` - Сервис товаров
- `ICategoryService.cs` - Сервис категорий

### Services/ - API сервисы
- `BaseApiService.cs` - Базовый API сервис
- `AuthApiService.cs` - Реализация авторизации
- `ProductApiService.cs` - Реализация работы с товарами

### Constants/ - Константы
- `ApiEndpoints.cs` - API endpoints
- `TransactionTypes.cs` - Типы транзакций
- `StorageKeys.cs` - Ключи для хранения

### Components/ - Общие Razor компоненты
- `Forms/LoginForm.razor` - Форма входа в систему
- `Forms/RegisterForm.razor` - Форма регистрации
- `Forms/ProductForm.razor` - Форма товара
- `Layout/MainLayout.razor` - Основной макет
- `Layout/NavigationMenu.razor` - Меню навигации
- `ProductCard.razor` - Карточка товара
- `ProductList.razor` - Список товаров

## Преимущества новой архитектуры

### 1. Переиспользование кода
- Все общие модели, DTOs и сервисы находятся в Shared проекте
- Клиентские приложения используют одни и те же интерфейсы и модели
- Упрощенная синхронизация между клиентами

### 2. Подготовка к мобильному приложению
- Shared проект готов для использования в .NET MAUI
- API сервисы можно использовать как в Blazor, так и в мобильном приложении
- Razor компоненты можно использовать в Blazor Hybrid (MAUI)
- Общие константы и настройки

### 3. Типобезопасность
- Strongly-typed API клиенты
- Общие модели данных
- Компилятор проверяет совместимость типов

### 4. Масштабируемость
- Легко добавлять новые клиентские приложения
- Централизованное управление API контрактами
- Единообразная обработка ошибок

## API Endpoints

### Авторизация
- `POST /api/auth/login` - Вход в систему
- `POST /api/auth/register` - Регистрация
- `POST /api/auth/refresh` - Обновление токена
- `POST /api/auth/logout` - Выход

### Товары
- `GET /api/products` - Список товаров
- `GET /api/products/{id}` - Товар по ID
- `GET /api/products/sku/{sku}` - Товар по SKU
- `POST /api/products` - Создание товара
- `PUT /api/products/{id}` - Обновление товара
- `DELETE /api/products/{id}` - Удаление товара
- `POST /api/products/{id}/stock/adjust` - Корректировка остатков

### Категории
- `GET /api/categories` - Список категорий
- `GET /api/categories/{id}` - Категория по ID
- `GET /api/categories/root` - Корневые категории
- `GET /api/categories/{parentId}/sub` - Подкатегории

## Технологии

### Backend
- ASP.NET Core 9.0
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Identity
- JWT Authentication
- Serilog

### Frontend
- Blazor WebAssembly
- Blazored.LocalStorage
- Bootstrap

### Shared
- .NET 9.0 Standard Library
- HTTP Client для API вызовов
- Общие модели и DTOs

## Развертывание

### Development
```bash
# Запуск сервера
dotnet run --project src/Inventory.Server

# Клиент автоматически собирается и обслуживается сервером
```

### Production
- Сервер: ASP.NET Core приложение
- Клиент: Статические файлы Blazor WebAssembly
- База данных: PostgreSQL
- Файловое хранилище: LocalStorage (клиент) / Database (сервер)

## Будущие улучшения

### Мобильное приложение (.NET MAUI)
- Использование Shared проекта
- Blazor Hybrid для UI (переиспользование Razor компонентов)
- Нативные UI компоненты для специфичных платформ
- Офлайн синхронизация
- Единый код для всех платформ

### Дополнительные клиенты
- Desktop приложение (.NET MAUI)
- Console приложение для администрирования
- Web API для интеграций

### Расширения
- SignalR для real-time обновлений
- Caching с Redis
- Message Queues для асинхронной обработки
- Microservices архитектура
