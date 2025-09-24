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
    [switch]$SkipHealthCheck,

    [Parameter(Mandatory=$false)]
    [int]$HealthCheckTimeout = 30
)

$script:RepoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-ConfigPath {
    param(
        [string]$PathValue
    )

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return $null
    }

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return Join-Path -Path $script:RepoRoot -ChildPath $PathValue
}



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
            $testUrl = "https://$Domain/api/health"
            $null = Invoke-WebRequest -Uri $testUrl -Method Get -TimeoutSec 5 -ErrorAction Stop
            Write-Host "Using domain URL: $testUrl" -ForegroundColor Green
            return $testUrl
        } catch {
            Write-Host "Domain $Domain not resolvable, trying localhost..." -ForegroundColor Yellow
        }

        # Try localhost with HTTP (nginx allows this for staging)
        try {
            $testUrl = "http://localhost/api/health"
            $null = Invoke-WebRequest -Uri $testUrl -Method Get -TimeoutSec 5 -ErrorAction Stop
            Write-Host "Using localhost URL: $testUrl" -ForegroundColor Green
            return $testUrl
        } catch {
            Write-Host "Localhost not accessible, using IP fallback..." -ForegroundColor Yellow
        }

        # Fallback to IP (should be last resort)
        $ipUrl = "http://192.168.139.96/api/health"
        Write-Host "Using IP fallback URL: $ipUrl" -ForegroundColor Yellow
        return $ipUrl
    } else {
        # For production and test, use domain
        return "https://$Domain/api/health"
    }
}

# Function to check service health
function Test-ServiceHealth {
    param(
        [string]$HealthUrl
    )

    Write-Host "Checking service health at: $HealthUrl" -ForegroundColor Yellow
    try {
        $null = Invoke-RestMethod -Uri $HealthUrl -Method Get -ErrorAction Stop
        Write-Host "[OK] API is healthy" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "[FAIL] API health check failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to show deployment summary
function Show-DeploymentSummary {
    param(
        [string]$Domain,
        [string]$DisplayName,
        [string]$HealthUrl,
        [bool]$HealthCheckSkipped = $false
    )

    Write-Host "$DisplayName deployment completed!" -ForegroundColor Green
    Write-Host "Application is available at: https://$Domain" -ForegroundColor Cyan
    Write-Host "API endpoint: https://$Domain/api" -ForegroundColor Cyan
    if ($HealthCheckSkipped) {
        Write-Host "Health check: skipped by parameter" -ForegroundColor Yellow
    } elseif ($HealthUrl) {
        Write-Host "Health check: $HealthUrl" -ForegroundColor Cyan
    } else {
        Write-Host "Health check: https://$Domain/health" -ForegroundColor Cyan
    }
}

# Get configuration for the specified environment
$config = Get-EnvironmentConfig -Env $Environment -CustomEnvFile $EnvFile -CustomComposeFile $ComposeFile -CustomDomain $Domain -BaseDomainName $BaseDomain

$envFilePath = Resolve-ConfigPath -Path $config.EnvFile
$composeFilePath = Resolve-ConfigPath -Path $config.ComposeFile

Write-Host "Starting $($config.DisplayName) deployment for $($config.Domain)..." -ForegroundColor Green

try {
    # Display configuration being used
    Write-Host "Configuration:" -ForegroundColor Cyan
    Write-Host "  Environment: $($config.DisplayName)" -ForegroundColor White
    Write-Host "  Domain: $($config.Domain)" -ForegroundColor White
    $envFileDisplay = if ($envFilePath) { $envFilePath } else { 'None (using docker-compose defaults)' }
    Write-Host "  Env File: $envFileDisplay" -ForegroundColor White
    Write-Host "  Compose File: $composeFilePath" -ForegroundColor White
    Write-Host "  Skip Health Check: $($SkipHealthCheck.IsPresent)" -ForegroundColor White
    Write-Host "  Skip VAPID Check: $($SkipVapidCheck.IsPresent)" -ForegroundColor White
    Write-Host "  Health Check Timeout: $HealthCheckTimeout seconds" -ForegroundColor White
    Write-Host ""

    if ($envFilePath -and -not (Test-Path $envFilePath)) {
        Write-Warning "$($config.EnvFile) file not found. Environment variables will rely on docker-compose defaults."
        $envFilePath = $null
    }

    # Check compose file exists
    if (-not (Test-Path $composeFilePath)) {
        Write-Error "Docker Compose file not found: $($config.ComposeFile)"
        exit 1
    }

    if ($SkipVapidCheck) {
        Write-Host "Skipping VAPID configuration check (Web Push integration removed)." -ForegroundColor Yellow
    } else {
        Write-Host "VAPID configuration check not required (Web Push integration removed)." -ForegroundColor DarkGray
    }

    # Stop existing containers
    Write-Host "Stopping existing containers..." -ForegroundColor Yellow
    docker-compose -f $composeFilePath down
    if ($LASTEXITCODE -ne 0) {
        throw "docker-compose down failed with exit code $LASTEXITCODE"
    }

    # Build and start services
    Write-Host "Building and starting $($config.DisplayName) services..." -ForegroundColor Yellow
    $upArgs = @('-f', $composeFilePath)
    if ($envFilePath) {
        $upArgs += @('--env-file', $envFilePath)
    }
    $upArgs += @('up', '-d', '--build')
    docker-compose @upArgs
    if ($LASTEXITCODE -ne 0) {
        throw "docker-compose up failed with exit code $LASTEXITCODE"
    }

    $healthUrl = $null
    $healthCheckSkipped = $SkipHealthCheck.IsPresent
    $isHealthy = $true

    if ($SkipHealthCheck) {
        Write-Host "Skipping health verification as requested." -ForegroundColor Yellow
    } else {
        Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
        Start-Sleep -Seconds $HealthCheckTimeout
        $healthUrl = Get-HealthCheckUrl -Environment $Environment -Domain $config.Domain
        $isHealthy = Test-ServiceHealth -HealthUrl $healthUrl
    }

    # Show running containers
    Write-Host "Running containers:" -ForegroundColor Yellow
    docker ps --filter "name=inventory"

    # Show deployment summary
    Show-DeploymentSummary -Domain $config.Domain -DisplayName $config.DisplayName -HealthUrl $healthUrl -HealthCheckSkipped $healthCheckSkipped

    if (-not $SkipHealthCheck -and -not $isHealthy) {
        Write-Warning "Deployment completed but health check failed. Please check the logs."
        exit 1
    }

} catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    exit 1
}
