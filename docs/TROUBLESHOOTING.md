# Устранение неполадок

Руководство по решению распространенных проблем при работе с Inventory Control System.

## 🌐 Nginx проблемы

### Проблема: nginx контейнер не запускается
**Симптомы:**
```
nginx: [emerg] "proxy_pass" cannot have URI part in location given by regular expression, or inside named location, or inside "if" statement, or inside "limit_except" block
```

**Решение:**
1. Проверьте конфигурацию nginx в `nginx/conf.d/locations.conf`
2. Убедитесь, что в именованных локациях `@fallback` нет URI части в `proxy_pass`
3. Исправьте: `proxy_pass http://inventory_web/;` → `proxy_pass http://inventory_web;`

### Проблема: Deprecated http2 директива
**Симптомы:**
```
nginx: [warn] the "listen ... http2" directive is deprecated, use the "http2" directive instead
```

**Решение:**
Замените в `nginx/nginx.conf`:
```nginx
listen 443 ssl http2;
```
на:
```nginx
listen 443 ssl;
http2 on;
```

### Проблема: Отсутствуют SSL сертификаты
**Симптомы:**
```
nginx: [emerg] cannot load certificate "/etc/nginx/ssl/warehouse.cuby.crt": BIO_new_file() failed
```

**Решение:**
1. Сгенерируйте SSL сертификаты:
   ```powershell
   .\generate-ssl-warehouse.ps1
   ```
2. Или создайте вручную через Docker:
   ```powershell
   docker run --rm -v "${PWD}/nginx/ssl:/ssl" alpine/openssl req -x509 -newkey rsa:4096 -keyout /ssl/warehouse.cuby.key -out /ssl/warehouse.cuby.crt -days 365 -nodes -subj "/C=US/ST=State/L=City/O=Organization/OU=OrgUnit/CN=warehouse.cuby"
   ```

### Проблема: Неправильные имена upstream серверов
**Симптомы:**
```
nginx: [error] host not found in upstream "inventory-api" in /etc/nginx/nginx.conf
```

**Решение:**
Обновите имена upstream серверов в `nginx/nginx.conf`:
```nginx
upstream inventory_api {
    server inventory-api-staging:80;  # Вместо inventory-api:80
}

upstream inventory_web {
    server inventory-web-staging:80;  # Вместо inventory-web:80
}
```

### Проблема: Нельзя подключиться с внешних компьютеров
**Симптомы:**
- Приложение работает только на localhost
- Внешние подключения не проходят

**Решение:**
1. Добавьте IP адрес в конфигурацию nginx
2. Создайте SSL сертификат для IP адреса
3. Настройте брандмауэр Windows:
   ```powershell
   New-NetFirewallRule -DisplayName "Allow HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
   New-NetFirewallRule -DisplayName "Allow HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
   ```

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

### Проблема: DirectoryNotFoundException для wwwroot в Inventory.Shared
**Симптомы:**
```
Unhandled exception. System.IO.DirectoryNotFoundException: C:\rec\prg\repo\InventoryCtrl_2\src\Inventory.Shared\wwwroot\
   at Microsoft.Extensions.FileProviders.PhysicalFileProvider..ctor(String root, ExclusionFilters filters)
   at Microsoft.AspNetCore.Hosting.StaticWebAssets.StaticWebAssetsLoader.<>c.<UseStaticWebAssetsCore>b__1_0(String contentRoot)
```

**Причина:**
- Проект `Inventory.Shared` использует SDK `Microsoft.NET.Sdk.Razor`
- Этот SDK автоматически генерирует Static Web Assets из папки `wwwroot`
- ASP.NET Core ожидает найти эту папку при запуске

**Решение:**
```powershell
# Создать пустую папку wwwroot в Inventory.Shared
mkdir "src\Inventory.Shared\wwwroot"
```

**Альтернативные решения:**
1. **Изменить SDK** в `Inventory.Shared.csproj` с `Microsoft.NET.Sdk.Razor` на `Microsoft.NET.Sdk` (если не нужны Razor компоненты)
2. **Настроить исключение** Static Web Assets в `.csproj` файле

**Примечание:** Пустая папка `wwwroot` в `Inventory.Shared` - это стандартная практика для проектов с Razor SDK. Все статические файлы должны находиться в `Inventory.UI/wwwroot/`.

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
Npgsql.NpgsqlException (0x80004005): Failed to connect to 127.0.0.1:5432
 ---> System.Net.Sockets.SocketException (10061): Подключение не установлено, т.к. конечный компьютер отверг запрос на подключение.
```

**Решение:**
1. **Запустить PostgreSQL через Docker:**
   ```powershell
   # Запустить базу данных
   docker-compose up -d postgres
   
   # Или полный запуск системы
   .\quick-deploy.ps1
   ```

2. **Проверить статус PostgreSQL:**
   ```powershell
   # Проверить контейнеры
   docker ps | findstr postgres
   
   # Проверить логи
   docker-compose logs postgres
   ```

3. **Проверить строку подключения** в `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=inventorydb;Username=postgres;Password=postgres"
   }
   ```

4. **Проверить порт 5432:**
   ```powershell
   # Проверить занятые порты
   netstat -ano | findstr :5432
   
   # Проверить подключение
   Test-NetConnection localhost -Port 5432
   ```

5. **Если PostgreSQL установлен локально:**
   ```powershell
   # Проверить службу
   Get-Service postgresql*
   
   # Запустить службу
   Start-Service postgresql-x64-14
   ```

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
