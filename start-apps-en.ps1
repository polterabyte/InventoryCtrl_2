# Inventory Control Application Launcher
# Starts API server, then Web client

# Set UTF-8 encoding for proper text display
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "=== Inventory Control Application Launcher ===" -ForegroundColor Green
Write-Host ""

# Check if we're in the project root directory
if (-not (Test-Path "InventoryCtrl_2.sln")) {
    Write-Host "Error: Run script from project root directory (where InventoryCtrl_2.sln is located)" -ForegroundColor Red
    exit 1
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
    # Step 1: Start API server
    Write-Host "1. Starting API server..." -ForegroundColor Cyan
    Write-Host "   Ports: https://localhost:7000, http://localhost:5000" -ForegroundColor Gray
    
    $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.API/Inventory.API.csproj" -PassThru -WindowStyle Normal
    
    # Wait for API server to start (check HTTP port 5000)
    if (-not (Wait-ForServer -Port 5000 -ServiceName "API Server")) {
        Write-Host "Failed to start API server" -ForegroundColor Red
        $apiProcess.Kill()
        exit 1
    }
    
    Write-Host ""
    
    # Step 2: Start Web client
    Write-Host "2. Starting Web client..." -ForegroundColor Cyan
    Write-Host "   Ports: https://localhost:5001, http://localhost:5142" -ForegroundColor Gray
    
    $webProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.Web/Inventory.Web.csproj" -PassThru -WindowStyle Normal
    
    # Wait for Web client to start (check HTTP port 5142)
    if (-not (Wait-ForServer -Port 5142 -ServiceName "Web Client")) {
        Write-Host "Failed to start Web client" -ForegroundColor Red
        $webProcess.Kill()
        $apiProcess.Kill()
        exit 1
    }
    
    Write-Host ""
    Write-Host "=== Applications successfully started! ===" -ForegroundColor Green
    Write-Host "API Server:  https://localhost:7000" -ForegroundColor White
    Write-Host "Web Client:  https://localhost:5001" -ForegroundColor White
    Write-Host ""
    Write-Host "Press Ctrl+C to stop all applications" -ForegroundColor Yellow
    
    # Wait for completion
    try {
        Wait-Process -Id $apiProcess.Id, $webProcess.Id
    }
    catch {
        Write-Host "Applications stopped" -ForegroundColor Yellow
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
