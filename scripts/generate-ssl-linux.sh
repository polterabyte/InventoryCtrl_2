#!/bin/sh
# Linux-compatible SSL Certificate Generation Script for Docker containers
# Supports both self-signed certificates and Let's Encrypt

set -e

# Default parameters
ENVIRONMENT="all"
USE_LETS_ENCRYPT=false
EMAIL="admin@warehouse.cuby"
OUTPUT_PATH="/etc/nginx/ssl"
KEY_SIZE=4096
VALIDITY_DAYS=365
FORCE=false
VERBOSE=false

# Color functions
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_info() { echo -e "${CYAN}ℹ️  $1${NC}"; }
print_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --use-lets-encrypt)
            USE_LETS_ENCRYPT=true
            shift
            ;;
        --email)
            EMAIL="$2"
            shift 2
            ;;
        -o|--output-path)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        -k|--key-size)
            KEY_SIZE="$2"
            shift 2
            ;;
        -d|--validity-days)
            VALIDITY_DAYS="$2"
            shift 2
            ;;
        -f|--force)
            FORCE=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -e, --environment ENV    Environment (development, staging, production, all)"
            echo "  --use-lets-encrypt       Use Let's Encrypt instead of self-signed"
            echo "  --email EMAIL           Email for Let's Encrypt"
            echo "  -o, --output-path PATH  Output directory for certificates"
            echo "  -k, --key-size SIZE     Key size in bits (default: 4096)"
            echo "  -d, --validity-days DAYS Validity period in days (default: 365)"
            echo "  -f, --force             Force overwrite existing certificates"
            echo "  -v, --verbose           Verbose output"
            echo "  -h, --help              Show this help"
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Domain configuration
get_domains() {
    case "$1" in
        development)
            echo "localhost 127.0.0.1 192.168.139.96"
            ;;
        staging)
            echo "staging.warehouse.cuby"
            ;;
        production)
            echo "warehouse.cuby"
            ;;
        test)
            echo "test.warehouse.cuby"
            ;;
        *)
            echo "localhost 127.0.0.1 192.168.139.96"
            ;;
    esac
}

print_success "🔐 SSL Certificate Generation Script for Inventory Control System"
print_info "Environment: $ENVIRONMENT"
print_info "Use Let's Encrypt: $USE_LETS_ENCRYPT"
print_info "Output Path: $OUTPUT_PATH"
print_info "Key Size: $KEY_SIZE bits"
print_info "Validity: $VALIDITY_DAYS days"

# Create output directory
mkdir -p "$OUTPUT_PATH"
print_info "📁 Created directory: $OUTPUT_PATH"

# Check if OpenSSL is available
if ! command -v openssl &> /dev/null; then
    print_error "OpenSSL not found. Installing..."
    if command -v apt-get &> /dev/null; then
        apt-get update && apt-get install -y openssl
    elif command -v apk &> /dev/null; then
        apk add --no-cache openssl
    elif command -v yum &> /dev/null; then
        yum install -y openssl
    else
        print_error "Cannot install OpenSSL. Please install it manually."
        exit 1
    fi
fi

# Function to generate self-signed certificate
generate_self_signed_cert() {
    local domain="$1"
    local output_dir="$2"
    local sans="$3"
    
    print_info "🔑 Generating certificate for: $domain"
    
    local key_file="$output_dir/$domain.key"
    local cert_file="$output_dir/$domain.crt"
    local csr_file="$output_dir/$domain.csr"
    
    # Check if certificate already exists and not forcing
    if [ -f "$cert_file" ] && [ "$FORCE" != "true" ]; then
        print_warning "Certificate for $domain already exists. Use --force to overwrite."
        return
    fi
    
    # Generate private key
    print_info "   Generating private key..."
    openssl genrsa -out "$key_file" $KEY_SIZE
    
    # Generate certificate signing request
    print_info "   Generating certificate signing request..."
    openssl req -new -key "$key_file" -out "$csr_file" \
        -subj "/C=US/ST=State/L=City/O=Inventory Control System/OU=IT Department/CN=$domain"
    
    # Generate self-signed certificate
    print_info "   Generating self-signed certificate..."
    if [[ -n "$sans" ]]; then
        # Create config file for SAN
        local config_file="$output_dir/$domain.conf"
        cat > "$config_file" << EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = v3_req
prompt = no

[req_distinguished_name]
C = US
ST = State
L = City
O = Inventory Control System
OU = IT Department
CN = $domain

[v3_req]
keyUsage = keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth
subjectAltName = $sans
EOF
        
        openssl x509 -req -days $VALIDITY_DAYS -in "$csr_file" -signkey "$key_file" \
            -out "$cert_file" -extensions v3_req -extfile "$config_file"
        
        rm -f "$config_file"
    else
        openssl x509 -req -days $VALIDITY_DAYS -in "$csr_file" -signkey "$key_file" -out "$cert_file"
    fi
    
    # Clean up CSR file
    rm -f "$csr_file"
    
    # Verify certificate
    print_info "   Verifying certificate..."
    openssl x509 -in "$cert_file" -text -noout > /dev/null
    
    print_success "   Certificate generated successfully: $domain"
}

