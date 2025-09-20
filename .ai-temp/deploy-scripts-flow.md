# Пошаговое объяснение скриптов деплоя

## Общая структура всех скриптов деплоя

Все три скрипта (`deploy-production.ps1`, `deploy-staging.ps1`, `deploy-test.ps1`) имеют одинаковую структуру, но работают с разными окружениями.

## Пошаговый процесс деплоя

### Шаг 1: Инициализация
```powershell
param([string]$Environment = "production|staging|test")
Write-Host "Starting [environment] deployment for [domain]..." -ForegroundColor Green
```
- Принимает параметр окружения
- Выводит сообщение о начале деплоя

### Шаг 2: Проверка и генерация VAPID ключей
```powershell
# Проверяет существование файла env.[environment]
if (Test-Path "env.[environment]") {
    # Читает содержимое файла
    $envContent = Get-Content "env.[environment]" -Raw
    # Проверяет, настроены ли VAPID ключи
    if ($envContent -match "VAPID_PUBLIC_KEY=$" -or $envContent -match "VAPID_PRIVATE_KEY=$") {
        # Генерирует VAPID ключи если они не настроены
        & ".\scripts\generate-vapid-production.ps1" -Environment "[environment]"
    }
}
```

**Что происходит:**
- Проверяет существование файла окружения (`env.production`, `env.staging`, `env.test`)
- Читает содержимое файла
- Проверяет, есть ли пустые VAPID ключи (`VAPID_PUBLIC_KEY=` или `VAPID_PRIVATE_KEY=`)
- Если ключи не настроены, запускает скрипт генерации VAPID ключей

### Шаг 3: Загрузка переменных окружения
```powershell
if (Test-Path "env.[environment]") {
    Get-Content "env.[environment]" | ForEach-Object {
        if ($_ -match "^([^#][^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
        }
    }
}
```

**Что происходит:**
- Читает файл окружения построчно
- Пропускает комментарии (строки начинающиеся с `#`)
- Извлекает пары ключ=значение
- Устанавливает переменные окружения для текущего процесса PowerShell

### Шаг 4: Остановка существующих контейнеров
```powershell
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.[environment].yml down
```

**Что происходит:**
- Останавливает и удаляет все контейнеры из соответствующего Docker Compose файла
- Освобождает порты и ресурсы

### Шаг 5: Сборка и запуск сервисов
```powershell
Write-Host "Building and starting [environment] services..." -ForegroundColor Yellow
docker-compose -f docker-compose.[environment].yml up -d --build
```

**Что происходит:**
- `--build` - пересобирает Docker образы
- `-d` - запускает контейнеры в фоновом режиме (detached)
- Запускает все сервисы из соответствующего Docker Compose файла

### Шаг 6: Ожидание готовности сервисов
```powershell
Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 30
```

**Что происходит:**
- Ждет 30 секунд, чтобы сервисы успели запуститься
- Позволяет базам данных инициализироваться
- Дает время на выполнение health checks

### Шаг 7: Проверка здоровья сервисов
```powershell
Write-Host "Checking service health..." -ForegroundColor Yellow
$apiHealth = Invoke-RestMethod -Uri "https://[domain]/health" -Method Get -ErrorAction SilentlyContinue
if ($apiHealth) {
    Write-Host "✅ API is healthy" -ForegroundColor Green
} else {
    Write-Host "❌ API health check failed" -ForegroundColor Red
}
```

**Что происходит:**
- Отправляет HTTP GET запрос на endpoint `/health`
- Проверяет, отвечает ли API
- Выводит результат проверки

### Шаг 8: Показ запущенных контейнеров
```powershell
Write-Host "Running containers:" -ForegroundColor Yellow
docker ps --filter "name=inventory"
```

**Что происходит:**
- Показывает список всех запущенных Docker контейнеров
- Фильтрует только контейнеры с именем содержащим "inventory"

### Шаг 9: Завершение деплоя
```powershell
Write-Host "[Environment] deployment completed!" -ForegroundColor Green
Write-Host "Application is available at: https://[domain]" -ForegroundColor Cyan
Write-Host "API endpoint: https://[domain]/api" -ForegroundColor Cyan
Write-Host "Health check: https://[domain]/health" -ForegroundColor Cyan
```

**Что происходит:**
- Выводит сообщение об успешном завершении
- Показывает URL-адреса для доступа к приложению

## Различия между окружениями

### Production (`deploy-production.ps1`)
- **Домен:** `warehouse.cuby`
- **Docker Compose:** `docker-compose.prod.yml`
- **Файл окружения:** `env.production`
- **Обязательные VAPID ключи:** Да

### Staging (`deploy-staging.ps1`)
- **Домен:** `staging.warehouse.cuby`
- **Docker Compose:** `docker-compose.staging.yml`
- **Файл окружения:** `env.staging`
- **Обязательные VAPID ключи:** Да

### Test (`deploy-test.ps1`)
- **Домен:** `test.warehouse.cuby`
- **Docker Compose:** `docker-compose.test.yml`
- **Файл окружения:** `env.test`
- **Обязательные VAPID ключи:** Опционально (для тестирования)

## Команды для запуска

```powershell
# Production
.\deploy-production.ps1

# Staging
.\deploy-staging.ps1

# Test
.\deploy-test.ps1
```

## Что происходит в Docker Compose файлах

Каждый Docker Compose файл запускает:
1. **PostgreSQL** - база данных
2. **Inventory API** - .NET Core Web API
3. **Inventory Web** - Blazor WebAssembly клиент
4. **Nginx** - reverse proxy и веб-сервер

## Безопасность

- VAPID ключи генерируются автоматически если не настроены
- Приватные ключи никогда не коммитятся в Git
- Переменные окружения загружаются из файлов окружения
- Используется HTTPS для всех окружений
