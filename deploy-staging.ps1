# Staging Deployment Script for staging.warehouse.cuby
param(
    [string]$Environment = "staging"
)

Write-Host "Starting staging deployment for staging.warehouse.cuby..." -ForegroundColor Green

# Load environment variables
if (Test-Path "env.staging") {
    Get-Content "env.staging" | ForEach-Object {
        if ($_ -match "^([^#][^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
        }
    }
    Write-Host "Loaded staging environment variables" -ForegroundColor Yellow
} else {
    Write-Warning "env.staging file not found. Using default values."
}

# Stop existing containers
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.staging.yml down

# Build and start services
Write-Host "Building and starting staging services..." -ForegroundColor Yellow
docker-compose -f docker-compose.staging.yml up -d --build

# Wait for services to be healthy
Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Check health
Write-Host "Checking service health..." -ForegroundColor Yellow
$apiHealth = Invoke-RestMethod -Uri "https://staging.warehouse.cuby/health" -Method Get -ErrorAction SilentlyContinue
if ($apiHealth) {
    Write-Host "✅ API is healthy" -ForegroundColor Green
} else {
    Write-Host "❌ API health check failed" -ForegroundColor Red
}

# Show running containers
Write-Host "Running containers:" -ForegroundColor Yellow
docker ps --filter "name=inventory"

Write-Host "Staging deployment completed!" -ForegroundColor Green
Write-Host "Application is available at: https://staging.warehouse.cuby" -ForegroundColor Cyan
Write-Host "API endpoint: https://staging.warehouse.cuby/api" -ForegroundColor Cyan
Write-Host "Health check: https://staging.warehouse.cuby/health" -ForegroundColor Cyan
