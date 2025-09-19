# Скрипты запуска приложения

Набор PowerShell скриптов для удобного запуска Inventory Control System v2.

## 🚀 Быстрый старт

```powershell
# Полный запуск приложения (API + Client + браузер)
.\start-apps.ps1

# Быстрый запуск без проверок
.\start-apps.ps1 -Quick

# Только API сервер
.\start-apps.ps1 -ApiOnly

# Только Web Client
.\start-apps.ps1 -ClientOnly
```

## 📁 Доступные скрипты

- **`start-apps.ps1`** - Основной скрипт (рекомендуется)
- **`start-api.ps1`** - Только API сервер
- **`start-client.ps1`** - Только Web Client
- **`start-quick.ps1`** - Быстрый запуск

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
