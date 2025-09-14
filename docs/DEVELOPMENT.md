# Development Guide

Руководство разработчика для системы управления инвентарем.

## 🛠 Инструкции для разработчиков

См. файл `.ai-agent-prompts` для постоянных пожеланий и инструкций по разработке.

## 🎯 Архитектурные принципы

### Общая информация
Проект построен на ASP.NET Core с Blazor WASM и PostgreSQL. Цель — создать масштабируемую, прозрачную и устойчивую платформу для управления инвентарём.

### Принципы разработки
- **Явность**: Все решения должны быть явными. Не допускается implicit behavior, скрытые зависимости или магия фреймворков
- **Читаемость**: Конфигурация должна быть читаемой и документированной
- **Обоснованность**: Любое решение должно быть обосновано. Предпочтение отдаётся прозрачности, масштабируемости и инженерной честности
- **Понятность**: Код должен быть понятен другому разработчику через 6 месяцев

### Архитектура
- **Разделение ответственности**: Domain, Application, Infrastructure
- **Паттерны**: CQRS, Mediator, DI, строгая типизация
- **Безопасность**: JWT, Identity, политики доступа. Все API защищены, CORS настроен явно, HTTPS обязателен

### Качество кода
- **Тестирование**: Unit-тесты для бизнес-логики, integration-тесты для сценариев, component-тесты для UI. Без тестов код считается непригодным к продакшену
- **Покрытие тестами**: Минимум 80% покрытия для критически важного кода
- **Логирование**: Serilog с выводом в файл и Seq. Ошибки не должны теряться
- **UI**: Blazor WASM должен быть отзывчивым, адаптированным под роли и сценарии. Компоненты переиспользуемы, состояние управляется явно

## 🔧 Технические требования

### Языки и стиль
- Всегда использовать актуальные версии .NET и пакетов
- Логи только на английском языке
- Namespace использовать в формате: `namespace MyNamespace;`
- **ОБЯЗАТЕЛЬНО** использовать primary конструкторы в новых моделях: `class MyClass(int id, string name, string? description = null) { ... }`
- **ПОСТЕПЕННО** рефакторить существующие модели при их изменении
- Primary конструкторы для обязательных полей, optional параметры для nullable полей

### Код и структура
- Не добавлять лишние комментарии в код
- Все пути указывать в кавычках
- Для скриптов использовать PowerShell
- CSS стили организовать в едином design-system.css файле, но логически разделять на подфайлы компонентов (buttons.css, cards.css, forms.css, notifications.css и др.)
- Предлагать миграции и тесты при работе с БД

### Команды сборки
- НЕ использовать `cd "путь"; dotnet build`
- ВМЕСТО этого использовать `dotnet build --project 'путь'`

## 🏛 Специфические правила проекта

### Структура проекта
- **Inventory.API**: ASP.NET Core Web API с PostgreSQL, JWT аутентификация, Serilog логирование
- **Inventory.Web.Client**: Blazor WebAssembly клиент
- **Inventory.UI**: Razor Class Library для переиспользуемых компонентов
- **Inventory.Shared**: Общие модели, DTOs, сервисы, интерфейсы
- Прицел на создание в будущем Blazor Mobile и desktop приложение на основе Maui

### Работа с базой данных
- **Модели**: Всегда использовать Entity Framework Core модели в `Inventory.API/Models/`
- **Миграции**: При изменении моделей обязательно предлагать создание миграции
- **Связи**: Строго соблюдать FK связи между таблицами (Product → Category, Manufacturer, ProductModel, ProductGroup)
- **Роли**: Admin может управлять IsActive полями, обычные пользователи - нет
- **История**: Использовать ProductHistory для отслеживания изменений

### API и сервисы
- **BaseApiService**: Все API сервисы должны наследоваться от BaseApiService
- **Логирование**: Использовать структурированное логирование через ILogger
- **Обработка ошибок**: Всегда возвращать ApiResponse<T> с Success/ErrorMessage
- **DTOs**: Использовать DTOs из Inventory.Shared для API контрактов
- **JWT**: Все API защищены JWT токенами с ролями (Admin, User, Manager)

### Blazor компоненты
- **Переиспользование**: Компоненты должны быть в Inventory.UI (RCL) для переиспользования
- **Стили**: Использовать CSS модули app.css и design-system.css с логическими подфайлами компонентов (buttons.css, cards.css, forms.css, notifications.css) в `Inventory.UI/wwwroot/css/`. Максимально переиспользовать созданные стили
- **RCL архитектура**: Все переиспользуемые компоненты и их стили находятся в Razor Class Library
- **Состояние**: Использовать Blazored.LocalStorage для локального хранения
- **Авторизация**: Компоненты должны учитывать роли пользователей
- **Уведомления**: Использовать NotificationService для пользовательских уведомлений

