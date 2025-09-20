# Universal Deployment Script for Inventory Control System
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("staging", "production", "test")]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$EnvFile,
    
    [Parameter(Mandatory=$false)]
    [string]$ComposeFile,
    
    [Parameter(Mandatory=$false)]
    [string]$Domain,
    
    [Parameter(Mandatory=$false)]
    [string]$BaseDomain = "warehouse.cuby",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipVapidCheck,
    
    [Parameter(Mandatory=$false)]
    [int]$HealthCheckTimeout = 30
)

# Function to generate standardized file names
function Get-StandardizedFileNames {
    param(
        [string]$Environment,
        [string]$BaseDomainName
    )
    
    # Generate domain name based on environment
    $domain = if ($Environment -eq "production") { 
        $BaseDomainName 
    } else { 
        "$Environment.$BaseDomainName" 
    }
    
    # Generate standardized file names
    $envFile = "deploy/env.$Environment"
    $composeFile = "docker-compose.$Environment.yml"
    
    return @{
        Domain = $domain
        EnvFile = $envFile
        ComposeFile = $composeFile
        DisplayName = $Environment
    }
}

# Function to get environment configuration
function Get-EnvironmentConfig {
    param(
        [string]$Env,
        [string]$CustomEnvFile,
        [string]$CustomComposeFile,
        [string]$CustomDomain,
        [string]$BaseDomainName
    )
    
    # Get standardized configuration
    $config = Get-StandardizedFileNames -Environment $Env -BaseDomainName $BaseDomainName
    
    # Override with custom values if provided
    if ($CustomEnvFile) { $config.EnvFile = $CustomEnvFile }
    if ($CustomComposeFile) { $config.ComposeFile = $CustomComposeFile }
    if ($CustomDomain) { $config.Domain = $CustomDomain }
    
    return $config
}

# Get configuration for the specified environment
$config = Get-EnvironmentConfig -Env $Environment -CustomEnvFile $EnvFile -CustomComposeFile $ComposeFile -CustomDomain $Domain -BaseDomainName $BaseDomain

Write-Host "Starting $($config.DisplayName) deployment for $($config.Domain)..." -ForegroundColor Green

# Function to check and generate VAPID keys
function Test-AndGenerateVapidKeys {
    param(
        [string]$EnvFilePath,
        [string]$EnvironmentName,
        [bool]$SkipCheck
    )
    
    if ($SkipCheck) {
        Write-Host "Skipping VAPID keys check as requested" -ForegroundColor Yellow
        return
    }
    
    if (Test-Path $EnvFilePath) {
        $envContent = Get-Content $EnvFilePath -Raw
        if ($envContent -match "VAPID_PUBLIC_KEY=$" -or $envContent -match "VAPID_PRIVATE_KEY=$") {
            Write-Host "VAPID keys not configured. Generating them..." -ForegroundColor Yellow
            & ".\scripts\generate-vapid-production.ps1" -Environment $EnvironmentName
        } else {
            Write-Host "VAPID keys are already configured" -ForegroundColor Green
        }
    } else {
        if ($EnvironmentName -eq "test") {
            Write-Host "$EnvFilePath file not found. Creating it with VAPID keys for testing..." -ForegroundColor Yellow
        } else {
            Write-Warning "$EnvFilePath file not found. Creating it with VAPID keys..."
        }
        & ".\scripts\generate-vapid-production.ps1" -Environment $EnvironmentName
    }
}

