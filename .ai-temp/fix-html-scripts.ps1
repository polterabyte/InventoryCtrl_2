# Fix HTML Scripts
Write-Host "Fixing HTML scripts..." -ForegroundColor Green

# Update Inventory.UI index.html
Write-Host "Updating Inventory.UI index.html..." -ForegroundColor Yellow
$uiHtmlPath = "src\Inventory.UI\wwwroot\index.html"
$uiHtmlContent = Get-Content $uiHtmlPath -Raw

# Remove the inline script that conflicts with api-config.js
$uiHtmlContent = $uiHtmlContent -replace '<script>\s*// Global functions for SignalR integration.*?</script>', ''

Set-Content $uiHtmlPath $uiHtmlContent
Write-Host "Updated Inventory.UI index.html" -ForegroundColor Green

# Update Inventory.Web.Client index.html
Write-Host "Updating Inventory.Web.Client index.html..." -ForegroundColor Yellow
$clientHtmlPath = "src\Inventory.Web.Client\wwwroot\index.html"
$clientHtmlContent = Get-Content $clientHtmlPath -Raw

# Remove the inline script that conflicts with api-config.js
$clientHtmlContent = $clientHtmlContent -replace '<script>\s*// Global functions for SignalR integration.*?</script>', ''

Set-Content $clientHtmlPath $clientHtmlContent
Write-Host "Updated Inventory.Web.Client index.html" -ForegroundColor Green

# Verify the script loading order
Write-Host "Checking script loading order..." -ForegroundColor Yellow
$uiHtml = Get-Content $uiHtmlPath -Raw
if ($uiHtml -match 'js/api-config\.js') {
    Write-Host "✅ api-config.js is loaded" -ForegroundColor Green
} else {
    Write-Host "❌ api-config.js is NOT loaded" -ForegroundColor Red
}

if ($uiHtml -match 'js/signalr-notifications\.js') {
    Write-Host "✅ signalr-notifications.js is loaded" -ForegroundColor Green
} else {
    Write-Host "❌ signalr-notifications.js is NOT loaded" -ForegroundColor Red
}

Write-Host "Fixed HTML scripts!" -ForegroundColor Green
Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Removed conflicting inline scripts from HTML files" -ForegroundColor White
Write-Host "2. Now only api-config.js and signalr-notifications.js are used" -ForegroundColor White
Write-Host "3. This should fix the 'Cannot read properties of undefined' error" -ForegroundColor White
Write-Host ""
Write-Host "Script loading order:" -ForegroundColor Yellow
Write-Host "1. blazor.webassembly.js" -ForegroundColor White
Write-Host "2. signalr.min.js" -ForegroundColor White
Write-Host "3. api-config.js (defines getApiBaseUrl and initializeSignalRConnection)" -ForegroundColor White
Write-Host "4. signalr-notifications.js (defines signalRNotificationService)" -ForegroundColor White
Write-Host ""
Write-Host "This should fix the TypeError!" -ForegroundColor Green
