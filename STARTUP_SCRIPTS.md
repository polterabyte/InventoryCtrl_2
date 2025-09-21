# Deploy скрипты запуска приложения

Набор deploy скриптов для удобного запуска Inventory Control System v2.

## 🚀 Быстрый старт

```powershell
# Быстрый запуск через deploy
.\deploy\quick-deploy.ps1

# Полный развертывание
.\deploy\deploy-all.ps1

# Просмотр логов
docker-compose logs -f

# Остановка всех сервисов
docker-compose down
```

## 📁 Доступные скрипты

- **`deploy\quick-deploy.ps1`** - Быстрый запуск (рекомендуется)
- **`deploy\deploy-all.ps1`** - Полное развертывание
- **`deploy\deploy-production.ps1`** - Production развертывание
- **`deploy\deploy-staging.ps1`** - Staging развертывание

## 🌐 URL адреса

- **Web приложение**: https://localhost:7001
- **API сервер**: https://localhost:7000
- **Swagger**: https://localhost:7000/swagger

## 👤 Тестовые данные

- **Пользователь**: admin
- **Пароль**: Admin123!

## 🛑 Остановка

Нажмите `Ctrl+C` в окне терминала.

## 📋 Требования

- .NET 9.0 SDK
- PowerShell 5.1+
- PostgreSQL (для базы данных)