# Function to determine health check URL
function Get-HealthCheckUrl {
    param(
        [string]$Environment,
        [string]$Domain
    )
    
    # For staging environment, try multiple approaches
    if ($Environment -eq "staging") {
        # Try domain first (if DNS is configured)
        try {
            $testUrl = "https://$Domain/health"
            $response = Invoke-WebRequest -Uri $testUrl -Method Get -TimeoutSec 5 -ErrorAction Stop
            Write-Host "Using domain URL: $testUrl" -ForegroundColor Green
            return $testUrl
        } catch {
            Write-Host "Domain $Domain not resolvable, trying localhost..." -ForegroundColor Yellow
        }
        
        # Try localhost with HTTP (nginx allows this for staging)
        try {
            $testUrl = "http://localhost/health"
            $response = Invoke-WebRequest -Uri $testUrl -Method Get -TimeoutSec 5 -ErrorAction Stop
            Write-Host "Using localhost URL: $testUrl" -ForegroundColor Green
            return $testUrl
        } catch {
            Write-Host "Localhost not accessible, using IP fallback..." -ForegroundColor Yellow
        }
        
        # Fallback to IP (should be last resort)
        $ipUrl = "http://192.168.139.96/health"
        Write-Host "Using IP fallback URL: $ipUrl" -ForegroundColor Yellow
        return $ipUrl
    } else {
        # For production and test, use domain
        return "https://$Domain/health"
    }
}

# Function to check service health
function Test-ServiceHealth {
    param(
        [string]$HealthUrl
    )
    
    Write-Host "Checking service health at: $HealthUrl" -ForegroundColor Yellow
    try {
        $apiHealth = Invoke-RestMethod -Uri $HealthUrl -Method Get -ErrorAction Stop
        Write-Host "✅ API is healthy" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "❌ API health check failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to show deployment summary
function Show-DeploymentSummary {
    param(
        [string]$Domain,
        [string]$DisplayName
    )
    
    Write-Host "$DisplayName deployment completed!" -ForegroundColor Green
    Write-Host "Application is available at: https://$Domain" -ForegroundColor Cyan
    Write-Host "API endpoint: https://$Domain/api" -ForegroundColor Cyan
    Write-Host "Health check: https://$Domain/health" -ForegroundColor Cyan
}

# Main deployment logic
try {
    # Display configuration being used
    Write-Host "Configuration:" -ForegroundColor Cyan
    Write-Host "  Environment: $($config.DisplayName)" -ForegroundColor White
    Write-Host "  Domain: $($config.Domain)" -ForegroundColor White
    Write-Host "  Env File: $($config.EnvFile)" -ForegroundColor White
    Write-Host "  Compose File: $($config.ComposeFile)" -ForegroundColor White
    Write-Host "  Health Check Timeout: $HealthCheckTimeout seconds" -ForegroundColor White
    Write-Host ""

    # Check and generate VAPID keys
    Test-AndGenerateVapidKeys -EnvFilePath $config.EnvFile -EnvironmentName $Environment -SkipCheck $SkipVapidCheck

    # Check environment file exists
    if (-not (Test-Path $config.EnvFile)) {
        Write-Warning "$($config.EnvFile) file not found. Using default values."
    }

    # Check compose file exists
    if (-not (Test-Path $config.ComposeFile)) {
        Write-Error "Docker Compose file not found: $($config.ComposeFile)"
        exit 1
    }

    # Stop existing containers
    Write-Host "Stopping existing containers..." -ForegroundColor Yellow
    docker-compose -f $config.ComposeFile down

    # Build and start services
    Write-Host "Building and starting $($config.DisplayName) services..." -ForegroundColor Yellow
    docker-compose -f $config.ComposeFile --env-file $config.EnvFile up -d --build

    # Wait for services to be healthy
    Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
    Start-Sleep -Seconds $HealthCheckTimeout

    # Check health
    $healthUrl = Get-HealthCheckUrl -Environment $Environment -Domain $config.Domain
    $isHealthy = Test-ServiceHealth -HealthUrl $healthUrl

    # Show running containers
    Write-Host "Running containers:" -ForegroundColor Yellow
    docker ps --filter "name=inventory"

    # Show deployment summary
    Show-DeploymentSummary -Domain $config.Domain -DisplayName $config.DisplayName

    if (-not $isHealthy) {
        Write-Warning "Deployment completed but health check failed. Please check the logs."
        exit 1
    }

} catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    exit 1
}
