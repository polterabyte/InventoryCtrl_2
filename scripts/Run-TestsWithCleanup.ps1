# Run-TestsWithCleanup.ps1
# Script to run tests with automatic cleanup of test databases

param(
    [string]$Project = "",
    [string]$Filter = "",
    [switch]$Verbose = $false,
    [switch]$NoCleanup = $false
)

Write-Host "🧪 Running tests with automatic cleanup..." -ForegroundColor Cyan

# Build the dotnet test command
$testCommand = "dotnet test"

if ($Project) {
    $testCommand += " $Project"
}

if ($Filter) {
    $testCommand += " --filter `"$Filter`""
}

if ($Verbose) {
    $testCommand += " --verbosity normal"
} else {
    $testCommand += " --verbosity minimal"
}

Write-Host "📋 Test command: $testCommand" -ForegroundColor Yellow

# Run tests
Write-Host "`n🚀 Starting test execution..." -ForegroundColor Green
$testResult = Invoke-Expression $testCommand
$exitCode = $LASTEXITCODE

Write-Host "`n📊 Test execution completed with exit code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })

# Cleanup test databases unless explicitly disabled
if (-not $NoCleanup) {
    Write-Host "`n🧹 Starting cleanup of test databases..." -ForegroundColor Yellow
    
    # Check if PostgreSQL container is running
    $containerName = "inventoryctrl-db-1"
    $containerStatus = docker ps --filter "name=$containerName" --format "{{.Status}}"
    
    if ($containerStatus) {
        # Get list of test databases
        $testDatabases = docker exec $containerName psql -U postgres -t -c "SELECT datname FROM pg_database WHERE datname LIKE 'inventory_test_%';" 2>$null
        
        if ($testDatabases -and $testDatabases.Trim() -ne "") {
            $dbCount = ($testDatabases -split "`n" | Where-Object { $_.Trim() -ne "" }).Count
            Write-Host "🗑️  Found $dbCount test databases to clean up" -ForegroundColor Yellow
            
            # Delete each test database
            $deletedCount = 0
            foreach ($db in $testDatabases) {
                $dbName = $db.Trim()
                if ($dbName -eq "") { continue }
                
                try {
                    $result = docker exec $containerName psql -U postgres -c "DROP DATABASE IF EXISTS `"$dbName`";" 2>&1
                    if ($LASTEXITCODE -eq 0) {
                        $deletedCount++
                    }
                }
                catch {
                    # Ignore errors
                }
            }
            
            Write-Host "✅ Cleaned up $deletedCount test databases" -ForegroundColor Green
        } else {
            Write-Host "✅ No test databases found to clean up" -ForegroundColor Green
        }
    } else {
        Write-Host "⚠️  PostgreSQL container not running, skipping cleanup" -ForegroundColor Yellow
    }
} else {
    Write-Host "⏭️  Skipping cleanup (--NoCleanup flag specified)" -ForegroundColor Yellow
}

Write-Host "`n🎉 Test run completed!" -ForegroundColor Green

# Return the test exit code
exit $exitCode
