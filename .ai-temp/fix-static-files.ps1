# Скрипт для исправления проблемы со статическими файлами
Write-Host "🔧 Исправление проблемы со статическими файлами..." -ForegroundColor Yellow

# Остановить все процессы dotnet
Write-Host "⏹️ Остановка всех процессов dotnet..." -ForegroundColor Blue
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Очистить кэш сборки
Write-Host "🧹 Очистка кэша сборки..." -ForegroundColor Blue
dotnet clean
dotnet build --no-restore

# Восстановить пакеты
Write-Host "📦 Восстановление пакетов..." -ForegroundColor Blue
dotnet restore

# Пересобрать проект
Write-Host "🔨 Пересборка проекта..." -ForegroundColor Blue
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Сборка успешна!" -ForegroundColor Green
    
    # Запустить приложения
    Write-Host "🚀 Запуск приложений..." -ForegroundColor Green
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\..'; .\start-apps.ps1"
    
    Write-Host "🎉 Приложения запущены! Проверьте браузер на http://localhost:5001" -ForegroundColor Green
    Write-Host "📝 Изменения:" -ForegroundColor Cyan
    Write-Host "   - Добавлен UseStaticFiles() в API" -ForegroundColor White
    Write-Host "   - Добавлен UseBlazorFrameworkFiles() в API" -ForegroundColor White
    Write-Host "   - Добавлен MapFallbackToFile() в API" -ForegroundColor White
    Write-Host "   - Исправлены пути к статическим файлам в index.html" -ForegroundColor White
    Write-Host "   - Добавлена ссылка на Blazor WebAssembly Server" -ForegroundColor White
} else {
    Write-Host "Error: Build failed! Check logs above." -ForegroundColor Red
}
