# Diagnose SignalR Connection Issues
Write-Host "Diagnosing SignalR connection issues..." -ForegroundColor Green

# Check if API server is running on port 5000
Write-Host "Checking if API server is running on port 5000..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api" -Method GET -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ API server is running on port 5000" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor White
} catch {
    Write-Host "❌ API server is NOT running on port 5000" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Check if API server is running on port 7000 (HTTPS)
Write-Host "Checking if API server is running on port 7000 (HTTPS)..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://localhost:7000/api" -Method GET -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ API server is running on port 7000 (HTTPS)" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor White
} catch {
    Write-Host "❌ API server is NOT running on port 7000 (HTTPS)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Check SignalR hub endpoint
Write-Host "Checking SignalR hub endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/notificationHub" -Method GET -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ SignalR hub endpoint is accessible" -ForegroundColor Green
} catch {
    Write-Host "❌ SignalR hub endpoint is NOT accessible" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Check what processes are listening on ports
Write-Host "Checking what processes are listening on ports..." -ForegroundColor Yellow
$netstat = netstat -an | Select-String "5000|7000"
if ($netstat) {
    Write-Host "Ports in use:" -ForegroundColor Cyan
    $netstat | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
} else {
    Write-Host "No processes found listening on ports 5000 or 7000" -ForegroundColor Red
}

Write-Host ""
Write-Host "Solutions:" -ForegroundColor Cyan
Write-Host "1. Make sure API server is running: dotnet run --project src/Inventory.API" -ForegroundColor White
Write-Host "2. Check if ports are configured correctly in ports.json" -ForegroundColor White
Write-Host "3. Verify CORS settings allow SignalR connections" -ForegroundColor White
Write-Host "4. Check firewall settings" -ForegroundColor White
