# Restart Servers
Write-Host "Restarting servers..." -ForegroundColor Green

# Stop all dotnet processes
Write-Host "Stopping all dotnet processes..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

# Wait a moment
Start-Sleep -Seconds 2

# Check if ports are free
Write-Host "Checking port availability..." -ForegroundColor Yellow
$port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
$port7000 = Get-NetTCPConnection -LocalPort 7000 -ErrorAction SilentlyContinue
$port5001 = Get-NetTCPConnection -LocalPort 5001 -ErrorAction SilentlyContinue
$port7001 = Get-NetTCPConnection -LocalPort 7001 -ErrorAction SilentlyContinue

if ($port5000) {
    Write-Host "Port 5000 is still in use" -ForegroundColor Red
} else {
    Write-Host "Port 5000 is free" -ForegroundColor Green
}

if ($port7000) {
    Write-Host "Port 7000 is still in use" -ForegroundColor Red
} else {
    Write-Host "Port 7000 is free" -ForegroundColor Green
}

if ($port5001) {
    Write-Host "Port 5001 is still in use" -ForegroundColor Red
} else {
    Write-Host "Port 5001 is free" -ForegroundColor Green
}

if ($port7001) {
    Write-Host "Port 7001 is still in use" -ForegroundColor Red
} else {
    Write-Host "Port 7001 is free" -ForegroundColor Green
}

Write-Host ""
Write-Host "Starting API server..." -ForegroundColor Yellow
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.API" -WindowStyle Minimized

# Wait for API to start
Write-Host "Waiting for API server to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Check if API is running
$apiRunning = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($apiRunning) {
    Write-Host "✅ API server is running on port 5000" -ForegroundColor Green
} else {
    Write-Host "❌ API server failed to start" -ForegroundColor Red
}

Write-Host ""
Write-Host "Starting Web client..." -ForegroundColor Yellow
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.Web.Client" -WindowStyle Minimized

# Wait for Web to start
Write-Host "Waiting for Web client to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Check if Web is running
$webRunning = Get-NetTCPConnection -LocalPort 5001 -ErrorAction SilentlyContinue
if ($webRunning) {
    Write-Host "✅ Web client is running on port 5001" -ForegroundColor Green
} else {
    Write-Host "❌ Web client failed to start" -ForegroundColor Red
}

Write-Host ""
Write-Host "Server restart completed!" -ForegroundColor Green
Write-Host "API: http://localhost:5000 | https://localhost:7000" -ForegroundColor Cyan
Write-Host "Web: http://localhost:5001 | https://localhost:7001" -ForegroundColor Cyan
