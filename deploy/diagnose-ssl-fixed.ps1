# SSL Deployment Diagnostics Script
param(
    [switch]$Verbose = $false
)

Write-Host "SSL Deployment Diagnostics" -ForegroundColor Green
Write-Host "=============================" -ForegroundColor Green

# Function to write verbose output
function Write-VerboseOutput {
    param([string]$Message)
    if ($Verbose) {
        Write-Host "   [VERBOSE] $Message" -ForegroundColor DarkGray
    }
}

# Function to check and report status
function Test-AndReport {
    param(
        [string]$Description,
        [scriptblock]$TestScript,
        [string]$SuccessMessage = "OK",
        [string]$FailureMessage = "FAILED"
    )
    
    try {
        $result = & $TestScript
        if ($result) {
            Write-Host "   $SuccessMessage $Description" -ForegroundColor Green
            Write-VerboseOutput "${Description}: $result"
            return $true
        } else {
            Write-Host "   $FailureMessage $Description" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "   $FailureMessage $Description - Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-VerboseOutput "${Description} error: $($_.Exception.Message)"
        return $false
    }
}

$issues = @()

Write-Host "`nDocker Environment:" -ForegroundColor Yellow
$dockerRunning = Test-AndReport "Docker is running" { 
    docker version | Out-Null; $LASTEXITCODE -eq 0 
}
if (-not $dockerRunning) { $issues += "Docker not running" }

$dockerCompose = Test-AndReport "Docker Compose available" { 
    docker-compose --version | Out-Null; $LASTEXITCODE -eq 0 
}
if (-not $dockerCompose) { $issues += "Docker Compose not available" }

Write-Host "`nRequired Files:" -ForegroundColor Yellow
$dockerComposeFile = Test-AndReport "docker-compose.ssl.yml exists" { 
    Test-Path "docker-compose.ssl.yml" 
}
if (-not $dockerComposeFile) { $issues += "docker-compose.ssl.yml missing" }

$sslScript = Test-AndReport "generate-ssl-linux.sh exists" { 
    Test-Path "scripts\generate-ssl-linux.sh" 
}
if (-not $sslScript) { $issues += "generate-ssl-linux.sh missing" }

$sslDockerfile = Test-AndReport "Dockerfile.ssl exists" { 
    Test-Path "src\Inventory.API\Dockerfile.ssl" 
}
if (-not $sslDockerfile) { $issues += "Dockerfile.ssl missing" }

Write-Host "`nNetwork Ports:" -ForegroundColor Yellow
$port80 = Test-AndReport "Port 80 available" { 
    -not (Get-NetTCPConnection -LocalPort 80 -ErrorAction SilentlyContinue) 
}
if (-not $port80) { $issues += "Port 80 in use" }

$port443 = Test-AndReport "Port 443 available" { 
    -not (Get-NetTCPConnection -LocalPort 443 -ErrorAction SilentlyContinue) 
}
if (-not $port443) { $issues += "Port 443 in use" }

$port5000 = Test-AndReport "Port 5000 available" { 
    -not (Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue) 
}
if (-not $port5000) { $issues += "Port 5000 in use" }

$port5432 = Test-AndReport "Port 5432 available" { 
    -not (Get-NetTCPConnection -LocalPort 5432 -ErrorAction SilentlyContinue) 
}
if (-not $port5432) { $issues += "Port 5432 in use" }

Write-Host "`nDocker Resources:" -ForegroundColor Yellow
try {
    $dockerInfo = docker system df --format "table {{.Type}}\t{{.TotalCount}}\t{{.Size}}" 2>$null
    if ($dockerInfo) {
        Write-Host "   OK Docker system info available" -ForegroundColor Green
        Write-VerboseOutput "Docker system info: $dockerInfo"
    } else {
        Write-Host "   Could not get Docker system info" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Could not check Docker resources" -ForegroundColor Yellow
}

Write-Host "`nEnvironment Variables:" -ForegroundColor Yellow
$envVars = @("DOTNET_RUNNING_IN_CONTAINER", "SSL_ENVIRONMENT", "SSL_LETS_ENCRYPT_ENABLED")
foreach ($var in $envVars) {
    $value = [Environment]::GetEnvironmentVariable($var)
    if ($value) {
        Write-Host "   OK $var = $value" -ForegroundColor Green
    } else {
        Write-Host "   $var not set" -ForegroundColor Yellow
    }
}

Write-Host "`nExisting Containers:" -ForegroundColor Yellow
try {
    $containers = docker ps -a --filter "name=inventory-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>$null
    if ($containers) {
        Write-Host "   Found existing inventory containers:" -ForegroundColor Yellow
        Write-Host $containers -ForegroundColor Gray
    } else {
        Write-Host "   OK No existing inventory containers" -ForegroundColor Green
    }
} catch {
    Write-Host "   Could not check existing containers" -ForegroundColor Yellow
}

Write-Host "`nDocker Volumes:" -ForegroundColor Yellow
try {
    $volumes = docker volume ls --filter "name=inventory" --format "table {{.Name}}\t{{.Driver}}" 2>$null
    if ($volumes) {
        Write-Host "   Found inventory volumes:" -ForegroundColor Yellow
        Write-Host $volumes -ForegroundColor Gray
    } else {
        Write-Host "   OK No existing inventory volumes" -ForegroundColor Green
    }
} catch {
    Write-Host "   Could not check volumes" -ForegroundColor Yellow
}

Write-Host "`nSSL Script Analysis:" -ForegroundColor Yellow
if (Test-Path "scripts\generate-ssl-linux.sh") {
    try {
        $scriptContent = Get-Content "scripts\generate-ssl-linux.sh" -Raw
        $hasShebang = $scriptContent -match "^#!/bin/bash"
        $hasOpenSSL = $scriptContent -match "openssl"
        $hasFunctions = $scriptContent -match "function|print_"
        
        if ($hasShebang) {
            Write-Host "   OK Script has proper shebang" -ForegroundColor Green
        } else {
            Write-Host "   FAILED Script missing shebang" -ForegroundColor Red
            $issues += "SSL script missing shebang"
        }
        
        if ($hasOpenSSL) {
            Write-Host "   OK Script uses OpenSSL" -ForegroundColor Green
        } else {
            Write-Host "   FAILED Script doesn't use OpenSSL" -ForegroundColor Red
            $issues += "SSL script doesn't use OpenSSL"
        }
        
        if ($hasFunctions) {
            Write-Host "   OK Script has helper functions" -ForegroundColor Green
        } else {
            Write-Host "   Script may be missing helper functions" -ForegroundColor Yellow
        }
        
        Write-VerboseOutput "Script size: $($scriptContent.Length) characters"
    } catch {
        Write-Host "   FAILED Could not analyze SSL script" -ForegroundColor Red
        $issues += "Could not analyze SSL script"
    }
} else {
    Write-Host "   FAILED SSL script not found" -ForegroundColor Red
    $issues += "SSL script not found"
}

Write-Host "`nSummary:" -ForegroundColor Yellow
if ($issues.Count -eq 0) {
    Write-Host "   OK No issues found! Ready to deploy with SSL." -ForegroundColor Green
    Write-Host "`nYou can now run:" -ForegroundColor Green
    Write-Host "   .\deploy\deploy-with-ssl.ps1 -Verbose" -ForegroundColor White
} else {
    Write-Host "   Found $($issues.Count) issue(s):" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "      - $issue" -ForegroundColor Red
    }
    
    Write-Host "`nRecommended Actions:" -ForegroundColor Yellow
    if ($issues -contains "Docker not running") {
        Write-Host "   1. Start Docker Desktop" -ForegroundColor White
    }
    if ($issues -contains "Port 80 in use" -or $issues -contains "Port 443 in use" -or $issues -contains "Port 5000 in use" -or $issues -contains "Port 5432 in use") {
        Write-Host "   2. Stop services using required ports" -ForegroundColor White
    }
    if ($issues -contains "docker-compose.ssl.yml missing" -or $issues -contains "generate-ssl-linux.sh missing" -or $issues -contains "Dockerfile.ssl missing") {
        Write-Host "   3. Ensure all required files are present" -ForegroundColor White
    }
    if ($issues -contains "SSL script missing shebang" -or $issues -contains "SSL script doesn't use OpenSSL") {
        Write-Host "   4. Check SSL script content" -ForegroundColor White
    }
}

Write-Host "`nFor more detailed output, run:" -ForegroundColor Cyan
Write-Host "   .\deploy\diagnose-ssl-fixed.ps1 -Verbose" -ForegroundColor White
