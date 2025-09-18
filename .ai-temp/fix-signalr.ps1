# Fix SignalR Connection Issues
Write-Host "Fixing SignalR connection issues..." -ForegroundColor Green

# Backup original files
Write-Host "Creating backups..." -ForegroundColor Yellow
Copy-Item "src\Inventory.UI\wwwroot\js\signalr-notifications.js" "src\Inventory.UI\wwwroot\js\signalr-notifications.js.backup" -Force
Copy-Item "src\Inventory.Web.Client\wwwroot\js\signalr-notifications.js" "src\Inventory.Web.Client\wwwroot\js\signalr-notifications.js.backup" -Force

# Replace with fixed version
Write-Host "Applying fixes..." -ForegroundColor Yellow
Copy-Item ".ai-temp\signalr-notifications-fixed.js" "src\Inventory.UI\wwwroot\js\signalr-notifications.js" -Force
Copy-Item ".ai-temp\signalr-notifications-fixed.js" "src\Inventory.Web.Client\wwwroot\js\signalr-notifications.js" -Force

# Also add SignalR script tag to HTML files if not present
Write-Host "Updating HTML files..." -ForegroundColor Yellow

# Check and update Inventory.UI index.html
$uiHtmlPath = "src\Inventory.UI\wwwroot\index.html"
$uiHtmlContent = Get-Content $uiHtmlPath -Raw

if ($uiHtmlContent -notmatch '<script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>') {
    $uiHtmlContent = $uiHtmlContent -replace '(<script src="_framework/blazor.webassembly.js"></script>)', '$1
    <script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>'
    Set-Content $uiHtmlPath $uiHtmlContent
    Write-Host "Added SignalR script to Inventory.UI index.html" -ForegroundColor Green
}

# Check and update Inventory.Web.Client index.html
$clientHtmlPath = "src\Inventory.Web.Client\wwwroot\index.html"
$clientHtmlContent = Get-Content $clientHtmlPath -Raw

if ($clientHtmlContent -notmatch '<script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>') {
    $clientHtmlContent = $clientHtmlContent -replace '(<script src="_framework/blazor.webassembly.js"></script>)', '$1
    <script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>'
    Set-Content $clientHtmlPath $clientHtmlContent
    Write-Host "Added SignalR script to Inventory.Web.Client index.html" -ForegroundColor Green
}

Write-Host "SignalR fixes applied successfully!" -ForegroundColor Green
Write-Host "Backup files created with .backup extension" -ForegroundColor Cyan
Write-Host "Please rebuild and test your application" -ForegroundColor Cyan
