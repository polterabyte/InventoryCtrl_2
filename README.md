# InventoryCtrl_2

Веб-приложение для управления инвентарем на ASP.NET Core 9 + Blazor WebAssembly

> 📖 **Полная документация**: См. [DOCUMENTATION.md](DOCUMENTATION.md) для подробной информации об архитектуре, системе уведомлений, конфигурации портов и инструкциях по разработке.

## Структура проекта
- `src/Inventory.API` — серверная часть (ASP.NET Core Web API, PostgreSQL)
- `src/Inventory.Web.Client` — клиентская часть (Blazor WebAssembly)
- `src/Inventory.UI` — Razor Class Library (компоненты, стили, страницы)
- `src/Inventory.Shared` — общие компоненты, модели и сервисы
- `test/` — тестовые проекты (Unit, Integration, Component тесты)

## Быстрый старт

### Автоматический запуск (рекомендуется)
```powershell
# Полный запуск с проверками портов
.\start-apps.ps1

# Быстрый запуск без проверок
.\start-apps.ps1 -Quick

# Показать справку по использованию
.\start-apps.ps1 -Help
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
   dotnet run --project "src/Inventory.Web.Client/Inventory.Web.Client.csproj"
   ```

### Доступ к приложению
- **Web приложение**: https://localhost:7001
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
- **Комплексное тестирование** Unit, Integration и Component тесты


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

## CI/CD и Тестирование

Проект включает комплексную систему CI/CD и тестирования:

### CI/CD Pipeline
- **GitHub Actions** — автоматический запуск тестов при push и pull requests
- **Azure DevOps** — альтернативный pipeline с многоэтапным развертыванием
- **Docker** — контейнеризация тестов для изоляции и воспроизводимости
- **Отчеты о покрытии** — детальная аналитика качества кода

### Запуск тестов

#### Локально
```powershell
# Все тесты
dotnet test

# Или из папки test
cd test
.\run-tests.ps1

# С покрытием кода
.\run-tests.ps1 -Coverage
```

#### В Docker
```powershell
# Все тесты в контейнерах
.\scripts\Run-Tests-Docker.ps1

# Конкретный тип тестов
.\scripts\Run-Tests-Docker.ps1 -TestType unit
```

#### Генерация отчетов
```powershell
# HTML отчет о покрытии
.\scripts\Generate-Coverage-Report.ps1 -OpenReport
```

### Типы тестов
- **Unit Tests** — тестирование бизнес-логики и моделей
- **Integration Tests** — тестирование API endpoints
- **Component Tests** — тестирование Blazor компонентов
- **Performance Tests** — тестирование производительности

> 🧪 **Документация по тестированию**: См. [test/README.md](test/README.md) для подробной информации о тестах, принципах тестирования и инструкциях по разработке тестов.

> 🚀 **Документация по CI/CD**: См. [.ai-agents/reports/cicd-setup-report.md](.ai-agents/reports/cicd-setup-report.md) для полной информации о системе непрерывной интеграции и развертывания.

## Требования

- **.NET 9.0 SDK** — для разработки и сборки
- **PostgreSQL** — база данных (настроена в `appsettings.json`)
- **Современный браузер** — с поддержкой WebAssembly и HTTPS

## Устранение неполадок

1. **Порт занят**: Убедитесь, что порты 5000, 5001, 7000, 7001 свободны
2. **CORS ошибки**: Проверьте настройки CORS в `src/Inventory.API/appsettings.json`
3. **База данных**: Убедитесь, что PostgreSQL запущен и доступен
4. **Проблемы с HTTPS**: При необходимости отключите HTTPS в `Properties/launchSettings.json`

## Разработка

### Структура проектов

#### Inventory.Shared
- **Models/** — общие модели данных
- **DTOs/** — Data Transfer Objects для API
- **Interfaces/** — интерфейсы сервисов
- **Services/** — реализация API сервисов
- **Constants/** — константы и настройки

#### Inventory.UI
- **Components/** — переиспользуемые Razor компоненты
- **Layout/** — компоненты макета
- **Pages/** — компоненты страниц
- **wwwroot/** — статические ресурсы

#### Inventory.Web.Client
- **Pages/** — страницы приложения
- **Services/** — клиентские сервисы
- **Program.cs** — конфигурация клиента

### Инструкции для разработчиков
См. файл `.ai-agent-prompts` для постоянных пожеланий и инструкций по разработке.

## 📚 Документация

Для подробной информации о проекте см.:
- **[DOCUMENTATION.md](DOCUMENTATION.md)** - Полная документация проекта
  - Архитектура системы
  - Система уведомлений и обработки ошибок
  - Конфигурация портов
  - Инструкции по запуску и тестированию
  - Руководство по разработке
- **[test/README.md](test/README.md)** - Документация по тестированию
  - Структура тестовых проектов
  - Принципы тестирования
  - Инструкции по написанию тестов
  - Лучшие практики тестирования
