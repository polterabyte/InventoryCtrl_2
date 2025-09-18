# Quick Start Script for Inventory Control Applications
# Simple script for fast application startup

param(
    [switch]$NoBrowser
)

Write-Host "🚀 Quick Starting Inventory Control Applications..." -ForegroundColor Green

# Load configuration
$config = Get-Content "ports.json" | ConvertFrom-Json

# Stop existing processes
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Start API
Write-Host "Starting API server..." -ForegroundColor Yellow
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.API" -WindowStyle Minimized

# Wait for API
Write-Host "Waiting for API to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# Start Web Client
Write-Host "Starting Web client..." -ForegroundColor Yellow
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.Web.Client" -WindowStyle Minimized

# Wait for Web Client
Write-Host "Waiting for Web client to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# Open browser
if (-not $NoBrowser) {
    $webUrl = $config.launchUrls.web
    Write-Host "Opening browser to $webUrl" -ForegroundColor Green
    Start-Process $webUrl
}

Write-Host "✅ Applications started!" -ForegroundColor Green
Write-Host "API: $($config.launchUrls.api)" -ForegroundColor Cyan
Write-Host "Web: $($config.launchUrls.web)" -ForegroundColor Cyan
