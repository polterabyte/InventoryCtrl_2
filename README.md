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
# Полный запуск с проверками
.\start-apps.ps1

# Быстрый запуск без проверок
.\start-apps.ps1 -Quick

# Только API сервер
.\start-apps.ps1 -ApiOnly

# Только Web Client
.\start-apps.ps1 -ClientOnly
```

**Доступные скрипты:**
- `start-apps.ps1` - Основной скрипт (рекомендуется)
- `start-api.ps1` - Только API сервер
- `start-client.ps1` - Только Web Client
- `start-quick.ps1` - Быстрый запуск

Подробная документация: [STARTUP_SCRIPTS.md](STARTUP_SCRIPTS.md)

### Ручной запуск

1. **Установка требований:**
   - .NET 8.0 SDK
   - PostgreSQL 14+

2. **Настройка базы данных:**
   Отредактируйте `src/Inventory.API/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=InventoryDb;Username=postgres;Password=your_password;"
     }
   }
   ```

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
- **Web приложение**: https://localhost:7001
- **API документация**: https://localhost:7000/swagger
- **Тестовый пользователь**: admin / Admin123!

## 🐳 Docker развертывание

### Быстрый запуск с Docker
```powershell
# Полное развертывание с Docker
.\quick-deploy.ps1

# Очистка и перезапуск
.\quick-deploy.ps1 -Clean

# Production развертывание с SSL
.\quick-deploy.ps1 -Environment production -GenerateSSL
```

**Docker URLs:**
- **Web приложение**: http://localhost
- **API**: http://localhost:5000
- **API Swagger**: http://localhost:5000/swagger

Подробная документация: [DOCKER_QUICK_START.md](DOCKER_QUICK_START.md)

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
Конфигурация портов централизована в `ports.json`:
```json
{
  "ApiHttp": 5000,
  "ApiHttps": 7000,
  "WebHttp": 5001,
  "WebHttps": 7001
}
```

### Управление пакетами
Все версии пакетов управляются через `Directory.Packages.props` для обеспечения совместимости.

## 🛡 Безопасность

- **JWT аутентификация** с refresh токенами
- **Ролевая авторизация** (Admin/Manager/User)
- **Rate limiting** для защиты от злоупотреблений
- **CORS настройки** для безопасных межсайтовых запросов
- **Аудит действий** всех пользователей
- **HTTPS обязательно** в production

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
