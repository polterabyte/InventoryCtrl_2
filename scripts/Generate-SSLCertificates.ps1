# Enhanced SSL Certificate Generation Script for Inventory Control System
# Supports both self-signed certificates for development and Let's Encrypt for production

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("development", "staging", "production", "all")]
    [string]$Environment = "all",
    
    [Parameter(Mandatory=$false)]
    [switch]$UseLetsEncrypt = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$Email = "admin@warehouse.cuby",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "deploy/nginx/ssl",
    
    [Parameter(Mandatory=$false)]
    [int]$KeySize = 4096,
    
    [Parameter(Mandatory=$false)]
    [int]$ValidityDays = 365,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose = $false
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Color functions
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    } else {
        $input | Write-Output
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Write-Success { Write-ColorOutput Green $args }
function Write-Info { Write-ColorOutput Cyan $args }
function Write-Warning { Write-ColorOutput Yellow $args }
function Write-Error { Write-ColorOutput Red $args }

# Domain configuration
$Domains = @{
    "development" = @("localhost", "127.0.0.1", "192.168.139.96")
    "staging" = @("staging.warehouse.cuby")
    "production" = @("warehouse.cuby")
    "test" = @("test.warehouse.cuby")
}

# Certificate configuration
$CertConfig = @{
    Country = "US"
    State = "State"
    City = "City"
    Organization = "Inventory Control System"
    OrganizationalUnit = "IT Department"
    CommonName = ""
    SubjectAlternativeNames = @()
}

Write-Success "🔐 SSL Certificate Generation Script for Inventory Control System"
Write-Info "Environment: $Environment"
Write-Info "Use Let's Encrypt: $UseLetsEncrypt"
Write-Info "Output Path: $OutputPath"
Write-Info "Key Size: $KeySize bits"
Write-Info "Validity: $ValidityDays days"

try {
    # Create output directory
    if (!(Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        Write-Info "📁 Created directory: $OutputPath"
    }

    # Check if OpenSSL is available
    $opensslPath = Get-Command openssl -ErrorAction SilentlyContinue
    if (-not $opensslPath) {
        Write-Error "❌ OpenSSL not found. Please install OpenSSL or use WSL."
        Write-Info "💡 You can install OpenSSL via:"
        Write-Info "   - Chocolatey: choco install openssl"
        Write-Info "   - Or use WSL with Ubuntu"
        Write-Info "   - Or use Docker: docker run --rm -v `"${PWD}/$OutputPath:/ssl`" alpine/openssl"
        exit 1
    }

    # Function to generate self-signed certificate
    function New-SelfSignedCertificate {
        param(
            [string]$Domain,
            [string]$OutputDir,
            [string[]]$SubjectAlternativeNames = @()
        )
        
        Write-Info "🔑 Generating certificate for: $Domain"
        
        $keyFile = Join-Path $OutputDir "$Domain.key"
        $certFile = Join-Path $OutputDir "$Domain.crt"
        $csrFile = Join-Path $OutputDir "$Domain.csr"
        
        # Check if certificate already exists and not forcing
        if ((Test-Path $certFile) -and !$Force) {
            Write-Warning "⚠️  Certificate for $Domain already exists. Use -Force to overwrite."
            return
        }
        
        # Build SAN string
        $sanString = ""
        if ($SubjectAlternativeNames.Count -gt 0) {
            $sanList = @()
            foreach ($san in $SubjectAlternativeNames) {
                if ($san -match '^\d+\.\d+\.\d+\.\d+$') {
                    $sanList += "IP:$san"
                } else {
                    $sanList += "DNS:$san"
                }
            }
            $sanString = $sanList -join ","
        }
        
        # Generate private key
        Write-Info "   Generating private key..."
        $keyCmd = "openssl genrsa -out `"$keyFile`" $KeySize"
        if ($Verbose) { Write-Info "   Command: $keyCmd" }
        Invoke-Expression $keyCmd
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to generate private key for $Domain"
        }
        
        # Generate certificate signing request
        Write-Info "   Generating certificate signing request..."
        $csrCmd = "openssl req -new -key `"$keyFile`" -out `"$csrFile`" -subj `/C=$($CertConfig.Country)/ST=$($CertConfig.State)/L=$($CertConfig.City)/O=$($CertConfig.Organization)/OU=$($CertConfig.OrganizationalUnit)/CN=$Domain`"
        if ($Verbose) { Write-Info "   Command: $csrCmd" }
        Invoke-Expression $csrCmd
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to generate CSR for $Domain"
        }
        
        # Generate self-signed certificate
        Write-Info "   Generating self-signed certificate..."
        $certCmd = "openssl x509 -req -days $ValidityDays -in `"$csrFile`" -signkey `"$keyFile`" -out `"$certFile`""
        
        # Add SAN if provided
        if ($sanString) {
            $certCmd += " -extensions v3_req -extfile <(echo `"[v3_req]`"; echo `"subjectAltName=$sanString`")"
        }
        
        if ($Verbose) { Write-Info "   Command: $certCmd" }
        Invoke-Expression $certCmd
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to generate certificate for $Domain"
        }
        
        # Clean up CSR file
        Remove-Item $csrFile -Force -ErrorAction SilentlyContinue
        
        # Verify certificate
        Write-Info "   Verifying certificate..."
        $verifyCmd = "openssl x509 -in `"$certFile`" -text -noout"
        Invoke-Expression $verifyCmd | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Certificate verification failed for $Domain"
        }
        
        Write-Success "   ✅ Certificate generated successfully: $Domain"
    }

    # Function to generate Let's Encrypt certificate
    function New-LetsEncryptCertificate {
        param(
            [string]$Domain,
            [string]$OutputDir
        )
        
        Write-Info "🔐 Generating Let's Encrypt certificate for: $Domain"
        
        # Check if certbot is available
        $certbotPath = Get-Command certbot -ErrorAction SilentlyContinue
        if (-not $certbotPath) {
            Write-Error "❌ Certbot not found. Please install certbot first:"
            Write-Info "   Ubuntu/Debian: sudo apt install certbot python3-certbot-nginx"
            Write-Info "   CentOS/RHEL: sudo yum install certbot python3-certbot-nginx"
            Write-Info "   Or use Docker: docker run --rm -v `"${PWD}/$OutputPath:/etc/letsencrypt`" certbot/certbot certonly --standalone -d $Domain --email $Email --agree-tos --non-interactive"
            return
        }
        
        # Generate certificate using certbot
        $certCmd = "certbot certonly --standalone -d $Domain --email $Email --agree-tos --non-interactive"
        Write-Info "   Command: $certCmd"
        Invoke-Expression $certCmd
        
        if ($LASTEXITCODE -eq 0) {
            # Copy certificates to our output directory
            $leCertPath = "/etc/letsencrypt/live/$Domain"
            $keyFile = Join-Path $OutputDir "$Domain.key"
            $certFile = Join-Path $OutputDir "$Domain.crt"
            
            if (Test-Path "$leCertPath/privkey.pem") {
                Copy-Item "$leCertPath/privkey.pem" $keyFile -Force
            }
            if (Test-Path "$leCertPath/fullchain.pem") {
                Copy-Item "$leCertPath/fullchain.pem" $certFile -Force
            }
            
            Write-Success "   ✅ Let's Encrypt certificate generated successfully: $Domain"
        } else {
            throw "Failed to generate Let's Encrypt certificate for $Domain"
        }
    }

    # Generate certificates based on environment
    $environmentsToProcess = @()
    if ($Environment -eq "all") {
        $environmentsToProcess = $Domains.Keys
    } else {
        $environmentsToProcess = @($Environment)
    }

    foreach ($env in $environmentsToProcess) {
        if ($Domains.ContainsKey($env)) {
            Write-Info "`n🌐 Processing environment: $env"
            
            foreach ($domain in $Domains[$env]) {
                try {
                    if ($UseLetsEncrypt) {
                        New-LetsEncryptCertificate -Domain $domain -OutputDir $OutputPath
                    } else {
                        # For localhost and IP addresses, add additional SANs
                        $sans = @($domain)
                        if ($domain -eq "localhost") {
                            $sans += @("127.0.0.1", "::1")
                        } elseif ($domain -eq "127.0.0.1") {
                            $sans += @("localhost", "::1")
                        } elseif ($domain -eq "192.168.139.96") {
                            $sans += @("localhost", "127.0.0.1")
                        }
                        
                        New-SelfSignedCertificate -Domain $domain -OutputDir $OutputPath -SubjectAlternativeNames $sans
                    }
                } catch {
                    Write-Error "❌ Failed to generate certificate for $domain`: $($_.Exception.Message)"
                }
            }
        }
    }

    # Generate a wildcard certificate for development
    if ($Environment -eq "all" -or $Environment -eq "development") {
        Write-Info "`n🌐 Generating wildcard certificate for development..."
        try {
            New-SelfSignedCertificate -Domain "*.warehouse.cuby" -OutputDir $OutputPath -SubjectAlternativeNames @("warehouse.cuby", "*.warehouse.cuby", "*.staging.warehouse.cuby", "*.test.warehouse.cuby")
        } catch {
            Write-Error "❌ Failed to generate wildcard certificate: $($_.Exception.Message)"
        }
    }

    # Set proper permissions
    Write-Info "`n🔒 Setting file permissions..."
    Get-ChildItem $OutputPath -Filter "*.key" | ForEach-Object {
        $_.Attributes = "Hidden, System, ReadOnly"
        Write-Info "   Set permissions for: $($_.Name)"
    }

    Write-Success "`n✅ SSL certificate generation completed!"
    Write-Info "📁 Certificates location: $OutputPath"
    
    # List generated certificates
    Write-Info "`n📋 Generated certificates:"
    Get-ChildItem $OutputPath -Filter "*.crt" | ForEach-Object {
        $certFile = $_.FullName
        $keyFile = $certFile -replace '\.crt$', '.key'
        
        if (Test-Path $keyFile) {
            Write-Info "   ✅ $($_.Name) (with private key)"
        } else {
            Write-Warning "   ⚠️  $($_.Name) (missing private key)"
        }
    }

    if (!$UseLetsEncrypt) {
        Write-Warning "`n⚠️  Note: These are self-signed certificates for development only."
        Write-Warning "   For production, use certificates from a trusted CA or Let's Encrypt."
    }

    Write-Info "`n🚀 Next steps:"
    Write-Info "   1. Restart nginx: docker-compose restart nginx-proxy"
    Write-Info "   2. Test HTTPS: https://localhost or your domain"
    Write-Info "   3. Check certificate: openssl x509 -in `"$OutputPath/localhost.crt`" -text -noout"

} catch {
    Write-Error "❌ Script failed: $($_.Exception.Message)"
    exit 1
}
