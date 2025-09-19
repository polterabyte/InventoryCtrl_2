# Скрипт для проверки доступности портов извне
param(
    [Parameter(Mandatory=$true)]
    [string]$ServerIP,
    
    [Parameter(Mandatory=$false)]
    [int[]]$Ports = @(80, 5000)
)

Write-Host "🌐 Проверка доступности портов на сервере $ServerIP" -ForegroundColor Cyan
Write-Host ""

# Функция для проверки порта через Test-NetConnection
function Test-PortWithNetConnection {
    param([string]$ComputerName, [int]$Port)
    
    try {
        $result = Test-NetConnection -ComputerName $ComputerName -Port $Port -InformationLevel Quiet
        return $result
    } catch {
        return $false
    }
}

# Функция для проверки порта через telnet
function Test-PortWithTelnet {
    param([string]$Hostname, [int]$Port)
    
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $connect = $tcpClient.BeginConnect($Hostname, $Port, $null, $null)
        $wait = $connect.AsyncWaitHandle.WaitOne(3000, $false)
        
        if ($wait) {
            $tcpClient.EndConnect($connect)
            $tcpClient.Close()
            return $true
        } else {
            $tcpClient.Close()
            return $false
        }
    } catch {
        return $false
    }
}

# Функция для получения внешнего IP
function Get-ExternalIP {
    try {
        $response = Invoke-WebRequest -Uri "https://api.ipify.org" -TimeoutSec 10
        return $response.Content.Trim()
    } catch {
        return "Не удалось получить"
    }
}

Write-Host "📍 Информация о сервере:" -ForegroundColor Yellow
$externalIP = Get-ExternalIP
Write-Host "   Внешний IP: $externalIP" -ForegroundColor White
Write-Host "   Локальный IP: $ServerIP" -ForegroundColor White
Write-Host ""

# Проверяем каждый порт
foreach ($port in $Ports) {
    Write-Host "🔍 Проверка порта $port..." -ForegroundColor Yellow
    
    # Метод 1: Test-NetConnection
    $netConnectionResult = Test-PortWithNetConnection -ComputerName $ServerIP -Port $port
    Write-Host "   Test-NetConnection: $(if ($netConnectionResult) { '✅ Открыт' } else { '❌ Закрыт' })" -ForegroundColor $(if ($netConnectionResult) { 'Green' } else { 'Red' })
    
    # Метод 2: TcpClient
    $tcpClientResult = Test-PortWithTelnet -Hostname $ServerIP -Port $port
    Write-Host "   TcpClient: $(if ($tcpClientResult) { '✅ Открыт' } else { '❌ Закрыт' })" -ForegroundColor $(if ($tcpClientResult) { 'Green' } else { 'Red' })
    
    # Метод 3: Проверка через HTTP (для веб-портов)
    if ($port -eq 80 -or $port -eq 443) {
        try {
            $protocol = if ($port -eq 443) { "https" } else { "http" }
            $url = "$protocol`://$ServerIP`:$port"
            $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 5 -ErrorAction Stop
            Write-Host "   HTTP Response: ✅ $($response.StatusCode)" -ForegroundColor Green
        } catch {
            Write-Host "   HTTP Response: ❌ $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

# Дополнительные проверки
Write-Host "🔧 Дополнительные проверки:" -ForegroundColor Yellow

# Проверяем, запущены ли контейнеры
Write-Host "📦 Статус Docker контейнеров:" -ForegroundColor White
try {
    $containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>$null
    if ($containers) {
        Write-Host $containers -ForegroundColor Gray
    } else {
        Write-Host "   Docker не доступен или контейнеры не запущены" -ForegroundColor Red
    }
} catch {
    Write-Host "   Ошибка проверки Docker: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Проверяем процессы, использующие порты
Write-Host "🔍 Процессы, использующие порты:" -ForegroundColor White
foreach ($port in $Ports) {
    $processes = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($proc in $processes) {
            $processName = (Get-Process -Id $proc.OwningProcess -ErrorAction SilentlyContinue).ProcessName
            Write-Host "   Порт $port`: $processName (PID: $($proc.OwningProcess))" -ForegroundColor Gray
        }
    } else {
        Write-Host "   Порт $port`: Не используется" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "📝 Рекомендации:" -ForegroundColor Cyan

$allPortsOpen = $true
foreach ($port in $Ports) {
    $netConnectionResult = Test-PortWithNetConnection -ComputerName $ServerIP -Port $port
    if (-not $netConnectionResult) {
        $allPortsOpen = $false
    }
}

if ($allPortsOpen) {
    Write-Host "   ✅ Все порты открыты! Приложение должно быть доступно с внешних компьютеров." -ForegroundColor Green
} else {
    Write-Host "   ❌ Некоторые порты закрыты. Выполните следующие действия:" -ForegroundColor Red
    Write-Host "      1. Запустите: .\.ai-temp\check-firewall.ps1 -AddRules" -ForegroundColor White
    Write-Host "      2. Убедитесь, что Docker контейнеры запущены" -ForegroundColor White
    Write-Host "      3. Проверьте настройки роутера/маршрутизатора" -ForegroundColor White
    Write-Host "      4. Убедитесь, что приложение прослушивает на 0.0.0.0, а не на localhost" -ForegroundColor White
}
