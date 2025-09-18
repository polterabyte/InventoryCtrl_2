# Test SignalR Fix Script
Write-Host "Testing SignalR Fixes..." -ForegroundColor Green

# Check if .ai-temp directory exists, create if not
if (!(Test-Path ".ai-temp")) {
    New-Item -ItemType Directory -Path ".ai-temp" -Force
    Write-Host "Created .ai-temp directory" -ForegroundColor Yellow
}

# Clean previous build artifacts
Write-Host "Cleaning previous build artifacts..." -ForegroundColor Yellow
& .\scripts\Clean-BuildArtifacts.ps1

# Build the solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Start the application
Write-Host "Starting application..." -ForegroundColor Yellow
& .\start-apps.ps1

Write-Host "Application started. Please check the browser console for SignalR errors." -ForegroundColor Cyan
Write-Host "Look for these improvements:" -ForegroundColor Cyan
Write-Host "   - No 'Failed to start the HttpConnection before stop() was called' errors" -ForegroundColor White
Write-Host "   - No JWT token expiration errors" -ForegroundColor White
Write-Host "   - SignalR connection should start successfully" -ForegroundColor White
Write-Host "   - No 'connectionestablished' method warnings" -ForegroundColor White

Write-Host ""
Write-Host "Test completed. Check browser console for results." -ForegroundColor Green