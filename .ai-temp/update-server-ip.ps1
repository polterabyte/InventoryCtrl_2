# Script to update server IP configuration
# Usage: .\update-server-ip.ps1 -NewIP "192.168.139.96"

param(
    [Parameter(Mandatory=$true)]
    [string]$NewIP
)

Write-Host "Updating server IP configuration to: $NewIP" -ForegroundColor Green

# Update environment files
$envFiles = @("env.example", "env.production", "env.staging")
foreach ($file in $envFiles) {
    if (Test-Path $file) {
        Write-Host "Updating $file..." -ForegroundColor Yellow
        (Get-Content $file) -replace "SERVER_IP=.*", "SERVER_IP=$NewIP" | Set-Content $file
    }
}

# Update nginx configuration
Write-Host "Updating nginx configuration..." -ForegroundColor Yellow
(Get-Content "nginx/nginx.conf") -replace "192\.168\.139\.96", $NewIP | Set-Content "nginx/nginx.conf"

# Update client configuration files
$clientConfigFiles = @(
    "src/Inventory.Web.Client/appsettings.Staging.json",
    "src/Inventory.Web.Client/appsettings.Production.json"
)

foreach ($file in $clientConfigFiles) {
    if (Test-Path $file) {
        Write-Host "Updating $file..." -ForegroundColor Yellow
        (Get-Content $file) -replace "192\.168\.139\.96", $NewIP | Set-Content $file
    }
}

Write-Host "Configuration updated successfully!" -ForegroundColor Green
Write-Host "Please restart your Docker containers to apply changes." -ForegroundColor Cyan
