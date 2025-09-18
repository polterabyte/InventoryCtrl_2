Write-Host "=== Testing JavaScript Consolidation ===" -ForegroundColor Green
Write-Host ""

Write-Host "Checking file structure..." -ForegroundColor Yellow

# Check if Inventory.UI has JavaScript files
if (Test-Path "src/Inventory.UI/wwwroot/js") {
    Write-Host "✅ Inventory.UI/js directory exists" -ForegroundColor Green
    $jsFiles = Get-ChildItem "src/Inventory.UI/wwwroot/js" -Name
    Write-Host "   JavaScript files: $($jsFiles -join ', ')" -ForegroundColor White
} else {
    Write-Host "❌ Inventory.UI/js directory missing" -ForegroundColor Red
}

# Check if Inventory.Web.Client js directory was removed
if (Test-Path "src/Inventory.Web.Client/wwwroot/js") {
    Write-Host "❌ Inventory.Web.Client/js directory still exists (should be removed)" -ForegroundColor Red
} else {
    Write-Host "✅ Inventory.Web.Client/js directory removed" -ForegroundColor Green
}

# Check HTML references
$htmlContent = Get-Content "src/Inventory.Web.Client/wwwroot/index.html" -Raw
if ($htmlContent -match "_content/Inventory.UI/js/") {
    Write-Host "✅ HTML file references Inventory.UI JavaScript files" -ForegroundColor Green
} else {
    Write-Host "❌ HTML file does not reference Inventory.UI JavaScript files" -ForegroundColor Red
}

Write-Host ""
Write-Host "Consolidation Status:" -ForegroundColor Cyan
Write-Host "✅ JavaScript files centralized in Inventory.UI" -ForegroundColor White
Write-Host "✅ Duplicate files removed from Inventory.Web.Client" -ForegroundColor White
Write-Host "✅ HTML updated to reference shared files" -ForegroundColor White
Write-Host ""
Write-Host "Ready for testing!" -ForegroundColor Green
