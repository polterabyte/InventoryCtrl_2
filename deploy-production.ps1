# Production Deployment Script for warehouse.cuby
param(
    [string]$Environment = "production"
)

Write-Host "Starting production deployment for warehouse.cuby..." -ForegroundColor Green

# Load environment variables
if (Test-Path "env.production") {
    Get-Content "env.production" | ForEach-Object {
        if ($_ -match "^([^#][^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
        }
    }
    Write-Host "Loaded production environment variables" -ForegroundColor Yellow
} else {
    Write-Warning "env.production file not found. Using default values."
}

# Stop existing containers
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.prod.yml down

# Build and start services
Write-Host "Building and starting production services..." -ForegroundColor Yellow
docker-compose -f docker-compose.prod.yml up -d --build

# Wait for services to be healthy
Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Check health
Write-Host "Checking service health..." -ForegroundColor Yellow
$apiHealth = Invoke-RestMethod -Uri "https://warehouse.cuby/health" -Method Get -ErrorAction SilentlyContinue
if ($apiHealth) {
    Write-Host "✅ API is healthy" -ForegroundColor Green
} else {
    Write-Host "❌ API health check failed" -ForegroundColor Red
}

# Show running containers
Write-Host "Running containers:" -ForegroundColor Yellow
docker ps --filter "name=inventory"

Write-Host "Production deployment completed!" -ForegroundColor Green
Write-Host "Application is available at: https://warehouse.cuby" -ForegroundColor Cyan
Write-Host "API endpoint: https://warehouse.cuby/api" -ForegroundColor Cyan
Write-Host "Health check: https://warehouse.cuby/health" -ForegroundColor Cyan
