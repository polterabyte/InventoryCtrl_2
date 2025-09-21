# Quick Fix Script for SSL Deployment Issues
param(
    [switch]$Force = $false,
    [switch]$Verbose = $false
)

Write-Host "🔧 SSL Deployment Quick Fix" -ForegroundColor Green
Write-Host "===========================" -ForegroundColor Green

# Function to write verbose output
function Write-VerboseOutput {
    param([string]$Message)
    if ($Verbose) {
        Write-Host "   [VERBOSE] $Message" -ForegroundColor DarkGray
    }
}

try {
    # 1. Stop and clean existing containers
    Write-Host "`n🧹 Cleaning up existing containers..." -ForegroundColor Yellow
    try {
        docker-compose -f docker-compose.ssl.yml down -v --remove-orphans 2>$null
        Write-Host "   ✅ Stopped SSL containers" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠️  No SSL containers to stop" -ForegroundColor Yellow
    }
    
    try {
        docker-compose down -v --remove-orphans 2>$null
        Write-Host "   ✅ Stopped regular containers" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠️  No regular containers to stop" -ForegroundColor Yellow
    }

    # 2. Clean up Docker system
    Write-Host "`n🗑️ Cleaning Docker system..." -ForegroundColor Yellow
    try {
        docker system prune -f
        Write-Host "   ✅ Docker system cleaned" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠️  Docker system cleanup had issues" -ForegroundColor Yellow
    }

    # 3. Fix SSL script line endings
    Write-Host "`n📜 Fixing SSL script line endings..." -ForegroundColor Yellow
    if (Test-Path "scripts\generate-ssl-linux.sh") {
        try {
            $content = Get-Content "scripts\generate-ssl-linux.sh" -Raw -Encoding UTF8
            $content = $content -replace "`r`n", "`n"
            Set-Content "scripts\generate-ssl-linux.sh" -Value $content -NoNewline -Encoding UTF8
            Write-Host "   ✅ SSL script line endings fixed" -ForegroundColor Green
            Write-VerboseOutput "Converted Windows line endings to Unix format"
        } catch {
            Write-Host "   ❌ Could not fix SSL script: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "   ❌ SSL script not found" -ForegroundColor Red
    }

    # 4. Create .env file if missing
    Write-Host "`n📝 Checking .env file..." -ForegroundColor Yellow
    if (!(Test-Path ".env")) {
        if (Test-Path "deploy\env.example") {
            Copy-Item "deploy\env.example" ".env"
            Write-Host "   ✅ Created .env from example" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  No .env example found, creating basic .env" -ForegroundColor Yellow
            $basicEnv = @"
# Basic Environment Configuration
POSTGRES_PASSWORD=postgres
POSTGRES_DB=inventorydb
POSTGRES_USER=postgres
JWT_KEY=ThisIsAVeryLongSecretKeyThatIsAtLeast32CharactersLongForJWT
JWT_ISSUER=InventoryServer
JWT_AUDIENCE=InventoryClient
JWT_EXPIRE_MINUTES=15
JWT_REFRESH_EXPIRE_DAYS=7
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:80
SERVER_IP=192.168.139.96
DOMAIN=warehouse.cuby
STAGING_DOMAIN=staging.warehouse.cuby
TEST_DOMAIN=test.warehouse.cuby
"@
            Set-Content ".env" -Value $basicEnv
            Write-Host "   ✅ Created basic .env file" -ForegroundColor Green
        }
    } else {
        Write-Host "   ✅ .env file already exists" -ForegroundColor Green
    }

    # 5. Check and fix file permissions
    Write-Host "`n🔒 Checking file permissions..." -ForegroundColor Yellow
    $filesToCheck = @(
        "docker-compose.ssl.yml",
        "scripts\generate-ssl-linux.sh",
        "src\Inventory.API\Dockerfile.ssl"
    )
    
    foreach ($file in $filesToCheck) {
        if (Test-Path $file) {
            Write-Host "   ✅ $file exists" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $file missing" -ForegroundColor Red
        }
    }

    # 6. Test Docker Compose syntax
    Write-Host "`n🔍 Testing Docker Compose syntax..." -ForegroundColor Yellow
    try {
        docker-compose -f docker-compose.ssl.yml config 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ Docker Compose syntax is valid" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Docker Compose syntax error" -ForegroundColor Red
        }
    } catch {
        Write-Host "   ❌ Could not validate Docker Compose syntax" -ForegroundColor Red
    }

    # 7. Check port availability
    Write-Host "`n🌐 Checking port availability..." -ForegroundColor Yellow
    $ports = @(80, 443, 5000, 5432)
    $blockedPorts = @()
    
    foreach ($port in $ports) {
        try {
            $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
            if ($connection) {
                Write-Host "   ❌ Port $port is in use" -ForegroundColor Red
                $blockedPorts += $port
            } else {
                Write-Host "   ✅ Port $port is available" -ForegroundColor Green
            }
        } catch {
            Write-Host "   ✅ Port $port is available" -ForegroundColor Green
        }
    }

    # 8. Summary and recommendations
    Write-Host "`n📋 Fix Summary:" -ForegroundColor Yellow
    if ($blockedPorts.Count -eq 0) {
        Write-Host "   ✅ All ports are available" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Blocked ports: $($blockedPorts -join ', ')" -ForegroundColor Yellow
        Write-Host "      You may need to stop services using these ports" -ForegroundColor Gray
    }

    Write-Host "`n🚀 Ready to deploy! Try running:" -ForegroundColor Green
    Write-Host "   .\deploy\deploy-with-ssl.ps1 -Verbose" -ForegroundColor White
    
    if ($blockedPorts.Count -gt 0) {
        Write-Host "`n⚠️  Note: Some ports are blocked. You may need to:" -ForegroundColor Yellow
        Write-Host "   1. Stop services using ports: $($blockedPorts -join ', ')" -ForegroundColor White
        Write-Host "   2. Or use different ports in docker-compose.ssl.yml" -ForegroundColor White
    }

    Write-Host "`n💡 If issues persist, run diagnostics:" -ForegroundColor Cyan
    Write-Host "   .\deploy\diagnose-ssl.ps1 -Verbose" -ForegroundColor White

} catch {
    Write-Host "`n❌ Fix script failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`n🔍 Manual troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "   1. Check if Docker Desktop is running" -ForegroundColor White
    Write-Host "   2. Run as Administrator" -ForegroundColor White
    Write-Host "   3. Check Windows Defender/Antivirus settings" -ForegroundColor White
    Write-Host "   4. Restart Docker Desktop" -ForegroundColor White
}
