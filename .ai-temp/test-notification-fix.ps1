# Test script for notification banner fix
Write-Host "Testing notification banner fix..." -ForegroundColor Green

# Check if the application is running
$apiProcess = Get-Process -Name "Inventory.API" -ErrorAction SilentlyContinue
$uiProcess = Get-Process -Name "Inventory.UI" -ErrorAction SilentlyContinue

if (-not $apiProcess -or -not $uiProcess) {
    Write-Host "Starting applications..." -ForegroundColor Yellow
    & ".\start-apps.ps1"
    Start-Sleep -Seconds 10
}

Write-Host "Applications should be running now." -ForegroundColor Green
Write-Host "Please test the following scenarios:" -ForegroundColor Cyan
Write-Host "1. Open the application in browser" -ForegroundColor White
Write-Host "2. Check if 'Sign in to enable notifications' banner appears" -ForegroundColor White
Write-Host "3. Verify the banner has a close button (X)" -ForegroundColor White
Write-Host "4. Click the close button to dismiss the banner" -ForegroundColor White
Write-Host "5. Verify the banner disappears after 10 seconds automatically" -ForegroundColor White
Write-Host "6. Login to the application" -ForegroundColor White
Write-Host "7. Verify the banner doesn't appear when authenticated" -ForegroundColor White
Write-Host "8. Logout and verify the banner appears again" -ForegroundColor White

Write-Host "`nExpected behavior:" -ForegroundColor Yellow
Write-Host "- Banner should appear only when not authenticated" -ForegroundColor White
Write-Host "- Banner should be dismissible with close button" -ForegroundColor White
Write-Host "- Banner should auto-dismiss after 10 seconds" -ForegroundColor White
Write-Host "- Banner should not appear when user is authenticated" -ForegroundColor White
Write-Host "- Banner should have yellow/warning styling instead of red/error" -ForegroundColor White

Write-Host "`nTest completed. Check the browser for the expected behavior." -ForegroundColor Green
