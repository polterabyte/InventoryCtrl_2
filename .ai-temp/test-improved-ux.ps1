# Test Improved UX for SignalR Notifications
Write-Host "Testing improved UX for SignalR notifications..." -ForegroundColor Green

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "UX Improvements Applied:" -ForegroundColor Cyan
    Write-Host "1. Before login: 'Sign in to enable notifications'" -ForegroundColor White
    Write-Host "2. After login: 'Real-time notifications active' (when connected)" -ForegroundColor White
    Write-Host "3. Connection issues: 'Real-time notifications offline'" -ForegroundColor White
    Write-Host ""
    Write-Host "Test Steps:" -ForegroundColor Yellow
    Write-Host "1. Open the application in browser" -ForegroundColor White
    Write-Host "2. Check notification status before login" -ForegroundColor White
    Write-Host "3. Login to the application" -ForegroundColor White
    Write-Host "4. Check notification status after login" -ForegroundColor White
    Write-Host "5. Verify SignalR connects automatically" -ForegroundColor White
    Write-Host ""
    Write-Host "Expected Behavior:" -ForegroundColor Cyan
    Write-Host "- Before login: Shows 'Sign in to enable notifications'" -ForegroundColor White
    Write-Host "- After login: Shows 'Real-time notifications active'" -ForegroundColor White
    Write-Host "- Console should show 'SignalR connection started'" -ForegroundColor White
    Write-Host ""
    
    # Start the application
    .\start-apps.ps1
} else {
    Write-Host "Build failed! Please check the errors above." -ForegroundColor Red
}
