# Test Runner Script for Inventory Control
# Runs all tests with different configurations

param(
    [string]$Project = "all",
    [string]$Configuration = "Debug",
    [switch]$Coverage,
    [switch]$Verbose,
    [switch]$Help
)

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

if ($Help) {
    Write-ColorOutput "Usage: .\run-tests.ps1 [-Project <project>] [-Configuration <config>] [-Coverage] [-Verbose] [-Help]" -Color Cyan
    Write-ColorOutput ""
    Write-ColorOutput "Parameters:" -Color Yellow
    Write-ColorOutput "  -Project      Test project to run (all, unit, integration, component)" -Color Gray
    Write-ColorOutput "  -Configuration Build configuration (Debug, Release)" -Color Gray
    Write-ColorOutput "  -Coverage     Generate code coverage report" -Color Gray
    Write-ColorOutput "  -Verbose      Show detailed test output" -Color Gray
    Write-ColorOutput "  -Help         Show this help message" -Color Gray
    Write-ColorOutput ""
    Write-ColorOutput "Examples:" -Color Yellow
    Write-ColorOutput "  .\run-tests.ps1                    # Run all tests" -Color Gray
    Write-ColorOutput "  .\run-tests.ps1 -Project unit      # Run unit tests only" -Color Gray
    Write-ColorOutput "  .\run-tests.ps1 -Coverage -Verbose # Run with coverage and verbose output" -Color Gray
    exit 0
}

Write-ColorOutput "=== Inventory Control Test Runner ===" -Color Green

# Set UTF-8 encoding
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# Build arguments
$testArgs = @("test")
$loggerArgs = @()

if ($Verbose) {
    $loggerArgs += "--logger"
    $loggerArgs += "console;verbosity=detailed"
}

if ($Coverage) {
    $testArgs += "--collect:XPlat Code Coverage"
    $testArgs += "--settings"
    $testArgs += "coverlet.runsettings"
}

$testArgs += "--configuration"
$testArgs += $Configuration

# Determine which projects to test
switch ($Project.ToLower()) {
    "unit" {
        $testArgs += "Inventory.UnitTests"
        Write-ColorOutput "Running Unit Tests..." -Color Yellow
    }
    "integration" {
        $testArgs += "Inventory.IntegrationTests"
        Write-ColorOutput "Running Integration Tests..." -Color Yellow
    }
    "component" {
        $testArgs += "Inventory.ComponentTests"
        Write-ColorOutput "Running Component Tests..." -Color Yellow
    }
    "all" {
        Write-ColorOutput "Running All Tests..." -Color Yellow
    }
    default {
        Write-ColorOutput "Invalid project specified. Use: all, unit, integration, component" -Color Red
        exit 1
    }
}

# Add logger arguments
if ($loggerArgs.Count -gt 0) {
    $testArgs += $loggerArgs
}

Write-ColorOutput "Command: dotnet $($testArgs -join ' ')" -Color Gray
Write-ColorOutput ""

try {
    # Run tests
    $result = & dotnet @testArgs
    $testExitCode = $LASTEXITCODE
    
    if ($testExitCode -eq 0) {
        Write-ColorOutput "✅ All tests passed!" -Color Green
    } else {
        Write-ColorOutput "❌ Some tests failed!" -Color Red
    }
    
    # Cleanup test databases
    Write-ColorOutput "`n🧹 Cleaning up test databases..." -Color Yellow
    
    # Check if PostgreSQL container is running
    $containerName = "inventoryctrl-db-1"
    $containerStatus = docker ps --filter "name=$containerName" --format "{{.Status}}"
    
    if ($containerStatus) {
        # Get list of test databases
        $testDatabases = docker exec $containerName psql -U postgres -t -c "SELECT datname FROM pg_database WHERE datname LIKE 'inventory_test_%';" 2>$null
        
        if ($testDatabases -and $testDatabases.Trim() -ne "") {
            $dbCount = ($testDatabases -split "`n" | Where-Object { $_.Trim() -ne "" }).Count
            Write-ColorOutput "🗑️  Found $dbCount test databases to clean up" -Color Yellow
            
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
            
            Write-ColorOutput "✅ Cleaned up $deletedCount test databases" -Color Green
        } else {
            Write-ColorOutput "✅ No test databases found to clean up" -Color Green
        }
    } else {
        Write-ColorOutput "⚠️  PostgreSQL container not running, skipping cleanup" -Color Yellow
    }
    
    # Exit with test result code
    if ($testExitCode -ne 0) {
        exit $testExitCode
    }
    
} catch {
    Write-ColorOutput "❌ Error running tests: $($_.Exception.Message)" -Color Red
    exit 1
}

Write-ColorOutput ""
Write-ColorOutput "=== Test Run Complete ===" -Color Green
