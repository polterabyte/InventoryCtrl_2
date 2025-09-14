# Cleanup-TestDatabases.ps1
# Script to clean up all test databases after testing

param(
    [switch]$Force = $false
)

Write-Host "🧹 Cleaning up test databases..." -ForegroundColor Yellow

# Check if PostgreSQL container is running
$containerName = "inventoryctrl-db-1"
$containerStatus = docker ps --filter "name=$containerName" --format "{{.Status}}"

if (-not $containerStatus) {
    Write-Host "❌ PostgreSQL container '$containerName' is not running!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ PostgreSQL container is running" -ForegroundColor Green

# Get list of test databases
Write-Host "🔍 Searching for test databases..." -ForegroundColor Cyan
$testDatabases = docker exec $containerName psql -U postgres -t -c "SELECT datname FROM pg_database WHERE datname LIKE 'inventory_test_%';" 2>$null

if (-not $testDatabases -or $testDatabases.Trim() -eq "") {
    Write-Host "✅ No test databases found to clean up" -ForegroundColor Green
    exit 0
}

# Count databases
$dbCount = ($testDatabases -split "`n" | Where-Object { $_.Trim() -ne "" }).Count
Write-Host "📊 Found $dbCount test databases to clean up" -ForegroundColor Yellow

if (-not $Force) {
    $confirmation = Read-Host "Do you want to delete all test databases? (y/N)"
    if ($confirmation -ne "y" -and $confirmation -ne "Y") {
        Write-Host "❌ Cleanup cancelled by user" -ForegroundColor Red
        exit 0
    }
}

# Delete each test database
Write-Host "🗑️  Deleting test databases..." -ForegroundColor Yellow
$deletedCount = 0
$errorCount = 0

foreach ($db in $testDatabases) {
    $dbName = $db.Trim()
    if ($dbName -eq "") { continue }
    
    try {
        $result = docker exec $containerName psql -U postgres -c "DROP DATABASE IF EXISTS `"$dbName`";" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ Deleted: $dbName" -ForegroundColor Green
            $deletedCount++
        } else {
            Write-Host "  ❌ Failed to delete: $dbName - $result" -ForegroundColor Red
            $errorCount++
        }
    }
    catch {
        Write-Host "  ❌ Error deleting $dbName : $($_.Exception.Message)" -ForegroundColor Red
        $errorCount++
    }
}

# Summary
Write-Host "`n📊 Cleanup Summary:" -ForegroundColor Cyan
Write-Host "  ✅ Successfully deleted: $deletedCount databases" -ForegroundColor Green
if ($errorCount -gt 0) {
    Write-Host "  ❌ Failed to delete: $errorCount databases" -ForegroundColor Red
}

# Verify cleanup
Write-Host "`n🔍 Verifying cleanup..." -ForegroundColor Cyan
$remainingDbs = docker exec $containerName psql -U postgres -t -c "SELECT datname FROM pg_database WHERE datname LIKE 'inventory_test_%';" 2>$null
$remainingCount = ($remainingDbs -split "`n" | Where-Object { $_.Trim() -ne "" }).Count

if ($remainingCount -eq 0) {
    Write-Host "✅ All test databases successfully cleaned up!" -ForegroundColor Green
} else {
    Write-Host "⚠️  $remainingCount test databases still remain" -ForegroundColor Yellow
}

Write-Host "`n🎉 Cleanup completed!" -ForegroundColor Green
