# Generate SSL certificates for staging.warehouse.cuby
param(
    [string]$Domain = "staging.warehouse.cuby",
    [string]$OutputPath = "deploy/nginx/ssl"
)

Write-Host "Generating SSL certificates for $Domain..." -ForegroundColor Green

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force
    Write-Host "Created directory: $OutputPath" -ForegroundColor Yellow
}

try {
    # Generate self-signed certificate
    Write-Host "Generating self-signed certificate..." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate -DnsName $Domain -CertStoreLocation "cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(1)
    
    # Export certificate
    $cert | Export-Certificate -FilePath "$OutputPath/$Domain.crt" -Type CERT
    Write-Host "Exported certificate: $OutputPath/$Domain.crt" -ForegroundColor Green
    
    # Export private key (need to use PFX first, then extract key)
    $pwd = ConvertTo-SecureString -String "temp123" -Force -AsPlainText
    $cert | Export-PfxCertificate -FilePath "$OutputPath/$Domain.pfx" -Password $pwd
    Write-Host "Exported PFX: $OutputPath/$Domain.pfx" -ForegroundColor Green
    
    # For nginx, we need PEM format. Let's create a simple key file
    # Note: This is a workaround since we can't easily extract the private key in PEM format without OpenSSL
    Write-Host "Note: You'll need to convert PFX to PEM format for nginx" -ForegroundColor Yellow
    Write-Host "Use: openssl pkcs12 -in $Domain.pfx -out $Domain.key -nocerts -nodes" -ForegroundColor Cyan
    
    Write-Host "SSL certificate generation completed!" -ForegroundColor Green
    
} catch {
    Write-Host "Failed to generate SSL certificates: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
