# Скрипт запуска Inventory Control приложения
# Запускает API сервер, затем Web клиент

# Устанавливаем кодировку UTF-8 для корректного отображения русского текста
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "=== Inventory Control Application Launcher ===" -ForegroundColor Green
Write-Host ""

# Проверяем, что мы в корневой директории проекта
if (-not (Test-Path "InventoryCtrl_2.sln")) {
    Write-Host "Ошибка: Запустите скрипт из корневой директории проекта (где находится InventoryCtrl_2.sln)" -ForegroundColor Red
    exit 1
}

# Функция для проверки доступности порта
function Test-Port {
    param([int]$Port)
    try {
        $connection = New-Object System.Net.Sockets.TcpClient
        $connection.Connect("localhost", $Port)
        $connection.Close()
        return $true
    }
    catch {
        return $false
    }
}

# Функция ожидания запуска сервера
function Wait-ForServer {
    param([int]$Port, [string]$ServiceName)
    Write-Host "Ожидание запуска $ServiceName на порту $Port..." -ForegroundColor Yellow
    $timeout = 30
    $elapsed = 0
    
    while ($elapsed -lt $timeout) {
        if (Test-Port $Port) {
            Write-Host "$ServiceName успешно запущен на порту $Port" -ForegroundColor Green
            return $true
        }
        Start-Sleep -Seconds 2
        $elapsed += 2
        Write-Host "." -NoNewline -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Таймаут ожидания запуска $ServiceName" -ForegroundColor Red
    return $false
}

try {
    # Шаг 1: Запуск API сервера
    Write-Host "1. Запуск API сервера..." -ForegroundColor Cyan
    Write-Host "   Порт: https://localhost:7000, http://localhost:5000" -ForegroundColor Gray
    
    $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.API/Inventory.API.csproj" -PassThru -WindowStyle Normal
    
    # Ждем запуска API сервера (проверяем HTTP порт 5000)
    if (-not (Wait-ForServer -Port 5000 -ServiceName "API Server")) {
        Write-Host "Не удалось запустить API сервер" -ForegroundColor Red
        $apiProcess.Kill()
        exit 1
    }
    
    Write-Host ""
    
    # Шаг 2: Запуск Web клиента
    Write-Host "2. Запуск Web клиента..." -ForegroundColor Cyan
    Write-Host "   Порт: https://localhost:5001, http://localhost:5142" -ForegroundColor Gray
    
    $webProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.Web/Inventory.Web.csproj" -PassThru -WindowStyle Normal
    
    # Ждем запуска Web клиента (проверяем HTTP порт 5142)
    if (-not (Wait-ForServer -Port 5142 -ServiceName "Web Client")) {
        Write-Host "Не удалось запустить Web клиент" -ForegroundColor Red
        $webProcess.Kill()
        $apiProcess.Kill()
        exit 1
    }
    
    Write-Host ""
    Write-Host "=== Приложения успешно запущены! ===" -ForegroundColor Green
    Write-Host "API Server:  https://localhost:7000" -ForegroundColor White
    Write-Host "Web Client:  https://localhost:5001" -ForegroundColor White
    Write-Host ""
    Write-Host "Нажмите Ctrl+C для остановки всех приложений" -ForegroundColor Yellow
    
    # Ожидание завершения
    try {
        Wait-Process -Id $apiProcess.Id, $webProcess.Id
    }
    catch {
        Write-Host "Приложения остановлены" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Ошибка при запуске приложений: $($_.Exception.Message)" -ForegroundColor Red
    
    # Останавливаем процессы в случае ошибки
    if ($apiProcess -and !$apiProcess.HasExited) {
        $apiProcess.Kill()
    }
    if ($webProcess -and !$webProcess.HasExited) {
        $webProcess.Kill()
    }
    
    exit 1
}
finally {
    # Очистка процессов
    if ($apiProcess -and !$apiProcess.HasExited) {
        Write-Host "Остановка API сервера..." -ForegroundColor Yellow
        $apiProcess.Kill()
    }
    if ($webProcess -and !$webProcess.HasExited) {
        Write-Host "Остановка Web клиента..." -ForegroundColor Yellow
        $webProcess.Kill()
    }
    
    Write-Host "Все приложения остановлены" -ForegroundColor Green
}
