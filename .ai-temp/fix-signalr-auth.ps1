# Fix SignalR Authentication Issue
Write-Host "Fixing SignalR authentication issue..." -ForegroundColor Green

# Backup current files
Write-Host "Creating backups..." -ForegroundColor Yellow
Copy-Item "src\Inventory.UI\wwwroot\js\signalr-notifications.js" "src\Inventory.UI\wwwroot\js\signalr-notifications.js.auth-backup" -Force
Copy-Item "src\Inventory.Web.Client\wwwroot\js\signalr-notifications.js" "src\Inventory.Web.Client\wwwroot\js\signalr-notifications.js.auth-backup" -Force

# Apply the fix
Write-Host "Applying authentication fix..." -ForegroundColor Yellow
Copy-Item ".ai-temp\signalr-notifications-fixed-auth.js" "src\Inventory.UI\wwwroot\js\signalr-notifications.js" -Force
Copy-Item ".ai-temp\signalr-notifications-fixed-auth.js" "src\Inventory.Web.Client\wwwroot\js\signalr-notifications.js" -Force

Write-Host "Authentication fix applied!" -ForegroundColor Green
Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Removed accessTokenFactory (not working with JWT)" -ForegroundColor White
Write-Host "2. Added access_token as query parameter in URL" -ForegroundColor White
Write-Host "3. Added detailed logging for debugging" -ForegroundColor White
Write-Host "4. Improved error handling" -ForegroundColor White
Write-Host ""
Write-Host "Now SignalR will connect using:" -ForegroundColor Yellow
Write-Host "URL: http://localhost:5000/notificationHub?access_token=JWT_TOKEN" -ForegroundColor White
Write-Host "This matches the JWT configuration in Program.cs" -ForegroundColor White
Write-Host ""
Write-Host "Please refresh your browser and test again" -ForegroundColor Cyan
