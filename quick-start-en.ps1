# Quick Start - Inventory Control
# Simple version without port checks

Write-Host "=== Quick Start - Inventory Control ===" -ForegroundColor Green
Write-Host ""

# Start API server in background
Write-Host "Starting API server..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.API/Inventory.API.csproj" -WindowStyle Normal

# Small pause
Start-Sleep -Seconds 3

# Start Web client
Write-Host "Starting Web client..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Inventory.Web/Inventory.Web.csproj" -WindowStyle Normal

Write-Host ""
Write-Host "Applications started!" -ForegroundColor Green
Write-Host "API:  https://localhost:7000" -ForegroundColor White
Write-Host "Web:  https://localhost:5001" -ForegroundColor White
Write-Host ""
Write-Host "Close application windows to stop" -ForegroundColor Yellow
