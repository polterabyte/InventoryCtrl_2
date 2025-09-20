# Deploy All Environments Script
# This script allows you to deploy all environments or specific ones

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("staging", "production", "test", "all")]
    [string]$Environment = "all",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipHealthCheck,
    
    [Parameter(Mandatory=$false)]
    [string]$BaseDomain = "warehouse.cuby",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipVapidCheck,
    
    [Parameter(Mandatory=$false)]
    [int]$HealthCheckTimeout = 30
)

# Function to deploy a specific environment
function Deploy-Environment {
    param(
        [string]$EnvName,
        [bool]$SkipHealth,
        [string]$BaseDomainName,
        [bool]$SkipVapid,
        [int]$HealthTimeout
    )
    
    Write-Host "`n=== Deploying $EnvName environment ===" -ForegroundColor Cyan
    try {
        $params = @{
            Environment = $EnvName
            BaseDomain = $BaseDomainName
            HealthCheckTimeout = $HealthTimeout
        }
        
        if ($SkipVapid) { $params.SkipVapidCheck = $true }
        
        & "..\deploy.ps1" @params
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to deploy $EnvName environment"
            return $false
        }
        Write-Host "✅ $EnvName environment deployed successfully" -ForegroundColor Green
        return $true
    } catch {
        Write-Error "Error deploying $EnvName environment: $($_.Exception.Message)"
        return $false
    }
}

# Main execution logic
Write-Host "Starting deployment process..." -ForegroundColor Green

$deploymentResults = @{}

if ($Environment -eq "all") {
    # Deploy all environments in order: test -> staging -> production
    $environments = @("test", "staging", "production")
    
    foreach ($env in $environments) {
        $deploymentResults[$env] = Deploy-Environment -EnvName $env -SkipHealth $SkipHealthCheck -BaseDomainName $BaseDomain -SkipVapid $SkipVapidCheck -HealthTimeout $HealthCheckTimeout
    }
} else {
    # Deploy specific environment
    $deploymentResults[$Environment] = Deploy-Environment -EnvName $Environment -SkipHealth $SkipHealthCheck -BaseDomainName $BaseDomain -SkipVapid $SkipVapidCheck -HealthTimeout $HealthCheckTimeout
}

# Summary
Write-Host "`n=== Deployment Summary ===" -ForegroundColor Yellow
foreach ($result in $deploymentResults.GetEnumerator()) {
    $status = if ($result.Value) { "✅ SUCCESS" } else { "❌ FAILED" }
    Write-Host "$($result.Key): $status" -ForegroundColor $(if ($result.Value) { "Green" } else { "Red" })
}

# Check if any deployment failed
$failedDeployments = $deploymentResults.Values | Where-Object { $_ -eq $false }
if ($failedDeployments.Count -gt 0) {
    Write-Warning "Some deployments failed. Please check the logs above."
    exit 1
} else {
    Write-Host "`n🎉 All deployments completed successfully!" -ForegroundColor Green
}
