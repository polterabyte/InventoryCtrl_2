# Docker Test Runner Script for Inventory Control
# Runs tests in containerized environment

param(
    [string]$TestType = "all",
    [switch]$Build,
    [switch]$Clean,
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

function Show-Help {
    Write-ColorOutput "Docker Test Runner for Inventory Control System" -Color Cyan
    Write-ColorOutput ""
    Write-ColorOutput "Usage: .\Run-Tests-Docker.ps1 [-TestType <type>] [-Build] [-Clean] [-Coverage] [-Verbose] [-Help]" -Color Cyan
    Write-ColorOutput ""
    Write-ColorOutput "Parameters:" -Color Yellow
    Write-ColorOutput "  -TestType      Type of tests to run (all, unit, integration, component)" -Color Gray
    Write-ColorOutput "  -Build         Build Docker images before running tests" -Color Gray
    Write-ColorOutput "  -Clean         Clean up Docker containers and images before running" -Color Gray
    Write-ColorOutput "  -Coverage      Generate code coverage reports" -Color Gray
    Write-ColorOutput "  -Verbose       Show detailed output" -Color Gray
    Write-ColorOutput "  -Help          Show this help message" -Color Gray
    Write-ColorOutput ""
    Write-ColorOutput "Examples:" -Color Yellow
    Write-ColorOutput "  .\Run-Tests-Docker.ps1                    # Run all tests" -Color Gray
    Write-ColorOutput "  .\Run-Tests-Docker.ps1 -TestType unit     # Run unit tests only" -Color Gray
    Write-ColorOutput "  .\Run-Tests-Docker.ps1 -Build -Coverage   # Build and run with coverage" -Color Gray
    Write-ColorOutput "  .\Run-Tests-Docker.ps1 -Clean             # Clean and run tests" -Color Gray
}

if ($Help) {
    Show-Help
    exit 0
}

Write-ColorOutput "=== Docker Test Runner for Inventory Control ===" -Color Green

# Set UTF-8 encoding
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# Check if Docker is running
try {
    docker version | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is not running"
    }
} catch {
    Write-ColorOutput "❌ Docker is not running or not installed. Please start Docker Desktop." -Color Red
    exit 1
}

# Clean up if requested
if ($Clean) {
    Write-ColorOutput "🧹 Cleaning up Docker containers and images..." -Color Yellow
    docker-compose -f docker-compose.test.yml down --volumes --remove-orphans
    docker system prune -f
    Write-ColorOutput "✅ Cleanup completed" -Color Green
}

# Create test results directory
if (!(Test-Path "test-results")) {
    New-Item -ItemType Directory -Path "test-results" | Out-Null
    Write-ColorOutput "📁 Created test-results directory" -Color Gray
}

# Build images if requested
if ($Build) {
    Write-ColorOutput "🔨 Building Docker images..." -Color Yellow
    docker-compose -f docker-compose.test.yml build
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Failed to build Docker images" -Color Red
        exit 1
    }
    Write-ColorOutput "✅ Docker images built successfully" -Color Green
}

# Determine which service to run
$serviceName = switch ($TestType.ToLower()) {
    "unit" { "unit-tests" }
    "integration" { "integration-tests" }
    "component" { "component-tests" }
    "all" { "all-tests" }
    default {
        Write-ColorOutput "❌ Invalid test type. Use: all, unit, integration, component" -Color Red
        exit 1
    }
}

Write-ColorOutput "🧪 Running $TestType tests in Docker..." -Color Yellow

try {
    # Start PostgreSQL first
    Write-ColorOutput "🐘 Starting PostgreSQL..." -Color Gray
    docker-compose -f docker-compose.test.yml up -d postgres
    
    # Wait for PostgreSQL to be ready
    Write-ColorOutput "⏳ Waiting for PostgreSQL to be ready..." -Color Gray
    $timeout = 60
    $elapsed = 0
    do {
        Start-Sleep -Seconds 2
        $elapsed += 2
        $health = docker-compose -f docker-compose.test.yml ps postgres
        if ($health -match "healthy") {
            break
        }
        if ($elapsed -ge $timeout) {
            throw "PostgreSQL failed to start within $timeout seconds"
        }
    } while ($true)
    
    Write-ColorOutput "✅ PostgreSQL is ready" -Color Green
    
    # Run tests
    if ($Verbose) {
        docker-compose -f docker-compose.test.yml run --rm $serviceName
    } else {
        docker-compose -f docker-compose.test.yml run --rm $serviceName 2>&1 | Tee-Object -FilePath "test-results/docker-test-output.log"
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ All tests passed!" -Color Green
    } else {
        Write-ColorOutput "❌ Some tests failed!" -Color Red
    }
    
    # Show test results
    if (Test-Path "test-results") {
        Write-ColorOutput "📊 Test results saved to test-results/ directory" -Color Cyan
        Get-ChildItem "test-results" -Recurse | ForEach-Object {
            Write-ColorOutput "  📄 $($_.Name)" -Color Gray
        }
    }
    
} catch {
    Write-ColorOutput "❌ Error running tests: $($_.Exception.Message)" -Color Red
    exit 1
} finally {
    # Clean up containers
    Write-ColorOutput "🧹 Cleaning up containers..." -Color Gray
    docker-compose -f docker-compose.test.yml down --volumes
}

Write-ColorOutput ""
Write-ColorOutput "=== Docker Test Run Complete ===" -Color Green

# Show summary
if (Test-Path "test-results") {
    $trxFiles = Get-ChildItem "test-results" -Recurse -Filter "*.trx"
    $coverageFiles = Get-ChildItem "test-results" -Recurse -Filter "*.xml"
    
    Write-ColorOutput "📈 Summary:" -Color Cyan
    Write-ColorOutput "  Test result files: $($trxFiles.Count)" -Color Gray
    Write-ColorOutput "  Coverage files: $($coverageFiles.Count)" -Color Gray
    
    if ($coverageFiles.Count -gt 0) {
        Write-ColorOutput "📊 Coverage reports generated" -Color Green
    }
}
