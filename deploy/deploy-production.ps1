# Production Deployment Script for warehouse.cuby
# This script is a wrapper around the universal deploy.ps1 script

param(
    [string]$Environment = "production",
    [string]$EnvFile,
    [string]$ComposeFile,
    [string]$Domain,
    [string]$BaseDomain,
    [switch]$SkipVapidCheck,
    [switch]$SkipHealthCheck,
    [int]$HealthCheckTimeout
)

# Call the universal deployment script with all parameters
$params = @{
    Environment = "production"
}

if ($EnvFile) { $params.EnvFile = $EnvFile }
if ($ComposeFile) { $params.ComposeFile = $ComposeFile }
if ($Domain) { $params.Domain = $Domain }
if ($BaseDomain) { $params.BaseDomain = $BaseDomain }
if ($SkipVapidCheck) { $params.SkipVapidCheck = $true }
if ($SkipHealthCheck) { $params.SkipHealthCheck = $true }
if ($HealthCheckTimeout) { $params.HealthCheckTimeout = $HealthCheckTimeout }

$deployScript = Join-Path $PSScriptRoot 'deploy.ps1'

if (-not (Test-Path $deployScript)) {
    throw "Universal deployment script not found at $deployScript"
}

& $deployScript @params
