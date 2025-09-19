# Docker Build Script for Inventory Control System
param(
    [string]$Environment = "development",
    [switch]$NoCache = $false,
    [switch]$Push = $false
)

Write-Host "Building Inventory Control System with Docker..." -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Build arguments
    $buildArgs = @()
    if ($NoCache) {
        $buildArgs += "--no-cache"
    }

    # Build API
    Write-Host "Building Inventory API..." -ForegroundColor Yellow
    docker build -t inventory-api:latest $buildArgs -f src/Inventory.API/Dockerfile .
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build API image"
    }

    # Build Web Client
    Write-Host "Building Inventory Web Client..." -ForegroundColor Yellow
    docker build -t inventory-web:latest $buildArgs -f src/Inventory.Web.Client/Dockerfile .
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build Web Client image"
    }

    # Build Nginx Proxy (for production)
    if ($Environment -eq "production") {
        Write-Host "Building Nginx Proxy..." -ForegroundColor Yellow
        docker build -t inventory-nginx:latest $buildArgs -f nginx/Dockerfile .
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to build Nginx image"
        }
    }

    Write-Host "All images built successfully!" -ForegroundColor Green

    # Show built images
    Write-Host "`nBuilt Images:" -ForegroundColor Cyan
    docker images | Select-String "inventory-"

    if ($Push) {
        Write-Host "`nPushing images to registry..." -ForegroundColor Yellow
        # Add your registry push commands here
        # docker push your-registry/inventory-api:latest
        # docker push your-registry/inventory-web:latest
    }

} catch {
    Write-Host "Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
