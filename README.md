# InventoryCtrl_2

Веб-приложение на ASP.NET Core 9 + Blazor WebAssembly

## Структура
- src/Inventory.Server — серверная часть (ASP.NET Core, PostgreSQL)
- src/Inventory.Client — клиентская часть (Blazor WebAssembly)

## Быстрый старт
1. Установите .NET 9 SDK
2. Настройте строку подключения к PostgreSQL в `src/Inventory.Server/appsettings.json`
3. Запустите скрипт `start.ps1` для одновременного запуска сервера и клиента

## Особенности
- Централизованное управление пакетами через Directory.Packages.props
- Поддержка миграций и Entity Framework Core
- Современная архитектура для корпоративных приложений


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

## Инструкции для Copilot
См. файл `.copilot-prompts` для постоянных пожеланий и инструкций.
