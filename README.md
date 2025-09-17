# Inventory Control System

Веб-приложение для управления инвентарем на ASP.NET Core 9 + Blazor WebAssembly.

## 🚀 Быстрый старт

```powershell
# Автоматический запуск (рекомендуется)
.\start-apps.ps1

# Ручной запуск
dotnet run --project "src/Inventory.API/Inventory.API.csproj"
dotnet run --project "src/Inventory.Web.Client/Inventory.Web.Client.csproj"
```

**Доступ к приложению:**
- Web приложение: https://localhost:7001
- API документация: https://localhost:7000/swagger

## 📁 Структура проекта

```
src/
├── Inventory.API/          # ASP.NET Core Web API + PostgreSQL
├── Inventory.Web.Client/   # Blazor WebAssembly клиент
├── Inventory.UI/           # Razor Class Library (компоненты)
└── Inventory.Shared/       # Общие компоненты и сервисы

test/                       # Тестовые проекты
├── Inventory.UnitTests/    # Unit тесты
├── Inventory.IntegrationTests/ # Integration тесты с PostgreSQL
└── Inventory.ComponentTests/   # Component тесты Blazor
```

## 🛠 Технологии

### Backend
- **ASP.NET Core 9.0** — веб-фреймворк
- **PostgreSQL** — база данных
- **Entity Framework Core** — ORM
- **JWT Authentication** — токенная аутентификация
- **Serilog** — структурированное логирование

### Frontend
- **Blazor WebAssembly** — клиентская платформа
- **Bootstrap** — CSS фреймворк
- **Blazored.LocalStorage** — локальное хранение

## 🎯 Особенности

- **Централизованное управление пакетами** через `Directory.Packages.props`
- **Shared проект** для переиспользования кода
- **JWT аутентификация** с ролевой моделью (Admin, User, Manager)
- **Комплексное тестирование** Unit, Integration и Component тесты
- **Dashboard API** для аналитики и статистики
- **Подготовка к мобильному приложению** (.NET MAUI)

## 📚 Документация

- **[docs/QUICK_START.md](docs/QUICK_START.md)** — Быстрый старт и команды запуска
- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** — Архитектура и структура проекта
- **[docs/API.md](docs/API.md)** — API документация и endpoints
- **[docs/TESTING.md](docs/TESTING.md)** — Руководство по тестированию
- **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** — Руководство разработчика
- **[docs/css/README.md](docs/css/README.md)** — CSS архитектура и design system

## 🧪 Тестирование

```powershell
# Все тесты
dotnet test

# Или через скрипт
.\test\run-tests.ps1

# С покрытием кода
.\test\run-tests.ps1 -Coverage
```

**Результаты:**
- ✅ **120 тестов** — все проходят успешно
- ✅ **100% успешность** — 0 ошибок
- ✅ **Реальная PostgreSQL** — вместо InMemory для Integration тестов

## 🔧 Требования

- **.NET 9.0 SDK**
- **PostgreSQL**
- **Современный браузер** с поддержкой WebAssembly и HTTPS

## 🚨 Устранение неполадок

1. **Порт занят**: Убедитесь, что порты 5000, 5001, 7000, 7001 свободны
2. **CORS ошибки**: Проверьте настройки CORS в `src/Inventory.API/appsettings.json`
3. **База данных**: Убедитесь, что PostgreSQL запущен и доступен

## 📋 TODO - Планы развития

### 🔐 1. Улучшение безопасности и аутентификации
- [ ] **JWT Refresh Token**: Добавить `RefreshToken` в модель `User` и endpoint `/api/auth/refresh`
- [ ] **Аудит действий**: Расширить `AuditLog` для детального логирования всех действий пользователей
- [ ] **Rate Limiting**: Добавить ограничение частоты запросов для API endpoints
- [ ] **CORS политики**: Реализовать CORS политики по ролям пользователей
- [ ] **Валидация данных**: Улучшить валидацию входных данных на всех уровнях

### 📊 2. Расширение аналитики и отчетности
- [ ] **Система отчетов**: Создать `ReportService` и `ReportController` с экспортом в Excel/PDF
- [ ] **Дашборд аналитики**: Добавить интерактивные графики (Chart.js) и фильтры по периодам
- [ ] **Умные уведомления**: Реализовать email/SMS уведомления о низких остатках и просроченных товарах
- [ ] **Отчеты**: "Оборот товаров", "Топ продаж", "Анализ остатков", "Прогнозирование потребностей"
- [ ] **Настройки уведомлений**: Добавить в профиль пользователя настройки уведомлений

### 🏗️ 3. Модернизация архитектуры и производительности
- [ ] **Redis кэширование**: Интегрировать Redis для кэширования часто запрашиваемых данных
- [ ] **CQRS/MediatR**: Разделить команды и запросы, добавить `MediatR` для обработки команд
- [ ] **Фоновые задачи**: Интегрировать `Hangfire` для задач "Синхронизация данных", "Генерация отчетов"
- [ ] **Оптимизация БД**: Добавить индексы, оптимизировать SQL запросы, реализовать пагинацию
- [ ] **Lazy Loading**: Реализовать ленивую загрузку для больших данных
- [ ] **Мониторинг**: Добавить мониторинг выполнения задач и производительности

### 🎯 Приоритеты реализации
1. **Фаза 1** (1-2 недели): Безопасность - JWT Refresh, аудит, валидация
2. **Фаза 2** (2-3 недели): Аналитика - отчеты, дашборд, уведомления  
3. **Фаза 3** (3-4 недели): Архитектура - кэширование, CQRS, фоновые задачи

## 📖 Дополнительно

- **CI/CD**: См. [CI-CD-QUICKSTART.md](CI-CD-QUICKSTART.md) для быстрого старта CI/CD

---

> 💡 **Совет**: Начните с [docs/QUICK_START.md](docs/QUICK_START.md) для быстрого запуска проекта.