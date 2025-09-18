# Simple test for notification banner
Write-Host "Testing notification banner behavior..." -ForegroundColor Green

# Wait for applications to start
Start-Sleep -Seconds 5

# Check if applications are running
$apiProcess = Get-Process -Name "Inventory.API" -ErrorAction SilentlyContinue
$uiProcess = Get-Process -Name "Inventory.UI" -ErrorAction SilentlyContinue

if ($apiProcess -and $uiProcess) {
    Write-Host "✅ Applications are running" -ForegroundColor Green
    Write-Host "API: https://localhost:7000" -ForegroundColor White
    Write-Host "UI: https://localhost:7001" -ForegroundColor White
} else {
    Write-Host "❌ Applications are not running" -ForegroundColor Red
    if (-not $apiProcess) { Write-Host "API is not running" -ForegroundColor Red }
    if (-not $uiProcess) { Write-Host "UI is not running" -ForegroundColor Red }
    exit 1
}

Write-Host "`n🔍 Test Instructions:" -ForegroundColor Cyan
Write-Host "1. Open browser and go to: https://localhost:7001" -ForegroundColor White
Write-Host "2. Open Developer Tools (F12) and go to Console tab" -ForegroundColor White
Write-Host "3. Look for these log messages:" -ForegroundColor White
Write-Host "   - 'RealTimeNotificationComponent initialized'" -ForegroundColor Gray
Write-Host "   - 'RealTimeNotificationComponent first render'" -ForegroundColor Gray
Write-Host "   - 'Authentication state check: IsAuthenticated=...'" -ForegroundColor Gray
Write-Host "4. Check if you see the notification banner" -ForegroundColor White
Write-Host "5. If you're logged in, logout first to see the banner" -ForegroundColor White

Write-Host "`n📋 Expected Results:" -ForegroundColor Yellow
Write-Host "✅ If NOT logged in: Yellow banner appears with close button" -ForegroundColor White
Write-Host "✅ Banner auto-disappears after 10 seconds" -ForegroundColor White
Write-Host "✅ Banner can be closed manually with X button" -ForegroundColor White
Write-Host "✅ If logged in: No banner appears" -ForegroundColor White

Write-Host "`n🐛 If banner doesn't disappear:" -ForegroundColor Red
Write-Host "- Check browser console for errors" -ForegroundColor White
Write-Host "- Look for authentication state messages" -ForegroundColor White
Write-Host "- Try refreshing the page" -ForegroundColor White
Write-Host "- Check if you are actually logged in" -ForegroundColor White

Write-Host "`n📝 Report what you see in the browser console and whether the banner behaves correctly." -ForegroundColor Cyan
