# SSL Certificate Generation Script for warehouse.cuby domains
param(
    [switch]$UseLetsEncrypt = $false,
    [string]$Email = "admin@warehouse.cuby"
)

Write-Host "Generating SSL certificates for warehouse.cuby domains..." -ForegroundColor Green

# Create SSL directory if it doesn't exist
if (!(Test-Path "deploy/nginx/ssl")) {
    New-Item -ItemType Directory -Path "deploy/nginx/ssl" -Force
    Write-Host "Created deploy/nginx/ssl directory" -ForegroundColor Yellow
}

if ($UseLetsEncrypt) {
    Write-Host "Using Let's Encrypt for SSL certificates..." -ForegroundColor Yellow
    
    # Install certbot if not already installed
    if (!(Get-Command certbot -ErrorAction SilentlyContinue)) {
        Write-Host "Installing certbot..." -ForegroundColor Yellow
        # This would need to be run on the server, not locally
        Write-Warning "Please install certbot on your server first:"
        Write-Host "sudo apt install certbot python3-certbot-nginx" -ForegroundColor Cyan
        exit 1
    }
    
    # Generate certificates for all domains
    $domains = @("warehouse.cuby", "staging.warehouse.cuby", "test.warehouse.cuby")
    
    foreach ($domain in $domains) {
        Write-Host "Generating certificate for $domain..." -ForegroundColor Yellow
        # This would need to be run on the server
        Write-Host "Run this command on your server:" -ForegroundColor Cyan
        Write-Host "sudo certbot certonly --nginx -d $domain --email $Email --agree-tos --non-interactive" -ForegroundColor Cyan
    }
} else {
    Write-Host "Generating self-signed certificates..." -ForegroundColor Yellow
    
    # Generate self-signed certificates for development/testing
    $domains = @("warehouse.cuby", "staging.warehouse.cuby", "test.warehouse.cuby")
    
    foreach ($domain in $domains) {
        Write-Host "Generating self-signed certificate for $domain..." -ForegroundColor Yellow
        
        # Generate private key
        $keyFile = "deploy/nginx/ssl/$domain.key"
        $certFile = "deploy/nginx/ssl/$domain.crt"
        
        # Create OpenSSL command for self-signed certificate
        $opensslCmd = @"
openssl req -x509 -newkey rsa:4096 -keyout $keyFile -out $certFile -days 365 -nodes -subj "/C=US/ST=State/L=City/O=Organization/OU=OrgUnit/CN=$domain"
"@
        
        Write-Host "Run this command to generate certificate for ${domain}:" -ForegroundColor Cyan
        Write-Host $opensslCmd -ForegroundColor White
        
        # Note: This would need to be run manually or with proper OpenSSL installation
        Write-Warning "Please run the above command to generate the certificate"
    }
}

Write-Host "SSL certificate generation instructions completed!" -ForegroundColor Green
Write-Host "After generating certificates, restart nginx with:" -ForegroundColor Cyan
Write-Host "docker-compose restart nginx-proxy" -ForegroundColor White
