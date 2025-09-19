# 🚀 Docker Quick Start Guide

Быстрый старт для развертывания Inventory Control System с Docker и nginx.

## ⚡ Быстрый запуск

### 1. Запуск в режиме разработки
```powershell
.\quick-deploy.ps1
```

### 2. Запуск в production режиме
```powershell
.\quick-deploy.ps1 -Environment production -GenerateSSL
```

### 3. Очистка и перезапуск
```powershell
.\quick-deploy.ps1 -Clean
```

## 🛠️ Альтернативные команды

### Через Makefile (если установлен make)
```bash
# Development
make dev-build

# Production
make prod-build

# Генерация SSL
make ssl
```

### Через PowerShell скрипты
```powershell
# Сборка образов
.\docker-build.ps1

# Запуск в development
.\docker-run.ps1 -Environment development -Build

# Запуск в production
.\docker-run.ps1 -Environment production -Build
```

## 🌐 Доступ к приложению

После успешного запуска:

- **Web Application**: http://localhost
- **API**: http://localhost:5000
- **API Swagger**: http://localhost:5000/swagger
- **Database**: localhost:5432

## 📊 Управление

### Просмотр статуса
```powershell
docker ps --filter "name=inventory-"
```

### Просмотр логов
```powershell
# Все сервисы
docker-compose logs -f

# Конкретный сервис
docker-compose logs -f inventory-api
```

### Остановка
```powershell
docker-compose down
```

### Полная очистка
```powershell
docker-compose down -v
docker system prune -a
```

## 🔧 Настройка

### Переменные окружения
Скопируйте `.env.example` в `.env` и настройте:

```bash
cp .env.example .env
```

### SSL сертификаты (для production)
```powershell
.\scripts\generate-ssl.ps1
```

## 🆘 Устранение проблем

### Проблемы с портами
```powershell
# Проверка занятых портов
netstat -an | findstr ":80\|:5000\|:5432"

# Освобождение порта PostgreSQL (если занят)
netstat -ano | findstr :5432
taskkill /F /PID <PID>
```

### Проблемы с Docker
```powershell
# Перезапуск Docker Desktop
# Закройте и откройте Docker Desktop

# Очистка системы
docker system prune -a
docker container prune -f
```

### Проблемы с базой данных
```powershell
# Проверка подключения
docker exec -it inventory-postgres psql -U postgres -d inventorydb

# Очистка контейнеров
docker-compose down -v --remove-orphans
```

### Проблемы с пакетами .NET
Если возникают ошибки с пакетами NuGet:
```powershell
# Очистка кэша NuGet
dotnet nuget locals all --clear

# Восстановление пакетов
dotnet restore
```

### Проблемы с PowerShell скриптами
```powershell
# Разрешить выполнение скриптов
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Проверить кодировку файлов
Get-Content docker-build.ps1 -Encoding UTF8
```

## 📚 Дополнительная документация

- [Полное руководство по развертыванию](DOCKER_DEPLOYMENT.md)
- [Архитектура системы](docs/ARCHITECTURE.md)
- [API документация](docs/API.md)
