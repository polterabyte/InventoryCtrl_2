# Update HTML files with proper script loading order
Write-Host "Updating HTML files with proper script loading order..." -ForegroundColor Green

# Copy api-config.js to both projects
Write-Host "Copying api-config.js to projects..." -ForegroundColor Yellow
Copy-Item ".ai-temp\api-config.js" "src\Inventory.UI\wwwroot\js\api-config.js" -Force
Copy-Item ".ai-temp\api-config.js" "src\Inventory.Web.Client\wwwroot\js\api-config.js" -Force

# Update Inventory.UI index.html
Write-Host "Updating Inventory.UI index.html..." -ForegroundColor Yellow
$uiHtmlPath = "src\Inventory.UI\wwwroot\index.html"
$uiHtmlContent = Get-Content $uiHtmlPath -Raw

# Remove the inline script and replace with api-config.js
$uiHtmlContent = $uiHtmlContent -replace '<script>\s*// Global functions for SignalR integration.*?</script>', '<script src="js/api-config.js"></script>'

Set-Content $uiHtmlPath $uiHtmlContent
Write-Host "Updated Inventory.UI index.html" -ForegroundColor Green

# Update Inventory.Web.Client index.html
Write-Host "Updating Inventory.Web.Client index.html..." -ForegroundColor Yellow
$clientHtmlPath = "src\Inventory.Web.Client\wwwroot\index.html"
$clientHtmlContent = Get-Content $clientHtmlPath -Raw

# Remove the inline script and replace with api-config.js
$clientHtmlContent = $clientHtmlContent -replace '<script>\s*// Global functions for SignalR integration.*?</script>', '<script src="js/api-config.js"></script>'

Set-Content $clientHtmlPath $clientHtmlContent
Write-Host "Updated Inventory.Web.Client index.html" -ForegroundColor Green

Write-Host "HTML files updated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Created separate api-config.js file" -ForegroundColor White
Write-Host "2. Moved all SignalR functions to api-config.js" -ForegroundColor White
Write-Host "3. Updated HTML files to load api-config.js" -ForegroundColor White
Write-Host "4. Ensured proper loading order" -ForegroundColor White
Write-Host ""
Write-Host "Script loading order:" -ForegroundColor Yellow
Write-Host "1. blazor.webassembly.js" -ForegroundColor White
Write-Host "2. signalr.min.js" -ForegroundColor White
Write-Host "3. api-config.js (defines getApiBaseUrl)" -ForegroundColor White
Write-Host "4. signalr-notifications.js" -ForegroundColor White
Write-Host ""
Write-Host "This should fix the 'getApiBaseUrl was undefined' error" -ForegroundColor Green
