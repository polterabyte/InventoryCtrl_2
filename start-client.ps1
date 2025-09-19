# Скрипт запуска Web Client приложения
# Inventory Control System v2 - Web Client

param(
    [switch]$Verbose,
    [switch]$NoWait
)

$ErrorActionPreference = "Stop"

Write-Host " Запуск Web Client приложения Inventory Control System v2" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green

# Проверка .NET SDK
Write-Host " Проверка .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host " .NET SDK версия: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host " .NET SDK не найден. Установите .NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

# Переход в директорию Client
$clientPath = "src/Inventory.Web.Client"
if (-not (Test-Path $clientPath)) {
    Write-Host " Директория Client не найдена: $clientPath" -ForegroundColor Red
    exit 1
}

Set-Location $clientPath
Write-Host " Рабочая директория: $(Get-Location)" -ForegroundColor Cyan

# Проверка файлов проекта
if (-not (Test-Path "Inventory.Web.Client.csproj")) {
    Write-Host " Файл проекта не найден: Inventory.Web.Client.csproj" -ForegroundColor Red
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

# Запуск Web Client приложения
Write-Host " Запуск Web Client приложения..." -ForegroundColor Yellow
Write-Host " URL: https://localhost:7001" -ForegroundColor Cyan
Write-Host " Для остановки нажмите Ctrl+C" -ForegroundColor Red
Write-Host ""

if ($NoWait) {
    Write-Host " Запуск в фоновом режиме..." -ForegroundColor Green
    Start-Process -FilePath "dotnet" -ArgumentList "run" -NoNewWindow -PassThru
    Write-Host " Web Client запущен в фоновом режиме" -ForegroundColor Green
} else {
    try {
        if ($Verbose) {
            dotnet run --verbosity detailed
        } else {
            dotnet run
        }
    } catch {
        Write-Host " Ошибка запуска Web Client приложения: $_" -ForegroundColor Red
        exit 1
    }
}