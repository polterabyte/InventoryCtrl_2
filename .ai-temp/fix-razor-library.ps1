# Fix Razor Class Library configuration
Write-Host "Fixing Razor Class Library configuration..." -ForegroundColor Yellow

# Stop all dotnet processes
Write-Host "Stopping dotnet processes..." -ForegroundColor Blue
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Clean and build
Write-Host "Cleaning and building..." -ForegroundColor Blue
dotnet clean
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Starting applications..." -ForegroundColor Green
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\..'; .\start-apps.ps1"
    
    Write-Host "Changes made:" -ForegroundColor Cyan
    Write-Host "  - Changed Inventory.Shared to Razor Class Library" -ForegroundColor White
    Write-Host "  - Added Microsoft.AspNetCore.Components.Web package" -ForegroundColor White
    Write-Host "  - Restored _content/ paths in index.html" -ForegroundColor White
} else {
    Write-Host "Build failed! Check logs above." -ForegroundColor Red
}
