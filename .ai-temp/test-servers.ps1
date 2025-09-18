# Test Servers
Write-Host "Testing server status..." -ForegroundColor Green

# Check API server
Write-Host "Checking API server..." -ForegroundColor Yellow
try {
    $apiResponse = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ API server is responding (Status: $($apiResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ API server is not responding: $($_.Exception.Message)" -ForegroundColor Red
}

# Check Web client
Write-Host "Checking Web client..." -ForegroundColor Yellow
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:5001" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ Web client is responding (Status: $($webResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ Web client is not responding: $($_.Exception.Message)" -ForegroundColor Red
}

# Check SignalR hub
Write-Host "Checking SignalR hub..." -ForegroundColor Yellow
try {
    $hubResponse = Invoke-WebRequest -Uri "http://localhost:5000/notificationHub" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ SignalR hub is accessible (Status: $($hubResponse.StatusCode))" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ SignalR hub is accessible but requires authentication (401 Unauthorized - expected)" -ForegroundColor Green
    } else {
        Write-Host "❌ SignalR hub error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Check processes
Write-Host "Checking running processes..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
Write-Host "Running dotnet processes: $($dotnetProcesses.Count)" -ForegroundColor Cyan

Write-Host ""
Write-Host "Server Status Summary:" -ForegroundColor Cyan
Write-Host "API Server: http://localhost:5000 | https://localhost:7000" -ForegroundColor White
Write-Host "Web Client: http://localhost:5001 | https://localhost:7001" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Open browser to http://localhost:5001" -ForegroundColor White
Write-Host "2. Login as superadmin" -ForegroundColor White
Write-Host "3. Check notification status" -ForegroundColor White
Write-Host "4. Check browser console for errors" -ForegroundColor White
