# Simple SSL Diagnostics
param([switch]$Verbose = $false)

Write-Host "🔍 SSL Deployment Diagnostics" -ForegroundColor Green
Write-Host "=============================" -ForegroundColor Green

$issues = @()

# Check Docker
Write-Host "`n🐳 Checking Docker..." -ForegroundColor Yellow
try {
    docker version | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Docker is running" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Docker not running" -ForegroundColor Red
        $issues += "Docker not running"
    }
} catch {
    Write-Host "   ❌ Docker not accessible" -ForegroundColor Red
    $issues += "Docker not accessible"
}

# Check Docker Compose
Write-Host "`n🔧 Checking Docker Compose..." -ForegroundColor Yellow
try {
    docker-compose --version | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Docker Compose available" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Docker Compose not available" -ForegroundColor Red
        $issues += "Docker Compose not available"
    }
} catch {
    Write-Host "   ❌ Docker Compose not accessible" -ForegroundColor Red
    $issues += "Docker Compose not accessible"
}

# Check required files
Write-Host "`n📁 Checking required files..." -ForegroundColor Yellow

$files = @(
    @{Path="docker-compose.ssl.yml"; Name="Docker Compose SSL"},
    @{Path="scripts\generate-ssl-linux.sh"; Name="Linux SSL Script"},
    @{Path="src\Inventory.API\Dockerfile.ssl"; Name="SSL Dockerfile"}
)

foreach ($file in $files) {
    if (Test-Path $file.Path) {
        Write-Host "   ✅ $($file.Name) exists" -ForegroundColor Green
        if ($Verbose) {
            Write-Host "      [VERBOSE] Path: $($file.Path)" -ForegroundColor DarkGray
        }
    } else {
        Write-Host "   ❌ $($file.Name) missing" -ForegroundColor Red
        Write-Host "      Path: $($file.Path)" -ForegroundColor Red
        $issues += "$($file.Name) missing"
    }
}

# Check ports
Write-Host "`n🌐 Checking ports..." -ForegroundColor Yellow
$ports = @(80, 443, 5000, 5432)
foreach ($port in $ports) {
    try {
        $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
        if ($connection) {
            Write-Host "   ❌ Port $port is in use" -ForegroundColor Red
            $issues += "Port $port in use"
        } else {
            Write-Host "   ✅ Port $port is available" -ForegroundColor Green
        }
    } catch {
        Write-Host "   ✅ Port $port is available" -ForegroundColor Green
    }
}

# Check existing containers
Write-Host "`n🐳 Checking existing containers..." -ForegroundColor Yellow
try {
    $containers = docker ps -a --filter "name=inventory-" --format "{{.Names}}" 2>$null
    if ($containers) {
        Write-Host "   Found containers: $containers" -ForegroundColor Yellow
    } else {
        Write-Host "   ✅ No existing inventory containers" -ForegroundColor Green
    }
} catch {
    Write-Host "   ⚠️  Could not check containers" -ForegroundColor Yellow
}

# Summary
Write-Host "`n📋 Summary:" -ForegroundColor Yellow
if ($issues.Count -eq 0) {
    Write-Host "   ✅ No issues found! Ready to deploy." -ForegroundColor Green
    Write-Host "`n🚀 Run: .\deploy\deploy-with-ssl.ps1 -Verbose" -ForegroundColor Green
} else {
    Write-Host "   ❌ Found $($issues.Count) issue(s):" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "      - $issue" -ForegroundColor Red
    }
    
    Write-Host "`n🔧 Try running: .\deploy\fix-ssl-deployment.ps1" -ForegroundColor Yellow
}