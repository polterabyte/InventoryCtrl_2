# Fix static files issue
Write-Host "Fixing static files configuration..." -ForegroundColor Yellow

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
} else {
    Write-Host "Build failed! Check logs above." -ForegroundColor Red
}
