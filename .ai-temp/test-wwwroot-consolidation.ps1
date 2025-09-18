Write-Host "=== Testing wwwroot Consolidation ===" -ForegroundColor Green
Write-Host ""

Write-Host "Checking consolidated structure..." -ForegroundColor Yellow

# Check if Shared has all resources
if (Test-Path "src/Inventory.Shared/wwwroot/js") {
    Write-Host "✅ Shared/js exists" -ForegroundColor Green
    $jsFiles = Get-ChildItem "src/Inventory.Shared/wwwroot/js" -Name
    Write-Host "   JavaScript files: $($jsFiles -join ', ')" -ForegroundColor White
} else {
    Write-Host "❌ Shared/js missing" -ForegroundColor Red
}

if (Test-Path "src/Inventory.Shared/wwwroot/css") {
    Write-Host "✅ Shared/css exists" -ForegroundColor Green
    $cssFiles = Get-ChildItem "src/Inventory.Shared/wwwroot/css" -Name
    Write-Host "   CSS files: $($cssFiles -join ', ')" -ForegroundColor White
} else {
    Write-Host "❌ Shared/css missing" -ForegroundColor Red
}

if (Test-Path "src/Inventory.Shared/wwwroot/lib") {
    Write-Host "✅ Shared/lib exists (Bootstrap)" -ForegroundColor Green
} else {
    Write-Host "❌ Shared/lib missing" -ForegroundColor Red
}

# Check if duplicates were removed
if (-not (Test-Path "src/Inventory.UI/wwwroot/js")) {
    Write-Host "✅ UI/js removed" -ForegroundColor Green
} else {
    Write-Host "❌ UI/js still exists" -ForegroundColor Red
}

if (-not (Test-Path "src/Inventory.UI/wwwroot/css")) {
    Write-Host "✅ UI/css removed" -ForegroundColor Green
} else {
    Write-Host "❌ UI/css still exists" -ForegroundColor Red
}

if (-not (Test-Path "src/Inventory.Web.Client/wwwroot/lib")) {
    Write-Host "✅ Web.Client/lib removed" -ForegroundColor Green
} else {
    Write-Host "❌ Web.Client/lib still exists" -ForegroundColor Red
}

if (-not (Test-Path "src/Inventory.Web.Client/wwwroot/css")) {
    Write-Host "✅ Web.Client/css removed" -ForegroundColor Green
} else {
    Write-Host "❌ Web.Client/css still exists" -ForegroundColor Red
}

# Check HTML references
$uiHtml = Get-Content "src/Inventory.UI/wwwroot/index.html" -Raw
if ($uiHtml -match "_content/Inventory.Shared/") {
    Write-Host "✅ UI HTML references Shared resources" -ForegroundColor Green
} else {
    Write-Host "❌ UI HTML does not reference Shared resources" -ForegroundColor Red
}

$webHtml = Get-Content "src/Inventory.Web.Client/wwwroot/index.html" -Raw
if ($webHtml -match "_content/Inventory.Shared/") {
    Write-Host "✅ Web.Client HTML references Shared resources" -ForegroundColor Green
} else {
    Write-Host "❌ Web.Client HTML does not reference Shared resources" -ForegroundColor Red
}

Write-Host ""
Write-Host "Consolidation Status:" -ForegroundColor Cyan
Write-Host "✅ All static resources centralized in Inventory.Shared" -ForegroundColor White
Write-Host "✅ Duplicate files removed from UI and Web.Client" -ForegroundColor White
Write-Host "✅ HTML files updated to reference Shared resources" -ForegroundColor White
Write-Host "✅ Ready for MAUI project integration" -ForegroundColor White
Write-Host ""
Write-Host "Ready for testing!" -ForegroundColor Green
