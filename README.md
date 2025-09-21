# Inventory Control System v2

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-purple)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Database-blue)
![License](https://img.shields.io/badge/License-MIT-green)

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
- Bootstrap для UI
- Blazored.LocalStorage
- Компонентная архитектура

**Тестирование:**
- xUnit для unit тестов
- bUnit для тестирования Blazor компонентов
- FluentAssertions для читаемых утверждений
- Moq для мокирования

## 🚀 Быстрый старт

### Автоматический запуск (рекомендуется)
```powershell
# Быстрый запуск через deploy
.\deploy\quick-deploy.ps1

# Полный развертывание
.\deploy\deploy-all.ps1

# Остановка всех сервисов
docker-compose down
```

**Deploy скрипты:**
- `deploy\quick-deploy.ps1` - Быстрый запуск (рекомендуется)
- `deploy\deploy-all.ps1` - Полное развертывание
- `deploy\deploy-production.ps1` - Production развертывание
- `deploy\deploy-staging.ps1` - Staging развертывание

Подробная документация: [docs/DEPLOYMENT_SCRIPTS.md](docs/DEPLOYMENT_SCRIPTS.md)

### Ручной запуск

1. **Установка требований:**
   - .NET 8.0 SDK
   - PostgreSQL 14+

2. **Настройка базы данных:**
   Пароли/строки подключения не редактируются в исходниках. Используйте переменные окружения или User Secrets (см. раздел «Обязательные переменные окружения (ENV)» ниже).

3. **Запуск приложения:**
   ```bash
   # Терминал 1 - API Server
   cd src/Inventory.API
   dotnet run

   # Терминал 2 - Web Client  
   cd src/Inventory.Web.Client
   dotnet run
   ```

### Доступ к приложению

URL и порты берутся из переменных окружения (.env/User Secrets) и конфигурации контейнеров.

- Dev (по умолчанию):
  - Web: https://localhost
  - API Swagger: https://localhost/swagger
- Docker/Deploy: значения зависят от .env (например, `SERVER_IP`, `CORS_ALLOWED_ORIGINS`, `ApiUrl`).
  - Web: https://<ваш_домен_или_IP>
  - API Swagger: https://<ваш_домен_или_IP>/swagger

> Примечание: При первом подключении в локальной среде возможны предупреждения о самоподписанном сертификате.

## 🐳 Docker развертывание

### Быстрый запуск с Docker
```powershell
# Полное развертывание с Docker
.\deploy\quick-deploy.ps1

# Очистка и перезапуск
.\deploy\quick-deploy.ps1 -Clean

# Production развертывание с SSL
.\deploy\quick-deploy.ps1 -Environment production -GenerateSSL
```

**Docker URLs:**
- **Web приложение**: http://localhost
- **API**: http://localhost:5000
- **API Swagger**: http://localhost:5000/swagger

Подробная документация: [docs/DEPLOYMENT_SCRIPTS.md](docs/DEPLOYMENT_SCRIPTS.md)

## 🧪 Тестирование

```powershell
# Запуск всех тестов
.\test\run-tests.ps1

# Конкретные типы тестов
.\test\run-tests.ps1 -Project unit
.\test\run-tests.ps1 -Project integration
.\test\run-tests.ps1 -Project component

# С покрытием кода
.\test\run-tests.ps1 -Coverage
```

## 📚 Документация

- **[Архитектура](docs/ARCHITECTURE.md)** — подробное описание архитектуры системы
- **[API](docs/API.md)** — документация REST API endpoints
- **[Быстрый старт](docs/QUICK_START.md)** — детальное руководство по запуску
- **[Разработка](docs/DEVELOPMENT.md)** — руководство для разработчиков
- **[Тестирование](docs/TESTING.md)** — стратегия и практики тестирования
- **[Устранение неполадок](docs/TROUBLESHOOTING.md)** — решение распространенных проблем
- **[Уведомления](docs/NOTIFICATION_SYSTEM.md)** — система уведомлений в реальном времени
- **[Roadmap](docs/DEVELOPMENT_ROADMAP.md)** — план развития системы уведомлений
- **[GitHub Issues](docs/GITHUB_ISSUES.md)** — детальные задачи для разработки
- **[Changelog](CHANGELOG.md)** — история изменений проекта

## 🔧 Конфигурация

### Порты приложения
Порты управляются через переменные окружения и docker-compose/.env (не в исходном коде). Типичные значения по умолчанию:

- Dev:
  - API HTTP: `http://localhost:5000`
  - API HTTPS: `https://localhost:7000`
  - PostgreSQL: `localhost:5432`
- Docker/Production: зависит от .env (например, публикация 80/443 на хосте)

### Управление пакетами
Все версии пакетов управляются через `Directory.Packages.props` для обеспечения совместимости.

## 🛡 Безопасность

- **JWT аутентификация** с refresh токенами
- **Ролевая авторизация** (Admin/Manager/User)
- **Rate limiting** для защиты от злоупотреблений
- **CORS настройки** для безопасных межсайтовых запросов
- **Аудит действий** всех пользователей
- **HTTPS обязательно** в production

## 🔧 Обязательные переменные окружения (ENV)

Для корректной и безопасной работы приложения требуется задать ряд переменных окружения (или User Secrets для локальной разработки):

- Все константы (домены, порты, URL, ключи, строки подключения) задаются через `.env`/переменные окружения (или User Secrets в Dev). Код не содержит хардкод‑домены/IP.

- API (src/Inventory.API)
  - `ConnectionStrings__DefaultConnection` — строка подключения к PostgreSQL (не храните пароль в репозитории)
  - `Jwt__Key` — секретный ключ для подписи JWT (обязателен вне Development)
  - `CORS_ALLOWED_ORIGINS` — список разрешенных Origin через запятую (например: `https://localhost,https://staging.example.com`)
  - `ADMIN_EMAIL`, `ADMIN_USERNAME`, `ADMIN_PASSWORD` — учетные данные первоначального администратора (пароль опционален; если не указан — админ создается без пароля)
  - `ApiUrl` — базовый URL API для `LocationApiService` (например: `https://localhost:7000` или ваш домен)

Пример .env (Docker/скрипты деплоя):

```env
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=inventorydb;Username=postgres;Password=CHANGE_ME
Jwt__Key=CHANGE_ME_SUPER_SECRET
CORS_ALLOWED_ORIGINS=https://localhost,https://staging.example.com,https://example.com
ADMIN_EMAIL=admin@localhost
ADMIN_USERNAME=admin
ADMIN_PASSWORD=CHANGE_ME
ApiUrl=https://localhost:7000
```

Пример User Secrets (локальная разработка):

```powershell
cd src/Inventory.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=inventorydb;Username=postgres;Password=CHANGE_ME"
dotnet user-secrets set "Jwt:Key" "CHANGE_ME_SUPER_SECRET"
dotnet user-secrets set "CORS_ALLOWED_ORIGINS" "https://localhost,https://staging.example.com"
dotnet user-secrets set "ADMIN_EMAIL" "admin@localhost"
dotnet user-secrets set "ADMIN_USERNAME" "admin"
dotnet user-secrets set "ADMIN_PASSWORD" "CHANGE_ME"
dotnet user-secrets set "ApiUrl" "https://localhost:7000"
```

Примечания:

- Секреты и пароли не должны храниться в репозитории; используйте ENV/User Secrets.
- `SignalR` клиент в браузере закреплен на версии `@microsoft/signalr@8.0.5` (см. `src/Inventory.Web.Client/wwwroot/index.html`).
- Клиентские URL (в т.ч. SignalR) формируются из конфигурации и относительных путей (`/api`, `/notificationHub`).
- Список CORS‑источников лучше поддерживать через `CORS_ALLOWED_ORIGINS`.

## 🔄 CI/CD

Проект настроен для автоматической сборки и тестирования:
- **Azure Pipelines** конфигурация в `azure-pipelines.yml`
- **Docker** поддержка для контейнеризации
- **Автоматические тесты** при каждом коммите

## 🚀 Планы развития

- **Мобильное приложение** (.NET MAUI)
- **Desktop приложение** (.NET MAUI)
- **Расширенная аналитика** с графиками
- **API для интеграций** с внешними системами
- **Многоязычность** интерфейса

## 🤝 Участие в разработке

1. Форкните репозиторий
2. Создайте feature branch (`git checkout -b feature/amazing-feature`)
3. Зафиксируйте изменения (`git commit -m 'Add amazing feature'`)
4. Отправьте в branch (`git push origin feature/amazing-feature`)
5. Создайте Pull Request

## 📄 Лицензия

Этот проект лицензирован под MIT License - см. файл [LICENSE](LICENSE) для деталей.

## 🆘 Поддержка

При возникновении проблем:
1. Проверьте [документацию](docs/)
2. Изучите [FAQ](docs/FAQ.md)
3. Создайте [Issue](https://github.com/your-repo/issues)

---

**Создано с ❤️ на .NET 8.0 и Blazor WebAssembly**
