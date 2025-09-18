# Download SignalR library locally
Write-Host "Downloading SignalR library locally..." -ForegroundColor Green

# Create lib/signalr directory
$signalrDir = "src\Inventory.UI\wwwroot\lib\signalr"
$signalrDirClient = "src\Inventory.Web.Client\wwwroot\lib\signalr"

if (!(Test-Path $signalrDir)) {
    New-Item -ItemType Directory -Path $signalrDir -Force
    Write-Host "Created directory: $signalrDir" -ForegroundColor Yellow
}

if (!(Test-Path $signalrDirClient)) {
    New-Item -ItemType Directory -Path $signalrDirClient -Force
    Write-Host "Created directory: $signalrDirClient" -ForegroundColor Yellow
}

# Download SignalR library
$signalrUrl = "https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"
$signalrPath = "$signalrDir\signalr.min.js"
$signalrPathClient = "$signalrDirClient\signalr.min.js"

try {
    Write-Host "Downloading SignalR library..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $signalrUrl -OutFile $signalrPath
    Copy-Item $signalrPath $signalrPathClient -Force
    Write-Host "SignalR library downloaded successfully!" -ForegroundColor Green
} catch {
    Write-Host "Failed to download SignalR library: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Update HTML files to use local SignalR
Write-Host "Updating HTML files to use local SignalR..." -ForegroundColor Yellow

# Update Inventory.UI index.html
$uiHtmlPath = "src\Inventory.UI\wwwroot\index.html"
$uiHtmlContent = Get-Content $uiHtmlPath -Raw

# Replace CDN with local
$uiHtmlContent = $uiHtmlContent -replace '<script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>', '<script src="lib/signalr/signalr.min.js"></script>'
Set-Content $uiHtmlPath $uiHtmlContent
Write-Host "Updated Inventory.UI index.html to use local SignalR" -ForegroundColor Green

# Update Inventory.Web.Client index.html
$clientHtmlPath = "src\Inventory.Web.Client\wwwroot\index.html"
$clientHtmlContent = Get-Content $clientHtmlPath -Raw

# Replace CDN with local
$clientHtmlContent = $clientHtmlContent -replace '<script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>', '<script src="lib/signalr/signalr.min.js"></script>'
Set-Content $clientHtmlPath $clientHtmlContent
Write-Host "Updated Inventory.Web.Client index.html to use local SignalR" -ForegroundColor Green

# Replace signalr-notifications.js with local version
Write-Host "Replacing signalr-notifications.js with local version..." -ForegroundColor Yellow
Copy-Item ".ai-temp\signalr-notifications-local.js" "src\Inventory.UI\wwwroot\js\signalr-notifications.js" -Force
Copy-Item ".ai-temp\signalr-notifications-local.js" "src\Inventory.Web.Client\wwwroot\js\signalr-notifications.js" -Force

Write-Host "Local SignalR setup completed!" -ForegroundColor Green
Write-Host "SignalR library is now served locally instead of from CDN" -ForegroundColor Cyan
