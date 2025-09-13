# Inventory Control Application Launcher
# Starts API server, then Web client
# Supports both full launch with checks and quick launch without checks

# Set UTF-8 encoding for proper text display
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# Parse command line parameters
param(
    [switch]$Quick,
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\start-apps.ps1 [-Quick] [-Help]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -Quick    Quick launch without port checks" -ForegroundColor Gray
    Write-Host "  -Help     Show this help message" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\start-apps.ps1           # Full launch with port checks" -ForegroundColor Gray
    Write-Host "  .\start-apps.ps1 -Quick    # Quick launch without checks" -ForegroundColor Gray
    exit 0
}

if ($Quick) {
    Write-Host "=== Быстрый запуск Inventory Control ===" -ForegroundColor Green
} else {
    Write-Host "=== Inventory Control Application Launcher ===" -ForegroundColor Green
}
Write-Host ""

# Check if we're in the project root directory
if (-not (Test-Path "InventoryCtrl_2.sln")) {
    Write-Host "Error: Run script from project root directory (where InventoryCtrl_2.sln is located)" -ForegroundColor Red
    exit 1
}

# Load port configuration
$portConfig = Get-Content "ports.json" -Raw | ConvertFrom-Json
$apiPorts = @{
    Http = $portConfig.api.http
    Https = $portConfig.api.https
    Urls = $portConfig.api.urls
}
$webPorts = @{
    Http = $portConfig.web.http
    Https = $portConfig.web.https
    Urls = $portConfig.web.urls
}

# Function to check port availability
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

# Function to wait for server startup
function Wait-ForServer {
    param([int]$Port, [string]$ServiceName)
    Write-Host "Waiting for $ServiceName to start on port $Port..." -ForegroundColor Yellow
    $timeout = 30
    $elapsed = 0
    
    while ($elapsed -lt $timeout) {
        if (Test-Port $Port) {
            Write-Host "$ServiceName successfully started on port $Port" -ForegroundColor Green
            return $true
        }
        Start-Sleep -Seconds 2
        $elapsed += 2
        Write-Host "." -NoNewline -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Timeout waiting for $ServiceName to start" -ForegroundColor Red
    return $false
}

try {
    if ($Quick) {
        # Quick launch without port checks
        Write-Host "Запуск API сервера..." -ForegroundColor Cyan
        $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.API/Inventory.API.csproj" -PassThru -WindowStyle Normal
        
        # Small pause
        Start-Sleep -Seconds 3
        
        Write-Host "Запуск Web клиента..." -ForegroundColor Cyan
        $webProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.Web.Client/Inventory.Web.Client.csproj" -PassThru -WindowStyle Normal
        
        Write-Host ""
        Write-Host "Приложения запущены!" -ForegroundColor Green
        Write-Host "API:  https://localhost:$($apiPorts.Https)" -ForegroundColor White
        Write-Host "Web:  https://localhost:$($webPorts.Https)" -ForegroundColor White
        Write-Host ""
        Write-Host "Закройте окна приложений для остановки" -ForegroundColor Yellow
        Start-Process "http://localhost:$($webPorts.Http)"
    } else {
        # Full launch with port checks
        Write-Host "1. Starting API server..." -ForegroundColor Cyan
        Write-Host "   Ports: https://localhost:$($apiPorts.Https), http://localhost:$($apiPorts.Http)" -ForegroundColor Gray
        
        $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.API/Inventory.API.csproj" -PassThru -WindowStyle Normal
        
        # Wait for API server to start (check HTTP port)
        if (-not (Wait-ForServer -Port $apiPorts.Http -ServiceName "API Server")) {
            Write-Host "Failed to start API server" -ForegroundColor Red
            $apiProcess.Kill()
            exit 1
        }
        
        Write-Host ""
        
        # Step 2: Start Web client
        Write-Host "2. Starting Web client..." -ForegroundColor Cyan
        Write-Host "   Ports: https://localhost:$($webPorts.Https), http://localhost:$($webPorts.Http)" -ForegroundColor Gray
        
        $webProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.Web.Client/Inventory.Web.Client.csproj" -PassThru -WindowStyle Normal
        
        # Wait for Web client to start (check HTTP port)
        if (-not (Wait-ForServer -Port $webPorts.Http -ServiceName "Web Client")) {
            Write-Host "Failed to start Web client" -ForegroundColor Red
            $webProcess.Kill()
            $apiProcess.Kill()
            exit 1
        }
        
        Write-Host ""
        Write-Host "=== Applications successfully started! ===" -ForegroundColor Green
        Write-Host "API Server:  https://localhost:$($apiPorts.Https)" -ForegroundColor White
        Write-Host "Web Client:  https://localhost:$($webPorts.Https)" -ForegroundColor White
        Write-Host ""
        Write-Host "Press Ctrl+C to stop all applications" -ForegroundColor Yellow
        Start-Process "http://localhost:$($webPorts.Http)"
        
        # Wait for completion
        try {
            Wait-Process -Id $apiProcess.Id, $webProcess.Id
        }
        catch {
            Write-Host "Applications stopped" -ForegroundColor Yellow
        }
    }
}
catch {
    Write-Host "Error starting applications: $($_.Exception.Message)" -ForegroundColor Red
    
    # Stop processes in case of error
    if ($apiProcess -and !$apiProcess.HasExited) {
        $apiProcess.Kill()
    }
    if ($webProcess -and !$webProcess.HasExited) {
        $webProcess.Kill()
    }
    
    exit 1
}
finally {
    # Cleanup processes
    if ($apiProcess -and !$apiProcess.HasExited) {
        Write-Host "Stopping API server..." -ForegroundColor Yellow
        $apiProcess.Kill()
    }
    if ($webProcess -and !$webProcess.HasExited) {
        Write-Host "Stopping Web client..." -ForegroundColor Yellow
        $webProcess.Kill()
    }
    
    Write-Host "All applications stopped" -ForegroundColor Green
}
