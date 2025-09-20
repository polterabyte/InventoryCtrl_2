# Custom Deployment Script
# This script allows you to deploy with custom file names and domains

param(
    [Parameter(Mandatory=$true)]
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
    [int]$HealthCheckTimeout = 30,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun
)

# Function to generate default file names based on environment
function Get-DefaultFileNames {
    param(
        [string]$Env,
        [string]$BaseDomainName
    )
    
    # Generate domain name based on environment
    $domain = if ($Env -eq "production") { 
        $BaseDomainName 
    } else { 
        "$Env.$BaseDomainName" 
    }
    
    # Generate standardized file names
    $envFile = "env.$Env"
    $composeFile = "docker-compose.$Env.yml"
    
    return @{
        Domain = $domain
        EnvFile = $envFile
        ComposeFile = $composeFile
    }
}

# Validate environment parameter
$validEnvironments = @("staging", "production", "test")
if ($Environment -notin $validEnvironments) {
    Write-Error "Invalid environment: $Environment. Valid options are: $($validEnvironments -join ', ')"
    exit 1
}

# Get default configuration
$defaults = Get-DefaultFileNames -Env $Environment -BaseDomainName $BaseDomain

# Use defaults if not specified
if (-not $EnvFile) { $EnvFile = $defaults.EnvFile }
if (-not $ComposeFile) { $ComposeFile = $defaults.ComposeFile }
if (-not $Domain) { $Domain = $defaults.Domain }

# Display configuration
Write-Host "Custom Deployment Configuration:" -ForegroundColor Cyan
Write-Host "  Environment: $Environment" -ForegroundColor White
Write-Host "  Domain: $Domain" -ForegroundColor White
Write-Host "  Env File: $EnvFile" -ForegroundColor White
Write-Host "  Compose File: $ComposeFile" -ForegroundColor White
Write-Host "  Base Domain: $BaseDomain" -ForegroundColor White
Write-Host "  Skip VAPID Check: $SkipVapidCheck" -ForegroundColor White
Write-Host "  Health Check Timeout: $HealthCheckTimeout seconds" -ForegroundColor White
Write-Host "  Dry Run: $DryRun" -ForegroundColor White
Write-Host ""

# Check if files exist
if (-not (Test-Path $EnvFile)) {
    Write-Warning "Environment file not found: $EnvFile"
}

if (-not (Test-Path $ComposeFile)) {
    Write-Error "Docker Compose file not found: $ComposeFile"
    exit 1
}

if ($DryRun) {
    Write-Host "Dry run mode - would execute:" -ForegroundColor Yellow
    Write-Host "  ..\deploy.ps1 -Environment $Environment -EnvFile `"$EnvFile`" -ComposeFile `"$ComposeFile`" -Domain `"$Domain`" -BaseDomain `"$BaseDomain`"" -ForegroundColor White
    if ($SkipVapidCheck) {
        Write-Host "    -SkipVapidCheck" -ForegroundColor White
    }
    Write-Host "    -HealthCheckTimeout $HealthCheckTimeout" -ForegroundColor White
    Write-Host "`nDry run completed. No changes made." -ForegroundColor Green
    exit 0
}

# Execute deployment
Write-Host "Executing custom deployment..." -ForegroundColor Green

$params = @{
    Environment = $Environment
    EnvFile = $EnvFile
    ComposeFile = $ComposeFile
    Domain = $Domain
    BaseDomain = $BaseDomain
    HealthCheckTimeout = $HealthCheckTimeout
}

if ($SkipVapidCheck) { $params.SkipVapidCheck = $true }

& "..\deploy.ps1" @params
