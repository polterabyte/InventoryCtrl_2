# Quick Deploy Script for Inventory Control System
param(
    [string]$Environment = "development",
    [switch]$GenerateSSL = $false,
    [switch]$Clean = $false
)

Write-Host "🚀 Quick Deploy - Inventory Control System" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Check if Docker is running
    Write-Host "🐳 Checking Docker..." -ForegroundColor Yellow
    docker version | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is not running. Please start Docker Desktop."
    }

    # Clean up if requested
    if ($Clean) {
        Write-Host "🧹 Cleaning up existing containers..." -ForegroundColor Yellow
        docker-compose down -v --remove-orphans 2>$null
        docker system prune -f
    }

    # Generate SSL certificates if requested
    if ($GenerateSSL) {
        Write-Host "🔐 Generating SSL certificates..." -ForegroundColor Yellow
        & .\scripts\generate-ssl.ps1
    }

    # Build and run
    Write-Host "🔨 Building and starting services..." -ForegroundColor Yellow
    & .\docker-run.ps1 -Environment $Environment -Build

    # Wait for services to be ready
    Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10

    # Check service health
    Write-Host "🏥 Checking service health..." -ForegroundColor Yellow
    
    $apiHealthy = $false
    $webHealthy = $false
    
    try {
        $apiResponse = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 10 -ErrorAction Stop
        if ($apiResponse.StatusCode -eq 200) {
            $apiHealthy = $true
            Write-Host "✅ API is healthy" -ForegroundColor Green
        }
    } catch {
        Write-Host "❌ API health check failed" -ForegroundColor Red
    }

    try {
        $webResponse = Invoke-WebRequest -Uri "http://localhost/health" -TimeoutSec 10 -ErrorAction Stop
        if ($webResponse.StatusCode -eq 200) {
            $webHealthy = $true
            Write-Host "✅ Web Client is healthy" -ForegroundColor Green
        }
    } catch {
        Write-Host "❌ Web Client health check failed" -ForegroundColor Red
    }

    # Show final status
    Write-Host "`n🎉 Deployment completed!" -ForegroundColor Green
    Write-Host "=========================" -ForegroundColor Green
    
    Write-Host "`n🌐 Access URLs:" -ForegroundColor Cyan
    Write-Host "   Web Application: http://localhost" -ForegroundColor White
    Write-Host "   API: http://localhost:5000" -ForegroundColor White
    Write-Host "   API Swagger: http://localhost:5000/swagger" -ForegroundColor White
    
    if ($Environment -eq "production") {
        Write-Host "   HTTPS: https://localhost" -ForegroundColor White
    }

    Write-Host "`n📊 Service Status:" -ForegroundColor Cyan
    docker ps --filter "name=inventory-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

    Write-Host "`n📋 Useful Commands:" -ForegroundColor Cyan
    Write-Host "   View logs: docker-compose logs -f" -ForegroundColor White
    Write-Host "   Stop services: docker-compose down" -ForegroundColor White
    Write-Host "   Restart API: docker-compose restart inventory-api" -ForegroundColor White

    if (-not $apiHealthy -or -not $webHealthy) {
        Write-Host "`n⚠️  Some services may still be starting up. Check logs for details." -ForegroundColor Yellow
    }

} catch {
    Write-Host "❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`n🔍 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "   1. Check if Docker is running" -ForegroundColor White
    Write-Host "   2. Check if ports 80, 5000, 5432 are available" -ForegroundColor White
    Write-Host "   3. Check logs: docker-compose logs" -ForegroundColor White
    Write-Host "   4. Try cleaning up: .\quick-deploy.ps1 -Clean" -ForegroundColor White
    exit 1
}