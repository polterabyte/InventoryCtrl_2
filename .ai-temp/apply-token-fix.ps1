# Apply Token Fix for SignalR Notifications
Write-Host "Applying token fix for SignalR notifications..." -ForegroundColor Green

# Backup original component
Write-Host "Creating backup..." -ForegroundColor Yellow
Copy-Item "src\Inventory.UI\Components\RealTimeNotificationComponent.razor" "src\Inventory.UI\Components\RealTimeNotificationComponent.razor.backup" -Force

# Apply the fix
Write-Host "Applying fix..." -ForegroundColor Yellow
Copy-Item ".ai-temp\RealTimeNotificationComponent-fixed.razor" "src\Inventory.UI\Components\RealTimeNotificationComponent.razor" -Force

Write-Host "Token fix applied!" -ForegroundColor Green
Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Added AuthenticationStateProvider injection" -ForegroundColor White
Write-Host "2. Added listener for authentication state changes" -ForegroundColor White
Write-Host "3. Auto-reconnect SignalR when user logs in" -ForegroundColor White
Write-Host "4. Auto-disconnect SignalR when user logs out" -ForegroundColor White
Write-Host "5. Proper cleanup on component disposal" -ForegroundColor White
Write-Host ""
Write-Host "Now the component will:" -ForegroundColor Yellow
Write-Host "- Show 'Sign in to enable notifications' when not logged in" -ForegroundColor White
Write-Host "- Automatically connect to SignalR when user logs in" -ForegroundColor White
Write-Host "- Show 'Real-time notifications active' when connected" -ForegroundColor White
Write-Host "- Disconnect when user logs out" -ForegroundColor White
Write-Host ""
Write-Host "Please rebuild and test the application" -ForegroundColor Cyan
