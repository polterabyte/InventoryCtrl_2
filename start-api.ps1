# Скрипт запуска API сервера
# Inventory Control System v2 - API Server

param(
    [switch]$Verbose,
    [switch]$NoWait
)

$ErrorActionPreference = "Stop"

Write-Host " Запуск API сервера Inventory Control System v2" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

# Проверка .NET SDK
Write-Host " Проверка .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host " .NET SDK версия: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host " .NET SDK не найден. Установите .NET 9.0 SDK" -ForegroundColor Red
    exit 1
}

# Переход в директорию API
$apiPath = "src/Inventory.API"
if (-not (Test-Path $apiPath)) {
    Write-Host " Директория API не найдена: $apiPath" -ForegroundColor Red
    exit 1
}

Set-Location $apiPath
Write-Host " Рабочая директория: $(Get-Location)" -ForegroundColor Cyan

# Проверка файлов проекта
if (-not (Test-Path "Inventory.API.csproj")) {
    Write-Host " Файл проекта не найден: Inventory.API.csproj" -ForegroundColor Red
    exit 1
}

# Восстановление пакетов
Write-Host " Восстановление пакетов NuGet..." -ForegroundColor Yellow
try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        throw "Ошибка восстановления пакетов"
    }
    Write-Host " Пакеты восстановлены успешно" -ForegroundColor Green
} catch {
    Write-Host " Ошибка восстановления пакетов: $_" -ForegroundColor Red
    exit 1
}

# Сборка проекта
Write-Host " Сборка проекта..." -ForegroundColor Yellow
try {
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Ошибка сборки проекта"
    }
    Write-Host " Проект собран успешно" -ForegroundColor Green
} catch {
    Write-Host " Ошибка сборки проекта: $_" -ForegroundColor Red
    exit 1
}

# Проверка конфигурации
Write-Host " Проверка конфигурации..." -ForegroundColor Yellow
if (Test-Path "appsettings.json") {
    Write-Host " Файл конфигурации найден" -ForegroundColor Green
} else {
    Write-Host " Файл appsettings.json не найден" -ForegroundColor Yellow
}

if (Test-Path "appsettings.Development.json") {
    Write-Host " Файл конфигурации разработки найден" -ForegroundColor Green
} else {
    Write-Host " Файл appsettings.Development.json не найден" -ForegroundColor Yellow
}

# Запуск API сервера
Write-Host " Запуск API сервера..." -ForegroundColor Yellow
Write-Host " URL: https://localhost:7000" -ForegroundColor Cyan
Write-Host " Swagger: https://localhost:7000/swagger" -ForegroundColor Cyan
Write-Host " Для остановки нажмите Ctrl+C" -ForegroundColor Red
Write-Host ""

if ($NoWait) {
    Write-Host " Запуск в фоновом режиме..." -ForegroundColor Green
    Start-Process -FilePath "dotnet" -ArgumentList "run" -NoNewWindow -PassThru
    Write-Host " API сервер запущен в фоновом режиме" -ForegroundColor Green
} else {
    try {
        if ($Verbose) {
            dotnet run --verbosity detailed
        } else {
            dotnet run
        }
    } catch {
        Write-Host " Ошибка запуска API сервера: $_" -ForegroundColor Red
        exit 1
    }
}
