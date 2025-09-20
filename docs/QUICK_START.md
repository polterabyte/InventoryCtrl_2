# Quick Start Guide

Быстрый старт для разработчиков и пользователей системы управления инвентарем.

## 🚀 Автоматический запуск (Рекомендуется)

### Deploy запуск
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

**Что делают deploy скрипты:**
- Запускают все сервисы в Docker контейнерах
- Настраивают сеть между сервисами
- Управляют жизненным циклом приложений
- Обеспечивают изоляцию окружения

## 🔧 Ручной запуск

### 1. Установка требований
- **.NET 8.0 SDK** — скачать с [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **PostgreSQL** — установить и запустить сервис

### 2. Настройка базы данных
Отредактируйте `src/Inventory.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=InventoryDb;Username=postgres;Password=your_password;"
  }
}
```

### 3. Запуск приложения

#### Терминал 1 - API Server
```bash
cd src/Inventory.API
dotnet run
```
**Порты:**
- HTTPS: https://localhost:7000
- HTTP: http://localhost:5000

#### Терминал 2 - Web Client
```bash
cd src/Inventory.Web.Client
dotnet run
```
**Порты:**
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5001

## 🌐 Доступ к приложению

После запуска откройте браузер:

- **Web приложение**: https://localhost:7001
- **API документация**: https://localhost:7000/swagger
- **API Health Check**: https://localhost:7000/health

## 👤 Тестовые данные

### Автоматически созданный пользователь
- **Username**: `admin`
- **Password**: `Admin123!`
- **Email**: `admin@localhost`
- **Role**: `Admin`

**Примечание**: Пользователь создается автоматически при первом запуске через `DbInitializer`.

## 🧪 Быстрое тестирование

```powershell
# Запуск всех тестов
dotnet test

# Запуск через PowerShell скрипт
.\test\run-tests.ps1

# Конкретный тип тестов
.\test\run-tests.ps1 -Project unit
.\test\run-tests.ps1 -Project integration
.\test\run-tests.ps1 -Project component

# С покрытием кода
.\test\run-tests.ps1 -Coverage
```

## 🔧 Устранение неполадок

### Порт занят
```powershell
# Проверить занятые порты
netstat -ano | findstr :5000
netstat -ano | findstr :7000
netstat -ano | findstr :5001
netstat -ano | findstr :7001

# Освободить порт (замените PID)
taskkill /PID <PID> /F
```

### CORS ошибки
Проверьте настройки CORS в `src/Inventory.API/appsettings.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:7001",
      "http://localhost:5001"
    ]
  }
}
```

### База данных недоступна
```powershell
# Проверить статус PostgreSQL
Get-Service postgresql*

# Запустить PostgreSQL
Start-Service postgresql-x64-14

# Проверить подключение
psql -h localhost -U postgres -d postgres
```

### Проблемы с HTTPS
Если возникают проблемы с SSL сертификатами:
```bash
# Создать development сертификат
dotnet dev-certs https --trust

# Или отключить HTTPS в Properties/launchSettings.json
```

### Проблемы с PowerShell
```powershell
# Разрешить выполнение скриптов
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Проверить версию PowerShell
$PSVersionTable.PSVersion
```

## 📊 Мониторинг

### Проверка статуса приложений
```powershell
# Проверить процессы .NET
Get-Process dotnet

# Проверить логи API
Get-Content src/Inventory.API/logs/log-*.txt -Tail 20

# Проверить статус портов
Test-NetConnection localhost -Port 7000
Test-NetConnection localhost -Port 7001
```

### Логи приложения
- **API логи**: `src/Inventory.API/logs/`
- **Браузерные логи**: F12 → Console
- **Тестовые логи**: `test/TestResults/`

## 🛑 Остановка приложений

### Docker остановка
```powershell
# Остановить все сервисы
docker-compose down

# Остановить и удалить контейнеры
docker-compose down --remove-orphans

# Остановить с удалением volumes
docker-compose down -v
```

### Ручной запуск (если используется)
- Нажмите `Ctrl+C` в каждом терминале

### Принудительная остановка
```powershell
# Остановить все Docker контейнеры проекта
docker-compose kill

# Остановить все процессы dotnet (если запущены вручную)
Get-Process dotnet | Stop-Process -Force
```

## 📈 Следующие шаги

После успешного запуска:

1. **Изучите API**: Откройте https://localhost:7000/swagger
2. **Протестируйте функциональность**: Войдите как admin
3. **Запустите тесты**: `.\test\run-tests.ps1`
4. **Изучите код**: Начните с [ARCHITECTURE.md](ARCHITECTURE.md)
5. **Настройте CI/CD**: См. [CI-CD-QUICKSTART.md](../CI-CD-QUICKSTART.md)

---

> 💡 **Совет**: При проблемах с запуском начните с проверки портов и статуса PostgreSQL.
