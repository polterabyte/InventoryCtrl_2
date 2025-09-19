# Docker Run Script for Inventory Control System
param(
    [string]$Environment = "development",
    [switch]$Detached = $true,
    [switch]$Build = $false,
    [switch]$Clean = $false
)

Write-Host " Starting Inventory Control System with Docker..." -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Clean up if requested
    if ($Clean) {
        Write-Host " Cleaning up existing containers and volumes..." -ForegroundColor Yellow
        docker-compose down -v --remove-orphans
        docker system prune -f
    }

    # Build if requested
    if ($Build) {
        Write-Host " Building images..." -ForegroundColor Yellow
        & .\docker-build.ps1 -Environment $Environment
    }

    # Determine compose file
    $composeFile = "docker-compose.yml"
    if ($Environment -eq "production") {
        $composeFile = "docker-compose.prod.yml"
    }

    # Run with docker-compose
    $detachedFlag = if ($Detached) { "-d" } else { "" }
    
    Write-Host " Starting services with $composeFile..." -ForegroundColor Yellow
    docker-compose -f $composeFile up $detachedFlag

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start services"
    }

    # Show running containers
    Write-Host "`n Running Containers:" -ForegroundColor Cyan
    docker ps --filter "name=inventory-"

    # Show access URLs
    Write-Host "`n Access URLs:" -ForegroundColor Green
    Write-Host "   Web Application: http://localhost" -ForegroundColor White
    Write-Host "   API: http://localhost:5000" -ForegroundColor White
    Write-Host "   API Swagger: http://localhost:5000/swagger" -ForegroundColor White
    
    if ($Environment -eq "production") {
        Write-Host "   HTTPS: https://localhost" -ForegroundColor White
    }

    Write-Host "`n To view logs: docker-compose logs -f" -ForegroundColor Cyan
    Write-Host " To stop: docker-compose down" -ForegroundColor Cyan

} catch {
    Write-Host " Failed to start services: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
