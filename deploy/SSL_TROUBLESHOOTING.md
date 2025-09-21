# SSL Deployment Troubleshooting Guide

## 🚨 Common Issues and Solutions

### 1. **Script Fails with "Docker not running" Error**

**Problem:** `Docker is not running or not accessible`

**Solutions:**
```powershell
# Check if Docker Desktop is running
docker version

# If not running, start Docker Desktop
# Or restart Docker Desktop service
Restart-Service docker
```

### 2. **Script Fails with "Required files missing" Error**

**Problem:** Missing `docker-compose.ssl.yml`, `generate-ssl-linux.sh`, or `Dockerfile.ssl`

**Solutions:**
```powershell
# Check if files exist
Test-Path docker-compose.ssl.yml
Test-Path scripts\generate-ssl-linux.sh
Test-Path src\Inventory.API\Dockerfile.ssl

# If missing, ensure you're in the project root directory
# All files should be present after running the SSL setup
```

### 3. **Script Fails with "Port already in use" Error**

**Problem:** Ports 80, 443, 5000, or 5432 are already in use

**Solutions:**
```powershell
# Check what's using the ports
netstat -an | findstr ':80 :443 :5000 :5432'

# Stop conflicting services
# Or modify ports in docker-compose.ssl.yml
```

### 4. **Script Fails with "Docker Compose command failed" Error**

**Problem:** Docker Compose syntax errors or build failures

**Solutions:**
```powershell
# Validate Docker Compose syntax
docker-compose -f docker-compose.ssl.yml config

# Check build logs
docker-compose -f docker-compose.ssl.yml build --no-cache

# Check specific service logs
docker-compose -f docker-compose.ssl.yml logs inventory-api
```

### 5. **SSL Certificates Not Generated**

**Problem:** SSL generator container fails or certificates not created

**Solutions:**
```powershell
# Check SSL generator logs
docker-compose -f docker-compose.ssl.yml logs ssl-generator

# Manually run SSL generation
docker exec inventory-api /usr/local/bin/generate-ssl-linux.sh --environment development --verbose

# Check SSL volume
docker volume inspect inventoryctrl_2_ssl_certificates
```

### 6. **Services Not Starting**

**Problem:** API or Web Client containers fail to start

**Solutions:**
```powershell
# Check all service logs
docker-compose -f docker-compose.ssl.yml logs

# Check specific service
docker-compose -f docker-compose.ssl.yml logs inventory-api
docker-compose -f docker-compose.ssl.yml logs inventory-web

# Restart specific service
docker-compose -f docker-compose.ssl.yml restart inventory-api
```

## 🔧 Quick Fix Commands

### **Run Diagnostics**
```powershell
# Comprehensive diagnostics
.\deploy\diagnose-ssl.ps1 -Verbose

# Quick fixes
.\deploy\fix-ssl-deployment.ps1 -Verbose
```

### **Clean and Retry**
```powershell
# Clean everything and retry
.\deploy\deploy-with-ssl.ps1 -Clean -Verbose

# Force clean Docker system
docker system prune -a -f
docker volume prune -f
```

### **Manual SSL Generation**
```powershell
# Generate SSL certificates manually
docker exec inventory-api /usr/local/bin/generate-ssl-linux.sh --environment development --force --verbose

# Check generated certificates
docker exec inventory-nginx-proxy ls -la /etc/nginx/ssl/
```

## 🐛 Debug Mode

### **Run with Maximum Verbosity**
```powershell
# Enable verbose output
.\deploy\deploy-with-ssl.ps1 -Verbose

# Check Docker logs in real-time
docker-compose -f docker-compose.ssl.yml logs -f
```

### **Step-by-Step Debugging**
```powershell
# 1. Check Docker
docker version

# 2. Check files
Test-Path docker-compose.ssl.yml
Test-Path scripts\generate-ssl-linux.sh

# 3. Check ports
netstat -an | findstr ':80 :443 :5000 :5432'

# 4. Test Docker Compose
docker-compose -f docker-compose.ssl.yml config

# 5. Build only
docker-compose -f docker-compose.ssl.yml build

# 6. Run step by step
docker-compose -f docker-compose.ssl.yml up ssl-generator
docker-compose -f docker-compose.ssl.yml up -d
```

## 📋 Environment-Specific Issues

### **Windows-Specific Issues**

1. **PowerShell Execution Policy**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

2. **Windows Defender/Antivirus**
- Add Docker Desktop to exclusions
- Add project folder to exclusions

3. **Hyper-V Issues**
```powershell
# Enable Hyper-V
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All

# Restart required
```

### **Docker Desktop Issues**

1. **Docker Desktop Not Starting**
- Restart Docker Desktop
- Check Windows features (Hyper-V, WSL2)
- Update Docker Desktop

2. **Resource Issues**
```powershell
# Check Docker resources
docker system df
docker system prune -f
```

## 🔍 Advanced Troubleshooting

### **Check Container Health**
```powershell
# Check all containers
docker ps -a

# Check container logs
docker logs inventory-api
docker logs inventory-nginx-proxy

# Check container resources
docker stats
```

### **Check SSL Certificates**
```powershell
# List SSL certificates
docker exec inventory-nginx-proxy ls -la /etc/nginx/ssl/

# Check certificate details
docker exec inventory-nginx-proxy openssl x509 -in /etc/nginx/ssl/localhost.crt -text -noout

# Test SSL connection
curl -k https://localhost
```

### **Check Network Connectivity**
```powershell
# Test internal network
docker exec inventory-api ping inventory-postgres
docker exec inventory-web ping inventory-api

# Check port mappings
docker port inventory-nginx-proxy
```

## 📞 Getting Help

### **Collect Debug Information**
```powershell
# Run comprehensive diagnostics
.\deploy\diagnose-ssl.ps1 -Verbose > ssl-debug.log

# Collect Docker logs
docker-compose -f docker-compose.ssl.yml logs > docker-logs.log

# Check system info
docker version > docker-version.log
docker system info > docker-info.log
```

### **Common Error Messages**

| Error | Cause | Solution |
|-------|-------|----------|
| `Docker is not running` | Docker Desktop not started | Start Docker Desktop |
| `Port already in use` | Another service using port | Stop conflicting service |
| `Required files missing` | Files not in correct location | Check file paths |
| `Docker Compose failed` | Syntax error in YAML | Validate docker-compose.ssl.yml |
| `SSL script not found` | Linux script missing | Ensure script exists |
| `Certificate generation failed` | OpenSSL issues | Check container logs |

## 🎯 Success Indicators

### **Deployment Successful When:**
- ✅ All containers are running (`docker ps`)
- ✅ API responds at `http://localhost:5000/health`
- ✅ Web Client responds at `http://localhost/health`
- ✅ HTTPS works at `https://localhost` (with security warning)
- ✅ SSL certificates exist in volume
- ✅ No error messages in logs

### **Test Commands**
```powershell
# Test API
Invoke-WebRequest -Uri "http://localhost:5000/health"

# Test Web Client
Invoke-WebRequest -Uri "http://localhost/health"

# Test HTTPS (ignore certificate warning)
Invoke-WebRequest -Uri "https://localhost" -SkipCertificateCheck

# Test SSL API
Invoke-WebRequest -Uri "https://localhost:5000/api/SSLCertificate" -SkipCertificateCheck
```