## 🎨 Design System

### Обзор
Проект реализует комплексную систему дизайна, которая обеспечивает согласованность стилей, тематизации и паттернов компонентов во всем приложении.

### Файлы Design System
- **Основной файл**: `src/Inventory.UI/wwwroot/css/design-system.css`
- **Компоненты**: `src/Inventory.UI/wwwroot/css/components/`
- **Темы**: `src/Inventory.UI/wwwroot/css/themes/`
- **Порядок импорта**: Bootstrap → Design System → App Styles → Component Styles

### CSS архитектура для переиспользуемых компонентов
Проект использует **модульную CSS архитектуру**, где стили для переиспользуемых UI компонентов находятся в RCL проекте:
- **Переиспользуемые компоненты**: Все CSS файлы для UI компонентов находятся в `Inventory.UI/wwwroot/css/`
- **RCL архитектура**: Razor Class Library обеспечивает переиспользование компонентов и стилей
- **Модульная структура**: Компоненты разделены на отдельные CSS файлы
- **Поддержка тем**: Светлые/темные темы с простой настройкой
- **Готовность к будущему**: RCL готов для Mobile (MAUI) и Desktop (Electron/WPF) клиентов

### Design Tokens

#### Цветовая палитра
```css
/* Primary Colors */
--color-primary: #1b6ec2;
--color-primary-dark: #1861ac;
--color-primary-light: #258cfb;

/* Semantic Colors */
--color-success: #10b981;
--color-error: #ef4444;
--color-warning: #f59e0b;
--color-info: #3b82f6;

/* Neutral Colors */
--color-gray-50 to --color-gray-900: Полная палитра оттенков серого
```

#### Типографика
```css
/* Font Family */
--font-family-primary: 'Helvetica Neue', Helvetica, Arial, sans-serif;

/* Font Sizes */
--font-size-xs: 0.75rem (12px)
--font-size-sm: 0.875rem (14px)
--font-size-base: 1rem (16px)
--font-size-lg: 1.125rem (18px)
--font-size-xl: 1.25rem (20px)
--font-size-2xl: 1.5rem (24px)
--font-size-3xl: 1.875rem (30px)
--font-size-4xl: 2.25rem (36px)

/* Font Weights */
--font-weight-normal: 400
--font-weight-medium: 500
--font-weight-semibold: 600
--font-weight-bold: 700
```

#### Шкала отступов
```css
--spacing-1: 0.25rem (4px)
--spacing-2: 0.5rem (8px)
--spacing-3: 0.75rem (12px)
--spacing-4: 1rem (16px)
--spacing-5: 1.25rem (20px)
--spacing-6: 1.5rem (24px)
--spacing-8: 2rem (32px)
--spacing-10: 2.5rem (40px)
--spacing-12: 3rem (48px)
--spacing-16: 4rem (64px)
--spacing-20: 5rem (80px)
```

### Компонентные стили

#### Кнопки
Система дизайна предоставляет комплексные стили кнопок:
- `.btn` — базовый класс кнопки
- `.btn-primary` — кнопка основного действия
- `.btn-secondary` — кнопка вторичного действия
- `.btn-outline` — кнопка с контуром
- `.btn-sm` — маленький вариант кнопки
- `.btn-lg` — большой вариант кнопки

#### Карточки
Компоненты карточек с согласованными стилями:
- `.card` — базовый контейнер карточки
- `.card-header` — секция заголовка карточки
- `.card-body` — основной контент карточки
- `.card-footer` — секция подвала карточки

#### Формы
Стилизация форм с состояниями валидации:
- `.form-control` — стилизация поля ввода
- `.form-label` — стилизация метки
- `.validation-message` — стилизация сообщения об ошибке
- `.valid.modified` — стилизация валидного поля
- `.invalid` — стилизация невалидного поля

### Утилитарные классы

#### Утилиты макета
```css
.flex, .flex-col, .flex-row
.items-center, .items-start, .items-end
.justify-center, .justify-between, .justify-end
```

#### Утилиты отступов
```css
.p-1 to .p-8 (padding)
.px-2, .px-4, .px-6 (horizontal padding)
.py-2, .py-4, .py-6 (vertical padding)
.m-1 to .m-8 (margin)
.mb-2, .mb-4, .mb-6 (bottom margin)
.mt-2, .mt-4, .mt-6 (top margin)
```

### Поддержка темной темы
Система дизайна включает автоматическую поддержку темной темы через CSS media queries:
```css
@media (prefers-color-scheme: dark) {
  :root {
    --color-text-primary: #f9fafb;
    --color-text-secondary: #d1d5db;
    --color-bg-primary: #1f2937;
    --color-bg-secondary: #111827;
    /* ... больше переменных темной темы */
  }
}
```