# Function to generate Let's Encrypt certificate
generate_lets_encrypt_cert() {
    local domain="$1"
    local output_dir="$2"
    
    print_info "🔐 Generating Let's Encrypt certificate for: $domain"
    
    # Check if certbot is available
    if ! command -v certbot &> /dev/null; then
        print_error "Certbot not found. Please install certbot first:"
        print_info "   Ubuntu/Debian: apt install certbot"
        print_info "   Alpine: apk add certbot"
        print_info "   CentOS/RHEL: yum install certbot"
        return
    fi
    
    # Generate certificate using certbot
    certbot certonly --standalone -d "$domain" --email "$EMAIL" --agree-tos --non-interactive
    
    if [[ $? -eq 0 ]]; then
        # Copy certificates to our output directory
        local le_cert_path="/etc/letsencrypt/live/$domain"
        local key_file="$output_dir/$domain.key"
        local cert_file="$output_dir/$domain.crt"
        
        if [[ -f "$le_cert_path/privkey.pem" ]]; then
            cp "$le_cert_path/privkey.pem" "$key_file"
        fi
        if [[ -f "$le_cert_path/fullchain.pem" ]]; then
            cp "$le_cert_path/fullchain.pem" "$cert_file"
        fi
        
        print_success "   Let's Encrypt certificate generated successfully: $domain"
    else
        print_error "Failed to generate Let's Encrypt certificate for $domain"
    fi
}

# Generate certificates based on environment
if [ "$ENVIRONMENT" = "all" ]; then
    environments="development staging production test"
else
    environments="$ENVIRONMENT"
fi

for env in $environments; do
    if [ -n "$(get_domains "$env")" ]; then
        print_info "🌐 Processing environment: $env"
        
        for domain in $(get_domains "$env"); do
            if [ "$USE_LETS_ENCRYPT" = "true" ]; then
                generate_lets_encrypt_cert "$domain" "$OUTPUT_PATH"
            else
                # Build SAN string for localhost and IP addresses
                sans=""
                if [ "$domain" = "localhost" ]; then
                    sans="DNS:localhost,DNS:127.0.0.1,IP:127.0.0.1,IP:192.168.139.96"
                elif [ "$domain" = "127.0.0.1" ]; then
                    sans="DNS:localhost,DNS:127.0.0.1,IP:127.0.0.1"
                elif [ "$domain" = "192.168.139.96" ]; then
                    sans="DNS:localhost,DNS:127.0.0.1,IP:127.0.0.1,IP:192.168.139.96"
                fi
                
                generate_self_signed_cert "$domain" "$OUTPUT_PATH" "$sans"
            fi
        done
    fi
done

# Generate wildcard certificate for development
if [ "$ENVIRONMENT" = "all" ] || [ "$ENVIRONMENT" = "development" ]; then
    print_info "🌐 Generating wildcard certificate for development..."
    generate_self_signed_cert "*.warehouse.cuby" "$OUTPUT_PATH" "DNS:warehouse.cuby,DNS:*.warehouse.cuby,DNS:*.staging.warehouse.cuby,DNS:*.test.warehouse.cuby"
fi

# Set proper permissions
print_info "🔒 Setting file permissions..."
find "$OUTPUT_PATH" -name "*.key" -exec chmod 600 {} \;
find "$OUTPUT_PATH" -name "*.crt" -exec chmod 644 {} \;

print_success "✅ SSL certificate generation completed!"
print_info "📁 Certificates location: $OUTPUT_PATH"

# List generated certificates
print_info "📋 Generated certificates:"
for cert_file in "$OUTPUT_PATH"/*.crt; do
    if [ -f "$cert_file" ]; then
        domain=$(basename "$cert_file" .crt)
        key_file="$OUTPUT_PATH/$domain.key"
        
        if [ -f "$key_file" ]; then
            print_success "   ✅ $domain.crt (with private key)"
        else
            print_warning "   ⚠️  $domain.crt (missing private key)"
        fi
    fi
done

if [[ "$USE_LETS_ENCRYPT" != "true" ]]; then
    print_warning "⚠️  Note: These are self-signed certificates for development only."
    print_warning "   For production, use certificates from a trusted CA or Let's Encrypt."
fi

print_info "🚀 Next steps:"
print_info "   1. Restart nginx: docker-compose restart nginx-proxy"
print_info "   2. Test HTTPS: https://localhost or your domain"
print_info "   3. Check certificate: openssl x509 -in \"$OUTPUT_PATH/localhost.crt\" -text -noout"
