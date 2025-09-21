# Deploy Inventory Control System with SSL Support
param(
    [string]$Environment = "development",
    [string]$Domain = "warehouse.cuby",
    [string]$Email = "admin@warehouse.cuby",
    [switch]$UseLetsEncrypt = $false,
    [switch]$Force = $false,
    [switch]$Clean = $false,
    [switch]$Verbose = $false
)

Write-Host "🚀 Deploying Inventory Control System with SSL Support" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Function to write verbose output
function Write-VerboseOutput {
    param([string]$Message)
    if ($Verbose) {
        Write-Host "   [VERBOSE] $Message" -ForegroundColor DarkGray
    }
}

# Function to check if file exists
function Test-FileExists {
    param([string]$Path, [string]$Description)
    if (Test-Path $Path) {
        Write-VerboseOutput "$Description found: $Path"
        return $true
    } else {
        Write-Host "❌ $Description not found: $Path" -ForegroundColor Red
        return $false
    }
}

try {
    # Check if Docker is running
    Write-Host "🐳 Checking Docker..." -ForegroundColor Yellow
    try {
        docker version | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Docker command failed"
        }
        Write-Host "   ✅ Docker is running" -ForegroundColor Green
    } catch {
        throw "Docker is not running or not accessible. Please start Docker Desktop and ensure it's running."
    }

    # Check required files
    Write-Host "📋 Checking required files..." -ForegroundColor Yellow
    $requiredFiles = @(
        @{Path="docker-compose.ssl.yml"; Description="Docker Compose SSL file"},
        @{Path="scripts\generate-ssl-linux.sh"; Description="Linux SSL generation script"},
        @{Path="src\Inventory.API\Dockerfile.ssl"; Description="SSL Dockerfile"}
    )

    $missingFiles = @()
    foreach ($file in $requiredFiles) {
        if (-not (Test-FileExists $file.Path $file.Description)) {
            $missingFiles += $file.Path
        }
    }

    if ($missingFiles.Count -gt 0) {
        Write-Host "❌ Missing required files:" -ForegroundColor Red
        foreach ($file in $missingFiles) {
            Write-Host "   - $file" -ForegroundColor Red
        }
        throw "Required files are missing. Please ensure all SSL-related files are present."
    }

    # Clean up if requested
    if ($Clean) {
        Write-Host "🧹 Cleaning up existing containers..." -ForegroundColor Yellow
        try {
            docker-compose -f docker-compose.ssl.yml down -v --remove-orphans 2>$null
            docker system prune -f
            Write-Host "   ✅ Cleanup completed" -ForegroundColor Green
        } catch {
            Write-Host "   ⚠️  Cleanup had issues, but continuing..." -ForegroundColor Yellow
        }
    }

    # Create .env file if it doesn't exist
    if (!(Test-Path ".env")) {
        Write-Host "📝 Creating .env file..." -ForegroundColor Yellow
        if (Test-Path "deploy\env.$Environment") {
            Copy-Item "deploy\env.$Environment" ".env"
            Write-Host "   ✅ Created .env from deploy\env.$Environment" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Using default .env configuration" -ForegroundColor Yellow
        }
    }

    # Set SSL environment variables
    Write-Host "🔐 Configuring SSL environment..." -ForegroundColor Yellow
    $env:SSL_ENVIRONMENT = $Environment
    $env:SSL_KEY_SIZE = "4096"
    $env:SSL_VALIDITY_DAYS = "365"
    $env:SSL_LETS_ENCRYPT_ENABLED = $UseLetsEncrypt.ToString().ToLower()
    $env:SSL_LETS_ENCRYPT_EMAIL = $Email
    $env:SSL_LETS_ENCRYPT_STAGING = "false"

    # Prepare Linux script for Docker
    Write-Host "📜 Preparing SSL generation script..." -ForegroundColor Yellow
    if (Test-Path "scripts\generate-ssl-linux.sh") {
        try {
            # On Windows, we need to ensure line endings are correct for Linux
            $content = Get-Content "scripts\generate-ssl-linux.sh" -Raw -Encoding UTF8
            $content = $content -replace "`r`n", "`n"
            Set-Content "scripts\generate-ssl-linux.sh" -Value $content -NoNewline -Encoding UTF8
            Write-Host "   ✅ SSL script prepared for Linux" -ForegroundColor Green
            Write-VerboseOutput "Script line endings converted to Unix format"
        } catch {
            Write-Host "   ⚠️  Warning: Could not prepare SSL script, but continuing..." -ForegroundColor Yellow
            Write-VerboseOutput "Error preparing script: $($_.Exception.Message)"
        }
    } else {
        Write-Host "   ❌ SSL script not found: scripts\generate-ssl-linux.sh" -ForegroundColor Red
        throw "SSL generation script not found"
    }

    # Build and run with SSL support
    Write-Host "🔨 Building and starting services with SSL..." -ForegroundColor Yellow
    try {
        Write-VerboseOutput "Running: docker-compose -f docker-compose.ssl.yml up -d --build"
        docker-compose -f docker-compose.ssl.yml up -d --build
        if ($LASTEXITCODE -ne 0) {
            throw "Docker Compose command failed with exit code $LASTEXITCODE"
        }
        Write-Host "   ✅ Docker Compose command completed" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to start services with Docker Compose" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        throw "Docker Compose failed: $($_.Exception.Message)"
    }

    # Wait for services to be ready
    Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
    Write-VerboseOutput "Waiting 15 seconds for services to initialize..."
    Start-Sleep -Seconds 15

    # Check if SSL certificates were generated
    Write-Host "🔍 Checking SSL certificate generation..." -ForegroundColor Yellow
    try {
        $sslContainer = docker ps --filter "name=inventory-ssl-generator" --format "{{.Names}}"
        if ($sslContainer) {
            Write-Host "   ✅ SSL generator container found: $sslContainer" -ForegroundColor Green
            Write-VerboseOutput "SSL generator container is running"
        } else {
            Write-Host "   ⚠️  SSL generator container not found (this is normal - it's an init container)" -ForegroundColor Yellow
            Write-VerboseOutput "SSL generator runs as init container and exits after completion"
        }
        
        # Check SSL certificates volume
        try {
            $sslVolume = docker volume inspect inventoryctrl_2_ssl_certificates 2>$null
            if ($sslVolume) {
                Write-Host "   ✅ SSL certificates volume created" -ForegroundColor Green
                Write-VerboseOutput "SSL volume: inventoryctrl_2_ssl_certificates"
            } else {
                Write-Host "   ⚠️  SSL certificates volume not found" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "   ⚠️  Could not check SSL volume: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   ⚠️  Could not check SSL generation status: $($_.Exception.Message)" -ForegroundColor Yellow
    }

    # Check service health
    Write-Host "🏥 Checking service health..." -ForegroundColor Yellow
    
    $apiHealthy = $false
    $webHealthy = $false
    
    # Check API health
    try {
        Write-VerboseOutput "Checking API health at http://localhost:5000/health"
        $apiResponse = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 10 -ErrorAction Stop
        if ($apiResponse.StatusCode -eq 200) {
            $apiHealthy = $true
            Write-Host "✅ API is healthy" -ForegroundColor Green
            Write-VerboseOutput "API responded with status 200"
        } else {
            Write-Host "⚠️  API responded with status $($apiResponse.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ API health check failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-VerboseOutput "API health check error: $($_.Exception.Message)"
    }

    # Check Web Client health
    try {
        Write-VerboseOutput "Checking Web Client health at http://localhost/health"
        $webResponse = Invoke-WebRequest -Uri "http://localhost/health" -TimeoutSec 10 -ErrorAction Stop
        if ($webResponse.StatusCode -eq 200) {
            $webHealthy = $true
            Write-Host "✅ Web Client is healthy" -ForegroundColor Green
            Write-VerboseOutput "Web Client responded with status 200"
        } else {
            Write-Host "⚠️  Web Client responded with status $($webResponse.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ Web Client health check failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-VerboseOutput "Web Client health check error: $($_.Exception.Message)"
    }

    # Test SSL endpoints
    Write-Host "🔐 Testing SSL endpoints..." -ForegroundColor Yellow
    try {
        Write-VerboseOutput "Testing HTTPS endpoint at https://localhost"
        $sslResponse = Invoke-WebRequest -Uri "https://localhost" -TimeoutSec 10 -SkipCertificateCheck -ErrorAction Stop
        if ($sslResponse.StatusCode -eq 200) {
            Write-Host "✅ HTTPS endpoint is working" -ForegroundColor Green
            Write-VerboseOutput "HTTPS responded with status 200"
        } else {
            Write-Host "⚠️  HTTPS endpoint responded with status $($sslResponse.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "⚠️  HTTPS endpoint not accessible: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-VerboseOutput "This is normal for self-signed certificates or if services are still starting"
    }

    # Show final status
    Write-Host "`n🎉 Deployment with SSL completed!" -ForegroundColor Green
    Write-Host "=================================" -ForegroundColor Green
    
    Write-Host "`n🌐 Access URLs:" -ForegroundColor Cyan
    Write-Host "   Web Application (HTTP): http://localhost" -ForegroundColor White
    Write-Host "   Web Application (HTTPS): https://localhost" -ForegroundColor White
    Write-Host "   API (HTTP): http://localhost:5000" -ForegroundColor White
    Write-Host "   API Swagger: http://localhost:5000/swagger" -ForegroundColor White
    Write-Host "   SSL API: https://localhost:5000/api/SSLCertificate" -ForegroundColor White
    
    Write-Host "`n📊 Service Status:" -ForegroundColor Cyan
    docker ps --filter "name=inventory-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

    Write-Host "`n🔐 SSL Certificate Management:" -ForegroundColor Cyan
    Write-Host "   View certificates: docker exec inventory-nginx-proxy ls -la /etc/nginx/ssl/" -ForegroundColor White
    Write-Host "   Generate new cert: docker exec inventory-api /usr/local/bin/generate-ssl-linux.sh --environment $Environment" -ForegroundColor White
    Write-Host "   Check cert details: docker exec inventory-nginx-proxy openssl x509 -in /etc/nginx/ssl/localhost.crt -text -noout" -ForegroundColor White

    Write-Host "`n📋 Useful Commands:" -ForegroundColor Cyan
    Write-Host "   View logs: docker-compose -f docker-compose.ssl.yml logs -f" -ForegroundColor White
    Write-Host "   Stop services: docker-compose -f docker-compose.ssl.yml down" -ForegroundColor White
    Write-Host "   Restart API: docker-compose -f docker-compose.ssl.yml restart inventory-api" -ForegroundColor White
    Write-Host "   Regenerate SSL: docker-compose -f docker-compose.ssl.yml up ssl-generator" -ForegroundColor White

    if (-not $apiHealthy -or -not $webHealthy) {
        Write-Host "`n⚠️  Some services may still be starting up. Check logs for details." -ForegroundColor Yellow
    }

    Write-Host "`n🔒 SSL Certificate Notes:" -ForegroundColor Yellow
    Write-Host "   - Self-signed certificates are generated for development" -ForegroundColor White
    Write-Host "   - For production, use Let's Encrypt with --UseLetsEncrypt flag" -ForegroundColor White
    Write-Host "   - Certificates are stored in Docker volume: inventoryctrl_2_ssl_certificates" -ForegroundColor White
    Write-Host "   - Browser will show security warning for self-signed certificates" -ForegroundColor White

} catch {
    Write-Host "`n❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "===============================================" -ForegroundColor Red
    
    Write-Host "`n🔍 Troubleshooting Steps:" -ForegroundColor Yellow
    Write-Host "   1. Check if Docker is running:" -ForegroundColor White
    Write-Host "      docker version" -ForegroundColor Gray
    Write-Host "   2. Check if ports are available:" -ForegroundColor White
    Write-Host "      netstat -an | findstr ':80 :443 :5000 :5432'" -ForegroundColor Gray
    Write-Host "   3. Check Docker Compose logs:" -ForegroundColor White
    Write-Host "      docker-compose -f docker-compose.ssl.yml logs" -ForegroundColor Gray
    Write-Host "   4. Check specific service logs:" -ForegroundColor White
    Write-Host "      docker-compose -f docker-compose.ssl.yml logs inventory-api" -ForegroundColor Gray
    Write-Host "      docker-compose -f docker-compose.ssl.yml logs ssl-generator" -ForegroundColor Gray
    Write-Host "   5. Try cleaning up and retrying:" -ForegroundColor White
    Write-Host "      .\deploy\deploy-with-ssl-fixed.ps1 -Clean -Verbose" -ForegroundColor Gray
    Write-Host "   6. Check required files:" -ForegroundColor White
    Write-Host "      Test-Path docker-compose.ssl.yml" -ForegroundColor Gray
    Write-Host "      Test-Path scripts\generate-ssl-linux.sh" -ForegroundColor Gray
    Write-Host "      Test-Path src\Inventory.API\Dockerfile.ssl" -ForegroundColor Gray
    Write-Host "   7. Run with verbose output for more details:" -ForegroundColor White
    Write-Host "      .\deploy\deploy-with-ssl-fixed.ps1 -Verbose" -ForegroundColor Gray
    
    Write-Host "`n📋 Common Issues:" -ForegroundColor Yellow
    Write-Host "   - Docker Desktop not running" -ForegroundColor White
    Write-Host "   - Ports already in use by other services" -ForegroundColor White
    Write-Host "   - Missing required files" -ForegroundColor White
    Write-Host "   - Docker Compose syntax errors" -ForegroundColor White
    Write-Host "   - SSL script permissions issues" -ForegroundColor White
    Write-Host "   - Insufficient disk space" -ForegroundColor White
    
    Write-Host "`n💡 Quick Fixes:" -ForegroundColor Yellow
    Write-Host "   - Restart Docker Desktop" -ForegroundColor White
    Write-Host "   - Stop conflicting services" -ForegroundColor White
    Write-Host "   - Run as Administrator" -ForegroundColor White
    Write-Host "   - Check Windows Defender/Antivirus" -ForegroundColor White
    
    exit 1
}