## 🔔 Notification System

### Обзор
Реализована комплексная система улучшения UX для ошибок, включающая:

1. **Toast notifications** - всплывающие уведомления для пользователей
2. **Retry logic** - автоматический повтор операций
3. **Debug logs** - специальная страница для суперпользователей
4. **Enhanced error handling** - детальная обработка ошибок и логирование

### Компоненты

#### 1. Toast Notifications

##### NotificationService
```csharp
// Показать уведомление об успехе
NotificationService.ShowSuccess("Success", "Operation completed successfully");

// Показать ошибку с опцией повтора
NotificationService.ShowError("Error", "Operation failed", () => RetryOperation(), "Retry");

// Показать предупреждение
NotificationService.ShowWarning("Warning", "Please check your input");

// Показать информационное сообщение
NotificationService.ShowInfo("Info", "New feature available");

// Показать отладочное сообщение (только для разработки)
NotificationService.ShowDebug("Debug", "Debug information");
```

##### ToastNotification.razor
- Автоматические анимации показа/скрытия
- Поддержка различных типов уведомлений
- Кнопка повтора для ошибок
- Полоса прогресса для автоматического скрытия
- Адаптивный дизайн для мобильных устройств

#### 2. Retry Logic

##### RetryService
```csharp
// Выполнить операцию с логикой повтора
var result = await RetryService.ExecuteWithRetryAsync(
    async () => await SomeOperation(),
    "OperationName",
    maxRetries: 3
);

// Настроить политику повтора
var policy = new RetryPolicy
{
    MaxRetries = 5,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromSeconds(10),
    BackoffMultiplier = 2
};

var result = await RetryService.ExecuteWithRetryAsync(
    async () => await SomeOperation(),
    "OperationName",
    policy
);
```

#### 3. Debug Logs

##### DebugLogsService
- Получение логов в реальном времени
- Фильтрация по уровню, времени, поисковому запросу
- Автоматические обновления (живая потоковая передача)
- Детальная информация об ошибках

##### DebugLogs.razor
- Специальная страница `/debug` для суперпользователей
- Фильтры по уровню логов, количеству, поисковому запросу
- Автоматическая прокрутка
- Модальные окна с детальной информацией

#### 4. Enhanced Error Handling

##### ErrorHandlingService
```csharp
// Обработать ошибки с уведомлениями
await ErrorHandlingService.HandleErrorAsync(exception, "Operation context");

// Выполнить с логикой повтора
var result = await ErrorHandlingService.TryExecuteWithRetryAsync(
    async () => await SomeOperation(),
    "OperationName",
    maxRetries: 3
);

// Обработать ошибки API
await ErrorHandlingService.HandleApiErrorAsync(response, "API Operation");
```

##### GlobalExceptionMiddleware
- Детальное логирование ошибок с контекстом
- Понятные пользователю сообщения об ошибках
- Интеграция с отладочными логами
- Определение IP клиента

### Использование

#### В Blazor компонентах
```razor
@inject INotificationService NotificationService
@inject IRetryService RetryService
@inject IErrorHandlingService ErrorHandlingService

@code {
    private async Task LoadData()
    {
        try
        {
            var data = await RetryService.ExecuteWithRetryAsync(
                async () => await DataService.GetDataAsync(),
                "LoadData"
            );
            
            NotificationService.ShowSuccess("Data Loaded", $"Loaded {data.Count} items");
        }
        catch (Exception ex)
        {
            await ErrorHandlingService.HandleErrorAsync(ex, "Loading data");
        }
    }
}
```

#### В API сервисах
```csharp
public class MyApiService
{
    private readonly IRetryService _retryService;
    private readonly INotificationService _notificationService;

    public async Task<List<Data>> GetDataAsync()
    {
        return await _retryService.ExecuteWithRetryAsync(
            async () =>
            {
                var response = await _httpClient.GetAsync("/api/data");
                return await response.Content.ReadFromJsonAsync<List<Data>>();
            },
            "GetData"
        );
    }
}
```

#### Настройка в Program.cs
```csharp
// API проект
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();

// Web проект
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<IDebugLogsService, DebugLogsService>();
```

## ⚙️ Port Configuration

### Обзор
Все конфигурации портов централизованы в файле `ports.json` в корне проекта. Это устраняет конфликты портов и упрощает изменение портов во всех компонентах.

### Структура файла конфигурации

