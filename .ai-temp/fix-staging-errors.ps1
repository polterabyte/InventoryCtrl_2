# Скрипт для исправления ошибок в staging окружении
Write-Host "Исправление ошибок в staging окружении..." -ForegroundColor Green

# Переходим в корневую директорию проекта
Set-Location "C:\rec\prg\repo\InventoryCtrl_2"

Write-Host "1. Очистка предыдущих сборок..." -ForegroundColor Yellow
dotnet clean

Write-Host "2. Восстановление пакетов..." -ForegroundColor Yellow
dotnet restore

Write-Host "3. Сборка проекта..." -ForegroundColor Yellow
dotnet build --configuration Release

Write-Host "4. Сборка Docker образов..." -ForegroundColor Yellow
docker-compose -f docker-compose.staging.yml build

Write-Host "5. Остановка текущих контейнеров..." -ForegroundColor Yellow
docker-compose -f docker-compose.staging.yml down

Write-Host "6. Запуск обновленных контейнеров..." -ForegroundColor Yellow
docker-compose -f docker-compose.staging.yml up -d

Write-Host "7. Ожидание запуска сервисов..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host "8. Проверка статуса контейнеров..." -ForegroundColor Yellow
docker-compose -f docker-compose.staging.yml ps

Write-Host "9. Проверка логов..." -ForegroundColor Yellow
Write-Host "Логи API:" -ForegroundColor Cyan
docker-compose -f docker-compose.staging.yml logs --tail=20 inventory-api

Write-Host "Логи Web Client:" -ForegroundColor Cyan
docker-compose -f docker-compose.staging.yml logs --tail=20 inventory-web-client

Write-Host "Исправления применены! Проверьте https://staging.warehouse.cuby/admin/reference-data" -ForegroundColor Green
