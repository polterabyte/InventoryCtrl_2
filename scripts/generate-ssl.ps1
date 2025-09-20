# Generate SSL certificates for development
param(
    [string]$Domain = "localhost",
    [string]$OutputPath = "deploy/nginx/ssl"
)

Write-Host "🔐 Generating SSL certificates for development..." -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Create output directory
    if (!(Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force
        Write-Host "📁 Created directory: $OutputPath" -ForegroundColor Yellow
    }

    # Check if OpenSSL is available
    $opensslPath = Get-Command openssl -ErrorAction SilentlyContinue
    if (-not $opensslPath) {
        Write-Host "❌ OpenSSL not found. Please install OpenSSL or use WSL." -ForegroundColor Red
        Write-Host "💡 You can install OpenSSL via:" -ForegroundColor Cyan
        Write-Host "   - Chocolatey: choco install openssl" -ForegroundColor White
        Write-Host "   - Or use WSL with Ubuntu" -ForegroundColor White
        exit 1
    }

    # Generate private key
    Write-Host "🔑 Generating private key..." -ForegroundColor Yellow
    & openssl genrsa -out "$OutputPath/key.pem" 2048
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate private key"
    }

    # Generate certificate signing request
    Write-Host "📝 Generating certificate signing request..." -ForegroundColor Yellow
    & openssl req -new -key "$OutputPath/key.pem" -out "$OutputPath/cert.csr" -subj "/C=US/ST=State/L=City/O=Organization/CN=$Domain"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate CSR"
    }

    # Generate self-signed certificate
    Write-Host "📜 Generating self-signed certificate..." -ForegroundColor Yellow
    & openssl x509 -req -days 365 -in "$OutputPath/cert.csr" -signkey "$OutputPath/key.pem" -out "$OutputPath/cert.pem"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate certificate"
    }

    # Clean up CSR file
    Remove-Item "$OutputPath/cert.csr" -Force

    Write-Host "✅ SSL certificates generated successfully!" -ForegroundColor Green
    Write-Host "📁 Certificates location: $OutputPath" -ForegroundColor Cyan
    Write-Host "   - cert.pem (certificate)" -ForegroundColor White
    Write-Host "   - key.pem (private key)" -ForegroundColor White

    Write-Host "`n⚠️  Note: These are self-signed certificates for development only." -ForegroundColor Yellow
    Write-Host "   For production, use certificates from a trusted CA." -ForegroundColor Yellow

} catch {
    Write-Host "❌ Failed to generate SSL certificates: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
