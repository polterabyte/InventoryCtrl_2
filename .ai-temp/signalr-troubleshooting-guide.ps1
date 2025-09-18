# SignalR Troubleshooting Guide
Write-Host "=== SignalR Connection Troubleshooting Guide ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Problem: 'HubConnectionBuilder is not a constructor' error" -ForegroundColor Red
Write-Host ""
Write-Host "Root Causes:" -ForegroundColor Yellow
Write-Host "1. SignalR library not loaded before trying to use it" -ForegroundColor White
Write-Host "2. Dynamic import from CDN failing in Blazor WebAssembly context" -ForegroundColor White
Write-Host "3. Timing issues with script loading" -ForegroundColor White
Write-Host "4. Incorrect import syntax or module resolution" -ForegroundColor White
Write-Host ""

Write-Host "Solutions Applied:" -ForegroundColor Green
Write-Host "1. Added SignalR script tag to HTML files" -ForegroundColor White
Write-Host "2. Fixed JavaScript to use global window.signalR instead of dynamic import" -ForegroundColor White
Write-Host "3. Added proper error handling and fallbacks" -ForegroundColor White
Write-Host "4. Created both CDN and local versions" -ForegroundColor White
Write-Host ""

Write-Host "Available Fixes:" -ForegroundColor Cyan
Write-Host "1. CDN Version (Applied): Uses CDN with proper script loading" -ForegroundColor White
Write-Host "2. Local Version: Downloads SignalR locally for better reliability" -ForegroundColor White
Write-Host ""

Write-Host "To test the fix:" -ForegroundColor Yellow
Write-Host "1. Run: .\ai-temp\test-signalr-fix.ps1" -ForegroundColor White
Write-Host "2. Check browser console for SignalR messages" -ForegroundColor White
Write-Host "3. Look for 'SignalR connection started' message" -ForegroundColor White
Write-Host ""

Write-Host "If CDN version doesn't work, try local version:" -ForegroundColor Yellow
Write-Host "1. Run: .\ai-temp\download-signalr-local.ps1" -ForegroundColor White
Write-Host "2. This downloads SignalR locally and updates HTML files" -ForegroundColor White
Write-Host ""

Write-Host "Debug Information:" -ForegroundColor Cyan
Write-Host "- Check browser console for detailed error messages" -ForegroundColor White
Write-Host "- Verify SignalR script is loaded: window.signalR should exist" -ForegroundColor White
Write-Host "- Check network tab for failed script loads" -ForegroundColor White
Write-Host "- Verify API endpoint is accessible: /notificationHub" -ForegroundColor White
Write-Host ""

Write-Host "Backup files created with .backup extension" -ForegroundColor Green
Write-Host "You can restore originals if needed" -ForegroundColor Green
