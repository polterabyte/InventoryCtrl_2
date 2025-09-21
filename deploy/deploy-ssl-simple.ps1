# Deploy Inventory Control System with SSL Support
param(
    [string]$Environment = "development",
    [switch]$Verbose = $false
)

Write-Host "Deploying Inventory Control System with SSL Support" -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Green

$ErrorActionPreference = "Stop"

function Write-VerboseOutput {
    param([string]$Message)
    if ($Verbose) {
        Write-Host "   [VERBOSE] $Message" -ForegroundColor DarkGray
    }
}

try {
    # Check Docker
    Write-Host "Checking Docker..." -ForegroundColor Yellow
    docker version | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is not running"
    }
    Write-Host "   Docker is running" -ForegroundColor Green

    # Check required files
    Write-Host "Checking required files..." -ForegroundColor Yellow
    $files = @("docker-compose.ssl.yml", "scripts\generate-ssl-linux.sh", "src\Inventory.API\Dockerfile.ssl")
    foreach ($file in $files) {
        if (Test-Path $file) {
            Write-Host "   $file exists" -ForegroundColor Green
        } else {
            Write-Host "   $file missing" -ForegroundColor Red
            throw "Required file missing: $file"
        }
    }

    # Clean up if needed
    Write-Host "Cleaning up existing containers..." -ForegroundColor Yellow
    docker-compose -f docker-compose.ssl.yml down -v --remove-orphans 2>$null
    docker system prune -f

    # Prepare SSL script
    Write-Host "Preparing SSL script..." -ForegroundColor Yellow
    if (Test-Path "scripts\generate-ssl-linux.sh") {
        $content = Get-Content "scripts\generate-ssl-linux.sh" -Raw -Encoding UTF8
        $content = $content -replace "`r`n", "`n"
        Set-Content "scripts\generate-ssl-linux.sh" -Value $content -NoNewline -Encoding UTF8
        Write-Host "   SSL script prepared" -ForegroundColor Green
    }

    # Set environment variables
    $env:SSL_ENVIRONMENT = $Environment
    $env:SSL_KEY_SIZE = "4096"
    $env:SSL_VALIDITY_DAYS = "365"
    $env:SSL_LETS_ENCRYPT_ENABLED = "false"
    $env:SSL_LETS_ENCRYPT_EMAIL = "admin@warehouse.cuby"
    $env:SSL_LETS_ENCRYPT_STAGING = "false"

    # Build and start services
    Write-Host "Building and starting services..." -ForegroundColor Yellow
    docker-compose -f docker-compose.ssl.yml up -d --build
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose failed"
    }
    Write-Host "   Services started" -ForegroundColor Green

    # Wait for services
    Write-Host "Waiting for services to start..." -ForegroundColor Yellow
    Start-Sleep -Seconds 15

    # Check service health
    Write-Host "Checking service health..." -ForegroundColor Yellow
    
    try {
        $apiResponse = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 10 -ErrorAction Stop
        if ($apiResponse.StatusCode -eq 200) {
            Write-Host "   API is healthy" -ForegroundColor Green
        } else {
            Write-Host "   API responded with status $($apiResponse.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   API health check failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    try {
        $webResponse = Invoke-WebRequest -Uri "http://localhost/health" -TimeoutSec 10 -ErrorAction Stop
        if ($webResponse.StatusCode -eq 200) {
            Write-Host "   Web Client is healthy" -ForegroundColor Green
        } else {
            Write-Host "   Web Client responded with status $($webResponse.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   Web Client health check failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test SSL
    Write-Host "Testing SSL endpoints..." -ForegroundColor Yellow
    try {
        $sslResponse = Invoke-WebRequest -Uri "https://localhost" -TimeoutSec 10 -SkipCertificateCheck -ErrorAction Stop
        Write-Host "   HTTPS endpoint is working" -ForegroundColor Green
    } catch {
        Write-Host "   HTTPS endpoint not accessible (normal for self-signed certificates)" -ForegroundColor Yellow
    }

    # Show results
    Write-Host "`nDeployment completed!" -ForegroundColor Green
    Write-Host "====================" -ForegroundColor Green
    
    Write-Host "`nAccess URLs:" -ForegroundColor Cyan
    Write-Host "   Web Application (HTTP): http://localhost" -ForegroundColor White
    Write-Host "   Web Application (HTTPS): https://localhost" -ForegroundColor White
    Write-Host "   API (HTTP): http://localhost:5000" -ForegroundColor White
    Write-Host "   API Swagger: http://localhost:5000/swagger" -ForegroundColor White

    Write-Host "`nService Status:" -ForegroundColor Cyan
    docker ps --filter "name=inventory-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

    Write-Host "`nUseful Commands:" -ForegroundColor Cyan
    Write-Host "   View logs: docker-compose -f docker-compose.ssl.yml logs -f" -ForegroundColor White
    Write-Host "   Stop services: docker-compose -f docker-compose.ssl.yml down" -ForegroundColor White
    Write-Host "   Restart API: docker-compose -f docker-compose.ssl.yml restart inventory-api" -ForegroundColor White

} catch {
    Write-Host "`nDeployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "===================" -ForegroundColor Red
    
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "   1. Check if Docker is running: docker version" -ForegroundColor White
    Write-Host "   2. Check ports: netstat -an | findstr ':80 :443 :5000 :5432'" -ForegroundColor White
    Write-Host "   3. Check logs: docker-compose -f docker-compose.ssl.yml logs" -ForegroundColor White
    Write-Host "   4. Try cleanup: .\deploy\deploy-ssl-simple.ps1 -Clean" -ForegroundColor White
    
    exit 1
}
