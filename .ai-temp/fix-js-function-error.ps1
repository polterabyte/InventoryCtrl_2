# Fix JavaScript Function Error
Write-Host "Fixing JavaScript function error..." -ForegroundColor Green

# Backup current component
Write-Host "Creating backup..." -ForegroundColor Yellow
Copy-Item "src\Inventory.UI\Components\RealTimeNotificationComponent.razor" "src\Inventory.UI\Components\RealTimeNotificationComponent.razor.js-backup" -Force

# Apply the fix
Write-Host "Applying JavaScript function fix..." -ForegroundColor Yellow
Copy-Item ".ai-temp\RealTimeNotificationComponent-fixed-js.razor" "src\Inventory.UI\Components\RealTimeNotificationComponent.razor" -Force

Write-Host "JavaScript function fix applied!" -ForegroundColor Green
Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Added delay before initializing SignalR (100ms)" -ForegroundColor White
Write-Host "2. Added check if getApiBaseUrl function is available" -ForegroundColor White
Write-Host "3. Added retry mechanism if function is not available" -ForegroundColor White
Write-Host "4. Added better error handling and logging" -ForegroundColor White
Write-Host ""
Write-Host "This fixes the 'getApiBaseUrl was undefined' error" -ForegroundColor Green
Write-Host "The component now waits for JavaScript functions to load" -ForegroundColor Green
Write-Host ""
Write-Host "Please rebuild and test the application" -ForegroundColor Cyan
