# Test Deployment Script for test.warehouse.cuby
# This script is a wrapper around the universal deploy.ps1 script

param(
    [string]$Environment = "test",
    [string]$EnvFile,
    [string]$ComposeFile,
    [string]$Domain,
    [string]$BaseDomain,
    [switch]$SkipVapidCheck,
    [int]$HealthCheckTimeout
)

# Call the universal deployment script with all parameters
$params = @{
    Environment = "test"
}

if ($EnvFile) { $params.EnvFile = $EnvFile }
if ($ComposeFile) { $params.ComposeFile = $ComposeFile }
if ($Domain) { $params.Domain = $Domain }
if ($BaseDomain) { $params.BaseDomain = $BaseDomain }
if ($SkipVapidCheck) { $params.SkipVapidCheck = $true }
if ($HealthCheckTimeout) { $params.HealthCheckTimeout = $HealthCheckTimeout }

& ".\deploy.ps1" @params
