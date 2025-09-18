# Test Token Fix for SignalR Notifications
Write-Host "Testing token fix for SignalR notifications..." -ForegroundColor Green

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Test Steps:" -ForegroundColor Cyan
    Write-Host "1. Open the application in browser" -ForegroundColor White
    Write-Host "2. Check notification status (should show 'Sign in to enable notifications')" -ForegroundColor White
    Write-Host "3. Login as super admin" -ForegroundColor White
    Write-Host "4. Check notification status (should show 'Real-time notifications active')" -ForegroundColor White
    Write-Host "5. Check browser console for SignalR connection messages" -ForegroundColor White
    Write-Host "6. Logout and check status (should show 'Sign in to enable notifications')" -ForegroundColor White
    Write-Host ""
    Write-Host "Expected Behavior:" -ForegroundColor Yellow
    Write-Host "- Before login: 'Sign in to enable notifications'" -ForegroundColor White
    Write-Host "- After login: 'Real-time notifications active'" -ForegroundColor White
    Write-Host "- Console should show: 'SignalR connection started'" -ForegroundColor White
    Write-Host "- After logout: 'Sign in to enable notifications'" -ForegroundColor White
    Write-Host ""
    Write-Host "If you still see 'Sign in to enable notifications' after login:" -ForegroundColor Red
    Write-Host "1. Check browser console for errors" -ForegroundColor White
    Write-Host "2. Verify token is saved in localStorage (F12 -> Application -> Local Storage)" -ForegroundColor White
    Write-Host "3. Check if API endpoint /notificationHub is accessible" -ForegroundColor White
    Write-Host ""
    
    # Start the application
    .\start-apps.ps1
} else {
    Write-Host "Build failed! Please check the errors above." -ForegroundColor Red
}
