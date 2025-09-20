# Test Deployment Script for test.warehouse.cuby
param(
    [string]$Environment = "test"
)

Write-Host "Starting test deployment for test.warehouse.cuby..." -ForegroundColor Green

# Check if VAPID keys are configured (optional for test environment)
$envTestPath = "env.test"
if (Test-Path $envTestPath) {
    $envContent = Get-Content $envTestPath -Raw
    if ($envContent -match "VAPID_PUBLIC_KEY=$" -or $envContent -match "VAPID_PRIVATE_KEY=$") {
        Write-Host "VAPID keys not configured. Generating them for testing..." -ForegroundColor Yellow
        & ".\scripts\generate-vapid-production.ps1" -Environment "test"
    } else {
        Write-Host "VAPID keys are already configured" -ForegroundColor Green
    }
} else {
    Write-Host "env.test file not found. Creating it with VAPID keys for testing..." -ForegroundColor Yellow
    & ".\scripts\generate-vapid-production.ps1" -Environment "test"
}

# Load environment variables
if (Test-Path "env.test") {
    Get-Content "env.test" | ForEach-Object {
        if ($_ -match "^([^#][^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
        }
    }
    Write-Host "Loaded test environment variables" -ForegroundColor Yellow
} else {
    Write-Warning "env.test file not found. Using default values."
}

# Stop existing containers
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.test.yml down

# Build and start services
Write-Host "Building and starting test services..." -ForegroundColor Yellow
docker-compose -f docker-compose.test.yml up -d --build

# Wait for services to be healthy
Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Check health
Write-Host "Checking service health..." -ForegroundColor Yellow
$apiHealth = Invoke-RestMethod -Uri "https://test.warehouse.cuby/health" -Method Get -ErrorAction SilentlyContinue
if ($apiHealth) {
    Write-Host "✅ API is healthy" -ForegroundColor Green
} else {
    Write-Host "❌ API health check failed" -ForegroundColor Red
}

# Show running containers
Write-Host "Running containers:" -ForegroundColor Yellow
docker ps --filter "name=inventory"

Write-Host "Test deployment completed!" -ForegroundColor Green
Write-Host "Application is available at: https://test.warehouse.cuby" -ForegroundColor Cyan
Write-Host "API endpoint: https://test.warehouse.cuby/api" -ForegroundColor Cyan
Write-Host "Health check: https://test.warehouse.cuby/health" -ForegroundColor Cyan
