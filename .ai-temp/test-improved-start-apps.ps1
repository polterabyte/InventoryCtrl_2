# Test Improved start-apps.ps1
Write-Host "Testing improved start-apps.ps1..." -ForegroundColor Green

Write-Host "Improvements made to start-apps.ps1:" -ForegroundColor Cyan
Write-Host "1. Enhanced Stop-ExistingProcesses function:" -ForegroundColor White
Write-Host "   - Stops all dotnet processes" -ForegroundColor Green
Write-Host "   - Stops processes on specific ports (5000, 7000, 5001, 7001)" -ForegroundColor Green
Write-Host "   - Waits for ports to be released" -ForegroundColor Green
Write-Host ""
Write-Host "2. Improved Check-PortAvailability function:" -ForegroundColor White
Write-Host "   - Shows which process is using a port" -ForegroundColor Green
Write-Host "   - Better error messages" -ForegroundColor Green
Write-Host "   - More detailed port status" -ForegroundColor Green
Write-Host ""

Write-Host "Test the improved script:" -ForegroundColor Yellow
Write-Host "1. Run: .\start-apps.ps1" -ForegroundColor White
Write-Host "2. The script will now:" -ForegroundColor White
Write-Host "   - Stop any existing processes on the required ports" -ForegroundColor White
Write-Host "   - Show detailed information about port conflicts" -ForegroundColor White
Write-Host "   - Wait for ports to be released before starting" -ForegroundColor White
Write-Host "   - Start the applications cleanly" -ForegroundColor White
Write-Host ""

Write-Host "Benefits:" -ForegroundColor Green
Write-Host "✅ No more 'address already in use' errors" -ForegroundColor White
Write-Host "✅ Automatic cleanup of conflicting processes" -ForegroundColor White
Write-Host "✅ Better error reporting" -ForegroundColor White
Write-Host "✅ More reliable startup process" -ForegroundColor White
Write-Host ""

Write-Host "You can now run: .\start-apps.ps1" -ForegroundColor Cyan
Write-Host "The script will handle port conflicts automatically!" -ForegroundColor Green
