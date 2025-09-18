# Restart applications with notification fix
Write-Host "Restarting applications with notification fix..." -ForegroundColor Green

# Stop any running processes
Get-Process -Name "Inventory.API", "Inventory.UI" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Build the project to ensure changes are compiled
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful. Starting applications..." -ForegroundColor Green

# Start applications
& ".\start-apps.ps1"

Write-Host "Applications restarted with notification fix." -ForegroundColor Green
Write-Host "Please test the notification banner behavior:" -ForegroundColor Cyan
Write-Host "1. Open https://localhost:7001 in browser" -ForegroundColor White
Write-Host "2. If you're already logged in, logout first" -ForegroundColor White
Write-Host "3. Check if the banner appears and disappears correctly" -ForegroundColor White
Write-Host "4. Login and verify banner doesn't appear" -ForegroundColor White
