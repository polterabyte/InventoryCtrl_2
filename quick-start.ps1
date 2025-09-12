# Быстрый запуск Inventory Control
# Простая версия без проверок портов

Write-Host "=== Quick Start - Inventory Control ===" -ForegroundColor Green
Write-Host ""

# Запуск API сервера в фоне
Write-Host "Запуск API сервера..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.API/Inventory.API.csproj" -WindowStyle Normal

# Небольшая пауза
Start-Sleep -Seconds 3

# Запуск Web клиента
Write-Host "Запуск Web клиента..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.Web/Inventory.Web.csproj" -WindowStyle Normal

Write-Host ""
Write-Host "Приложения запущены!" -ForegroundColor Green
Write-Host "API:  https://localhost:7000" -ForegroundColor White
Write-Host "Web:  https://localhost:5001" -ForegroundColor White
Write-Host ""
Write-Host "Закройте окна приложений для остановки" -ForegroundColor Yellow
