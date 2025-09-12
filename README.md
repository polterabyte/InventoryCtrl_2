# InventoryCtrl_2

Веб-приложение для управления инвентарем на ASP.NET Core 9 + Blazor WebAssembly

## Структура проекта
- `src/Inventory.API` — серверная часть (ASP.NET Core Web API, PostgreSQL)
- `src/Inventory.Web` — клиентская часть (Blazor WebAssembly)
- `src/Inventory.Shared` — общие компоненты, модели и сервисы

## Быстрый старт

### Автоматический запуск (рекомендуется)
```powershell
# Полная версия с проверками (английский текст)
.\start-apps-en.ps1

# Или русская версия
.\start-apps.ps1

# Быстрый запуск без проверок
.\quick-start-en.ps1
```

### Ручной запуск
1. Установите .NET 9 SDK
2. Настройте строку подключения к PostgreSQL в `src/Inventory.API/appsettings.json`
3. Запустите API сервер:
   ```bash
   dotnet run --project "src/Inventory.API/Inventory.API.csproj"
   ```
4. В новом терминале запустите Web клиент:
   ```bash
   dotnet run --project "src/Inventory.Web/Inventory.Web.csproj"
   ```

### Доступ к приложению
- **Web приложение**: https://localhost:5001
- **API документация**: https://localhost:7000/swagger

## Технологии

### Backend
- **ASP.NET Core 9.0** — веб-фреймворк
- **Entity Framework Core** — ORM для работы с базой данных
- **PostgreSQL** — основная база данных
- **ASP.NET Core Identity** — система аутентификации и авторизации
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

## Особенности архитектуры
- **Централизованное управление пакетами** через `Directory.Packages.props`
- **Shared проект** для переиспользования кода между клиентами
- **Поддержка миграций** Entity Framework Core
- **Глобальная обработка исключений** через middleware
- **Структурированное логирование** с Serilog
- **JWT аутентификация** с ролевой моделью (Admin, User, Manager)
- **CORS настройки** для поддержки множественных клиентов
- **Подготовка к мобильному приложению** (.NET MAUI)


## Структура базы данных

**Product** — товары
- Id (PK)
- Name
- SKU
- Description
- Quantity
- Unit
- IsActive (только для Admin)
- CategoryId (FK)
- ManufacturerId (FK)
- ProductModelId (FK)
- ProductGroupId (FK)
- MinStock
- MaxStock
- Note
**Manufacturer** — производители
- Id (PK)
- Name
**ProductModel** — модели товаров
- Id (PK)
- Name
- ManufacturerId (FK)
**ProductGroup** — группы товаров
- Id (PK)
- Name

**Category** — категории и подкатегории
- Id (PK)
- Name
- Description
- ParentCategoryId (FK, nullable)
- IsActive (только для Admin)

**Warehouse** — склады
- Id (PK)
- Name
- Location
- IsActive (только для Admin)

**InventoryTransaction** — все операции с товаром (приход, расход, установка)
- Id (PK)
- ProductId (FK)
- WarehouseId (FK)
- Type (Income/Outcome/Install)
- Quantity
- Date
- UserId (FK)
- LocationId (FK, nullable) — для операций установки
- Description

**User** — пользователи
- Id (PK)
- Username
- PasswordHash
- Role


**Location** — место установки (иерархия)
- Id (PK)
- Name
- Description
- ParentLocationId (FK, nullable)
- IsActive

**ProductHistory** (опционально)
- Id (PK)
- ProductId (FK)
- Date
- OldQuantity
- NewQuantity
- UserId (FK)
- Description

Операции архивирования/скрытия доступны только Admin.

## Требования

- **.NET 9.0 SDK** — для разработки и сборки
- **PostgreSQL** — база данных (настроена в `appsettings.json`)
- **Современный браузер** — с поддержкой WebAssembly и HTTPS

## Устранение неполадок

1. **Порт занят**: Убедитесь, что порты 5000, 5001, 5142, 7000 свободны
2. **CORS ошибки**: Проверьте настройки CORS в `src/Inventory.API/appsettings.json`
3. **База данных**: Убедитесь, что PostgreSQL запущен и доступен
4. **Проблемы с HTTPS**: При необходимости отключите HTTPS в `Properties/launchSettings.json`

## Разработка

### Структура Shared проекта
- **Models/** — общие модели данных
- **DTOs/** — Data Transfer Objects для API
- **Interfaces/** — интерфейсы сервисов
- **Services/** — реализация API сервисов
- **Constants/** — константы и настройки
- **Components/** — общие Razor компоненты

### Инструкции для разработчиков
См. файл `.ai-agent-prompts` для постоянных пожеланий и инструкций по разработке.
