Современная система управления инвентарем, построенная на ASP.NET Core 8.0 и Blazor WebAssembly с поддержкой реального времени через SignalR.

## 🚀 Основные возможности

### 📦 Управление инвентарем
- **Товары и категории** — полный каталог с иерархической структурой
- **Производители и модели** — детальная информация о товарах
- **Склады и локации** — многоуровневая система хранения
- **Операции** — приход, расход, установка товаров
- **Уведомления в реальном времени** — мгновенные обновления через SignalR

### 👥 Система пользователей
- **Ролевая модель** — Admin, Manager, User
- **JWT аутентификация** — безопасный доступ к системе
- **Аудит действий** — полная история операций пользователей

### 📊 Аналитика и отчетность
- **Dashboard** — статистика и ключевые метрики
- **История изменений** — отслеживание всех операций
- **Экспорт данных** — выгрузка отчетов

## 🏗 Архитектура

Проект построен по принципу **Clean Architecture** с четким разделением ответственности:

```
InventoryCtrl_2/
├── src/
│   ├── Inventory.API/          # ASP.NET Core Web API
│   ├── Inventory.Web.Client/   # Blazor WebAssembly клиент
│   ├── Inventory.UI/           # Переиспользуемые UI компоненты
│   ├── Inventory.Web.Assets/   # Razor Class Library для общих статических ресурсов (JS/CSS)
│   └── Inventory.Shared/       # Общие модели и сервисы
├── test/                       # Комплексное тестирование
│   ├── Inventory.UnitTests/    # Unit тесты
│   ├── Inventory.IntegrationTests/ # Интеграционные тесты
│   └── Inventory.ComponentTests/   # Тесты компонентов
└── docs/                       # Документация
```

### Технологический стек

**Backend:**
- ASP.NET Core 8.0
- Entity Framework Core с PostgreSQL
- ASP.NET Core Identity + JWT
- SignalR для уведомлений
- Serilog для логирования
- Swagger/OpenAPI

**Frontend:**
- Blazor WebAssembly
- Radzen для UI
- Blazored.LocalStorage
- Компонентная архитектура
- Microsoft.AspNetCore.SignalR.Client (C# клиент для SignalR в Blazor WASM)

**Тестирование:**
- xUnit для unit тестов
- bUnit для тестирования Blazor компонентов
- FluentAssertions для читаемых утверждений
- Moq для мокирования

# project
- read [README](./README.md)