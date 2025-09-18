# Final test for notification banner
Write-Host "Testing notification banner with working API..." -ForegroundColor Green

# Check if API is running
$apiProcess = Get-Process -Name "Inventory.API" -ErrorAction SilentlyContinue
if ($apiProcess) {
    Write-Host "✅ API is running (PID: $($apiProcess.Id))" -ForegroundColor Green
    Write-Host "API URL: https://localhost:7000" -ForegroundColor White
} else {
    Write-Host "❌ API is not running" -ForegroundColor Red
    Write-Host "Starting API..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-ExecutionPolicy Bypass -File start-apps.ps1" -WindowStyle Hidden
    Start-Sleep -Seconds 10
}

Write-Host "`n🔍 Test Instructions:" -ForegroundColor Cyan
Write-Host "1. Open browser and go to: https://localhost:7000" -ForegroundColor White
Write-Host "2. This will show the API Swagger interface" -ForegroundColor White
Write-Host "3. For the UI, we need to manually start it" -ForegroundColor White

Write-Host "`n📋 Manual UI Test:" -ForegroundColor Yellow
Write-Host "1. Open a new terminal" -ForegroundColor White
Write-Host "2. Navigate to: cd src\Inventory.Web.Client" -ForegroundColor White
Write-Host "3. Run: dotnet run --urls 'https://localhost:7001;http://localhost:5001'" -ForegroundColor White
Write-Host "4. Open browser to: https://localhost:7001" -ForegroundColor White

Write-Host "`n🎯 Expected Results:" -ForegroundColor Cyan
Write-Host "✅ If NOT logged in: Yellow banner appears with close button" -ForegroundColor White
Write-Host "✅ Banner auto-disappears after 10 seconds" -ForegroundColor White
Write-Host "✅ Banner can be closed manually with X button" -ForegroundColor White
Write-Host "✅ If logged in: No banner appears" -ForegroundColor White

Write-Host "`n📝 The notification banner fix has been implemented with:" -ForegroundColor Green
Write-Host "- Auto-dismiss after 10 seconds" -ForegroundColor White
Write-Host "- Manual close button" -ForegroundColor White
Write-Host "- Proper authentication state detection" -ForegroundColor White
Write-Host "- Yellow warning styling instead of red error" -ForegroundColor White
Write-Host "- Logging for debugging" -ForegroundColor White

Write-Host "`n🔧 If you still see issues:" -ForegroundColor Red
Write-Host "- Check browser console for JavaScript errors" -ForegroundColor White
Write-Host "- Look for authentication state messages in console" -ForegroundColor White
Write-Host "- Try refreshing the page" -ForegroundColor White
Write-Host "- Check if you're actually logged in" -ForegroundColor White
