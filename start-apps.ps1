# Скрипт последовательного запуска API и Client приложений
# Inventory Control System v2 - Complete Application Suite

param(
    [switch]$Verbose,
    [switch]$OpenBrowser = $true,
    [switch]$SkipAPI,
    [switch]$SkipClient,
    [int]$APIPort = 5000,
    [int]$ClientPort = 5001
)

$ErrorActionPreference = "Stop"

# Глобальные переменные для процессов
$global:APIProcess = $null
$global:ClientProcess = $null

Write-Host " Запуск полного комплекса Inventory Control System v2" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host " API Server: https://localhost:$($APIPort + 1) | http://localhost:$APIPort" -ForegroundColor Cyan
Write-Host " Web Client: https://localhost:$($ClientPort + 1) | http://localhost:$ClientPort" -ForegroundColor Cyan
Write-Host ""

# Функция очистки процессов при завершении
function Cleanup-Processes {
    Write-Host " Остановка всех процессов..." -ForegroundColor Yellow
    
    if ($global:ClientProcess -and !$global:ClientProcess.HasExited) {
        Write-Host "   Остановка Client процесса (PID: $($global:ClientProcess.Id))" -ForegroundColor Yellow
        Stop-Process -Id $global:ClientProcess.Id -Force -ErrorAction SilentlyContinue
    }
    
    if ($global:APIProcess -and !$global:APIProcess.HasExited) {
        Write-Host "   Остановка API процесса (PID: $($global:APIProcess.Id))" -ForegroundColor Yellow
        Stop-Process -Id $global:APIProcess.Id -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host " Все процессы остановлены" -ForegroundColor Green
}

# Обработчик Ctrl+C
$null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Cleanup-Processes }

# Функция проверки готовности API
function Test-APIReady {
    param([int]$Port, [int]$TimeoutSeconds = 30)
    
    $endTime = (Get-Date).AddSeconds($TimeoutSeconds)
    $apiUrl = "http://localhost:$Port"
    
    Write-Host " Ожидание готовности API на порту $Port..." -ForegroundColor Yellow
    
    while ((Get-Date) -lt $endTime) {
        try {
            $response = Invoke-WebRequest -Uri $apiUrl -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host " API готов к работе" -ForegroundColor Green
                return $true
            }
        } catch {
            # Игнорируем ошибки и продолжаем проверку
        }
        
        Start-Sleep -Seconds 1
        Write-Host "." -NoNewline -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host " API не отвечает в течение $TimeoutSeconds секунд" -ForegroundColor Red
    return $false
}

# Функция запуска API
function Start-APIServer {
    Write-Host " Запуск API сервера..." -ForegroundColor Yellow
    
    try {
        $apiArgs = @("run")
        if ($Verbose) { $apiArgs += "--verbosity", "detailed" }
        
        $global:APIProcess = Start-Process -FilePath "dotnet" -ArgumentList $apiArgs -WorkingDirectory "src/Inventory.API" -NoNewWindow -PassThru
        
        Write-Host " API сервер запущен (PID: $($global:APIProcess.Id))" -ForegroundColor Green
        
        # Ожидание готовности API
        if (Test-APIReady -Port $APIPort) {
            return $true
        } else {
            throw "API не готов к работе"
        }
    } catch {
        Write-Host " Ошибка запуска API: $_" -ForegroundColor Red
        return $false
    }
}

# Функция запуска Client
function Start-ClientApp {
    Write-Host " Запуск Web Client приложения..." -ForegroundColor Yellow
    
    try {
        $clientArgs = @("run")
        if ($Verbose) { $clientArgs += "--verbosity", "detailed" }
        
        $global:ClientProcess = Start-Process -FilePath "dotnet" -ArgumentList $clientArgs -WorkingDirectory "src/Inventory.Web.Client" -NoNewWindow -PassThru
        
        Write-Host " Web Client запущен (PID: $($global:ClientProcess.Id))" -ForegroundColor Green
        
        # Ожидание запуска Client
        Start-Sleep -Seconds 3
        
        # Открытие браузера
        if ($OpenBrowser) {
            Write-Host " Открытие браузера..." -ForegroundColor Green
            try {
                Start-Process "https://localhost:$($ClientPort + 1)"
                Write-Host " Браузер открыт" -ForegroundColor Green
            } catch {
                Write-Host " Не удалось открыть браузер автоматически" -ForegroundColor Yellow
                Write-Host "   Откройте вручную: https://localhost:$($ClientPort + 1)" -ForegroundColor Cyan
            }
        }
        
        return $true
    } catch {
        Write-Host " Ошибка запуска Client: $_" -ForegroundColor Red
        return $false
    }
}

# Основная логика
try {
    # Проверка .NET SDK
    Write-Host " Проверка .NET SDK..." -ForegroundColor Yellow
    try {
        $dotnetVersion = dotnet --version
        Write-Host " .NET SDK версия: $dotnetVersion" -ForegroundColor Green
    } catch {
        Write-Host " .NET SDK не найден. Установите .NET 9.0 SDK" -ForegroundColor Red
        exit 1
    }
    
    # Проверка директорий проектов
    if (-not (Test-Path "src/Inventory.API")) {
        Write-Host " Директория API не найдена: src/Inventory.API" -ForegroundColor Red
        exit 1
    }
    
    if (-not (Test-Path "src/Inventory.Web.Client")) {
        Write-Host " Директория Client не найдена: src/Inventory.Web.Client" -ForegroundColor Red
        exit 1
    }
    
    Write-Host " Все директории найдены" -ForegroundColor Green
    Write-Host ""
    
    # Запуск API (если не пропущен)
    if (-not $SkipAPI) {
        if (-not (Start-APIServer)) {
            Write-Host " Не удалось запустить API сервер" -ForegroundColor Red
            Cleanup-Processes
            exit 1
        }
        Write-Host ""
    } else {
        Write-Host " Пропуск запуска API сервера" -ForegroundColor Yellow
        Write-Host ""
    }
    
    # Запуск Client (если не пропущен)
    if (-not $SkipClient) {
        if (-not (Start-ClientApp)) {
            Write-Host " Не удалось запустить Client приложение" -ForegroundColor Red
            Cleanup-Processes
            exit 1
        }
        Write-Host ""
    } else {
        Write-Host " Пропуск запуска Client приложения" -ForegroundColor Yellow
        Write-Host ""
    }
    
    # Информация о запущенных процессах
    Write-Host " Все приложения запущены успешно!" -ForegroundColor Green
    Write-Host "=================================" -ForegroundColor Green
    if ($global:APIProcess) {
        Write-Host " API Server (PID: $($global:APIProcess.Id))" -ForegroundColor Cyan
    }
    if ($global:ClientProcess) {
        Write-Host " Web Client (PID: $($global:ClientProcess.Id))" -ForegroundColor Cyan
    }
    Write-Host ""
    Write-Host " Для остановки всех процессов нажмите Ctrl+C" -ForegroundColor Red
    Write-Host " Web Client: https://localhost:$($ClientPort + 1)" -ForegroundColor Cyan
    Write-Host " API Server: https://localhost:$($APIPort + 1)" -ForegroundColor Cyan
    Write-Host ""
    
    # Ожидание завершения
    try {
        if ($global:APIProcess) {
            $global:APIProcess.WaitForExit()
        }
        if ($global:ClientProcess) {
            $global:ClientProcess.WaitForExit()
        }
    } catch {
        # Обработка прерывания
    }
    
} catch {
    Write-Host " Критическая ошибка: $_" -ForegroundColor Red
    Cleanup-Processes
    exit 1
} finally {
    Cleanup-Processes
}