#### ports.json
```json
{
  "api": {
    "http": 5000,
    "https": 7000,
    "urls": "https://localhost:7000;http://localhost:5000"
  },
  "web": {
    "http": 5001,
    "https": 7001,
    "urls": "https://localhost:7001;http://localhost:5001"
  },
  "database": {
    "port": 5432
  },
  "cors": {
    "allowedOrigins": [
      "http://localhost:5000",
      "https://localhost:7000",
      "http://localhost:5001",
      "https://localhost:7001",
      "http://10.0.2.2:8080",
      "capacitor://localhost",
      "https://yourmobileapp.com"
    ]
  },
  "launchUrls": {
    "api": "https://localhost:7000",
    "web": "https://localhost:7001"
  }
}
```

### Назначение портов
- **API Server**: 
  - HTTP: 5000
  - HTTPS: 7000
- **Web Client**: 
  - HTTP: 5001
  - HTTPS: 7001
- **Database**: 
  - Port: 5432 (PostgreSQL)

### Использование

#### Изменение портов
Чтобы изменить порты, просто отредактируйте файл `ports.json` и обновите нужные значения. Все скрипты и конфигурации автоматически будут использовать новые порты.

#### Скрипты
Единый скрипт запуска с несколькими режимами:
- `start-apps.ps1` - Единый запускатель с полным и быстрым режимами
  - `.\start-apps.ps1` - Полный запуск с проверкой портов
  - `.\start-apps.ps1 -Quick` - Быстрый запуск без проверок
  - `.\start-apps.ps1 -Help` - Показать справку по использованию

#### Файлы конфигурации
Следующие файлы автоматически обновляются для использования централизованной конфигурации:
- `src/Inventory.API/Properties/launchSettings.json`
- `src/Inventory.Web/Properties/launchSettings.json`
- `src/Inventory.API/appsettings.json` (настройки CORS)

### Преимущества
1. **Централизованное управление**: Все порты определены в одном месте
2. **Нет конфликтов**: API и Web используют разные диапазоны портов
3. **Легкие обновления**: Изменение портов в одном файле влияет на все компоненты
4. **Согласованность**: Все скрипты и конфигурации остаются синхронизированными
5. **Документация**: Четкое назначение портов и использование

## 🚫 Специфические ограничения

- **НЕ создавать**: Отдельные .css файлы для компонентов (использовать логические подфайлы в design-system.css)
- **НЕ использовать**: `cd "путь"; dotnet build` - только `dotnet build --project 'путь'`
- **НЕ создавать**: Временные файлы в корне проекта - ТОЛЬКО в `.ai-agents/`
- **НЕ создавать**: CSS файлы в Inventory.Shared - ТОЛЬКО в Inventory.UI для переиспользуемых компонентов
- **ОБЯЗАТЕЛЬНО**: Проверять соответствие DOCUMENTATION.md и README.md
- **ВСЕГДА**: Предлагать миграции при изменении моделей БД
- **ТОЛЬКО**: Администраторы могут управлять IsActive полями
- **ЗАПРЕЩЕНО**: Использовать InMemory базу данных в любом виде (UseInMemoryDatabase, InMemoryDatabase и т.д.)
- **ОБЯЗАТЕЛЬНО**: Использовать только PostgreSQL для всех тестов и разработки, но не production а test БД

## 🔄 Primary конструкторы (ПРИМЕРЫ)

### ✅ Правильное использование:
```csharp
// Простая модель с обязательными полями
public class ProductTag(int id, string name, string? description = null, bool isActive = true)
{
    public int Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string? Description { get; set; } = description;
    public bool IsActive { get; set; } = isActive;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

// Сложная модель с множественными параметрами
public class AuditLog(
    string entityName, 
    string entityId, 
    string action, 
    string userId, 
    string? oldValues = null, 
    string? newValues = null, 
    string? description = null)
{
    public int Id { get; set; }
    public string EntityName { get; set; } = entityName;
    public string EntityId { get; set; } = entityId;
    public string Action { get; set; } = action;
    public string UserId { get; set; } = userId;
    public string? OldValues { get; set; } = oldValues;
    public string? NewValues { get; set; } = newValues;
    public string? Description { get; set; } = description;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
}
```

### ❌ Избегать:
- Primary конструкторы для классов со сложной бизнес-логикой
- Слишком много параметров (более 7-8)
- Primary конструкторы без документации

## 📚 Дополнительные ресурсы

- **[ARCHITECTURE.md](ARCHITECTURE.md)** — архитектура системы
- **[API.md](API.md)** — документация API
- **[TESTING.md](TESTING.md)** — руководство по тестированию
- **[css/README.md](css/README.md)** — CSS архитектура

---

> 💡 **Совет**: Следуйте принципам из `.ai-agent-prompts` для обеспечения качества и согласованности кода.
