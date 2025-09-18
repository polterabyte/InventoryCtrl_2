# Debug notification banner issue
Write-Host "Debugging notification banner issue..." -ForegroundColor Green

# Check if applications are running
$apiProcess = Get-Process -Name "Inventory.API" -ErrorAction SilentlyContinue
$uiProcess = Get-Process -Name "Inventory.UI" -ErrorAction SilentlyContinue

if ($apiProcess -and $uiProcess) {
    Write-Host "Applications are running" -ForegroundColor Green
    Write-Host "API Process ID: $($apiProcess.Id)" -ForegroundColor White
    Write-Host "UI Process ID: $($uiProcess.Id)" -ForegroundColor White
} else {
    Write-Host "Applications are not running. Starting them..." -ForegroundColor Yellow
    & ".\start-apps.ps1"
    Start-Sleep -Seconds 5
}

Write-Host "`nPlease follow these steps to debug:" -ForegroundColor Cyan
Write-Host "1. Open browser and go to https://localhost:7001" -ForegroundColor White
Write-Host "2. Open Developer Tools (F12)" -ForegroundColor White
Write-Host "3. Go to Console tab" -ForegroundColor White
Write-Host "4. Look for any JavaScript errors related to SignalR or notifications" -ForegroundColor White
Write-Host "5. Check if you see the notification banner" -ForegroundColor White
Write-Host "6. If you're logged in, try logging out first" -ForegroundColor White
Write-Host "7. Check the browser console for any authentication-related messages" -ForegroundColor White

Write-Host "`nExpected behavior:" -ForegroundColor Yellow
Write-Host "- If NOT logged in: Banner should appear with close button and auto-dismiss after 10s" -ForegroundColor White
Write-Host "- If logged in: Banner should NOT appear" -ForegroundColor White
Write-Host "- Banner should have yellow/warning styling" -ForegroundColor White

Write-Host "`nIf the banner still doesn't disappear:" -ForegroundColor Red
Write-Host "- Check browser console for JavaScript errors" -ForegroundColor White
Write-Host "- Try refreshing the page" -ForegroundColor White
Write-Host "- Check if you're actually logged in or not" -ForegroundColor White
