# Restart applications with SignalR fix
Write-Host "Restarting applications with SignalR fix..." -ForegroundColor Green

# Stop any running dotnet processes
Write-Host "Stopping existing applications..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { 
    $_.MainWindowTitle -like "*Inventory*" -or 
    $_.CommandLine -like "*Inventory*" 
} | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep -Seconds 3

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build src/Inventory.Web.Client/Inventory.Web.Client.csproj
dotnet build src/Inventory.API/Inventory.API.csproj

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Start applications
Write-Host "Starting applications..." -ForegroundColor Yellow
& .\start-apps.ps1
