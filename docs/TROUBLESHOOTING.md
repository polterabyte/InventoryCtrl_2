# Устранение неполадок

Руководство по решению распространенных проблем при работе с Inventory Control System.

## 🐳 Docker проблемы

### Проблема: Docker Desktop не запускается
**Симптомы:**
```
error during connect: Get "http://%2F%2F.%2Fpipe%2FdockerDesktopLinuxEngine/v1.51/version": 
open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified.
```

**Решение:**
1. Закройте Docker Desktop
2. Запустите Docker Desktop заново
3. Дождитесь полной загрузки (зеленый индикатор)
4. Попробуйте команду снова

### Проблема: Порт уже используется
**Симптомы:**
```
Bind for 0.0.0.0:5432 failed: port is already allocated
```

**Решение:**
```powershell
# Проверить занятые порты
netstat -ano | findstr :5432

# Остановить процесс (замените PID)
taskkill /F /PID <PID>

# Или очистить все контейнеры
docker-compose down -v --remove-orphans
```

### Проблема: Конфликт имен контейнеров
**Симптомы:**
```
Conflict. The container name "/inventory-postgres" is already in use
```

**Решение:**
```powershell
# Остановить все контейнеры
docker-compose down

# Удалить контейнеры
docker container prune -f

# Запустить заново
.\quick-deploy.ps1
```

## 🔧 .NET проблемы

### Проблема: Ошибки восстановления пакетов
**Симптомы:**
```
Unable to find a stable package Microsoft.AspNetCore.RateLimiting
Version conflict detected for Microsoft.AspNetCore.Components.Web
```

**Решение:**
```powershell
# Очистить кэш NuGet
dotnet nuget locals all --clear

# Восстановить пакеты
dotnet restore

# Если не помогает, пересобрать
dotnet clean
dotnet build
```

### Проблема: Ошибки компиляции
**Симптомы:**
```
error CS8601: Possible null reference assignment
warning CS1998: This async method lacks 'await' operators
```

**Решение:**
1. Проверьте настройки Nullable в .csproj файлах
2. Добавьте `await` в async методы или уберите `async`
3. Исправьте null reference warnings

## 📝 PowerShell проблемы

### Проблема: Ошибки выполнения скриптов
**Симптомы:**
```
cannot be loaded because running scripts is disabled on this system
```

**Решение:**
```powershell
# Разрешить выполнение скриптов
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Проверить политику
Get-ExecutionPolicy
```

### Проблема: Ошибки кодировки
**Симптомы:**
```
В строке отсутствует завершающий символ: ".
```

**Решение:**
```powershell
# Проверить кодировку файла
Get-Content docker-build.ps1 -Encoding UTF8

# Пересоздать файл с правильной кодировкой
[System.IO.File]::WriteAllText('docker-build.ps1', $content, [System.Text.Encoding]::UTF8)
```

## 🗄️ База данных проблемы

### Проблема: PostgreSQL не запускается
**Симптомы:**
```
Failed to start PostgreSQL service
```

**Решение:**
```powershell
# Проверить статус службы
Get-Service postgresql*

# Запустить службу
Start-Service postgresql-x64-14

# Проверить подключение
psql -h localhost -U postgres -d postgres
```

### Проблема: Ошибки подключения к БД
**Симптомы:**
```
Connection refused to database
```

**Решение:**
1. Проверьте строку подключения в `appsettings.json`
2. Убедитесь, что PostgreSQL запущен
3. Проверьте настройки firewall
4. Проверьте правильность пароля

## 🌐 Сетевые проблемы

### Проблема: CORS ошибки
**Симптомы:**
```
Access to fetch at 'http://localhost:5000/api/...' from origin 'http://localhost:5001' 
has been blocked by CORS policy
```

**Решение:**
1. Проверьте настройки CORS в `appsettings.json`
2. Добавьте нужные origins в `AllowedOrigins`
3. Перезапустите API сервер

### Проблема: HTTPS ошибки
**Симптомы:**
```
SSL certificate verification failed
```

**Решение:**
```bash
# Создать development сертификат
dotnet dev-certs https --trust

# Или отключить HTTPS в launchSettings.json
```

## 🔍 Диагностика

### Проверка статуса системы
```powershell
# Проверить процессы .NET
Get-Process dotnet

# Проверить порты
netstat -ano | findstr ":5000\|:5001\|:7000\|:7001\|:5432"

# Проверить Docker контейнеры
docker ps -a

# Проверить логи
docker-compose logs -f
```

### Проверка логов
```powershell
# API логи
Get-Content src/Inventory.API/logs/log-*.txt -Tail 20

# Docker логи
docker-compose logs inventory-api
docker-compose logs inventory-web

# Системные логи
Get-EventLog -LogName Application -Source "Docker Desktop" -Newest 10
```

## 🆘 Получение помощи

### Сбор информации для отчета об ошибке
```powershell
# Создать отчет о системе
$report = @{
    OS = (Get-ComputerInfo).WindowsProductName
    PowerShell = $PSVersionTable.PSVersion
    Docker = docker version
    DotNet = dotnet --version
    Ports = netstat -ano | findstr ":5000\|:5001\|:7000\|:7001\|:5432"
    Processes = Get-Process dotnet
}

$report | ConvertTo-Json -Depth 3 | Out-File "system-report.json"
```

### Полезные команды для отладки
```powershell
# Полная очистка и перезапуск
docker-compose down -v --remove-orphans
docker system prune -a
.\quick-deploy.ps1 -Clean

# Проверка конфигурации
Get-Content src/Inventory.API/appsettings.json | ConvertFrom-Json
Get-Content docker-compose.yml | ConvertFrom-Yaml

# Тестирование подключений
Test-NetConnection localhost -Port 5000
Test-NetConnection localhost -Port 5001
Test-NetConnection localhost -Port 5432
```

## 📚 Дополнительные ресурсы

- [Docker документация](https://docs.docker.com/)
- [.NET документация](https://docs.microsoft.com/en-us/dotnet/)
- [PostgreSQL документация](https://www.postgresql.org/docs/)
- [PowerShell документация](https://docs.microsoft.com/en-us/powershell/)

---

> 💡 **Совет**: При возникновении проблем начните с проверки логов и статуса сервисов. Большинство проблем решается перезапуском или очисткой кэша.
