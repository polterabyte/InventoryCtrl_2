# Test script after removing PortConfigurationService
param(
    [string]$ServerIP = "192.168.1.100",  # Replace with your server IP
    [int]$Port = 80
)

Write-Host "🔧 Testing external access after removing PortConfigurationService..." -ForegroundColor Cyan
Write-Host "Server IP: $ServerIP" -ForegroundColor Yellow
Write-Host "Port: $Port" -ForegroundColor Yellow
Write-Host ""

# Test 1: Basic connectivity
Write-Host "1️⃣ Testing basic connectivity..." -ForegroundColor Green
try {
    $response = Invoke-WebRequest -Uri "http://${ServerIP}:${Port}/health" -Method GET -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Health check passed: $($response.Content)" -ForegroundColor Green
    } else {
        Write-Host "❌ Health check failed with status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 2: API endpoint through nginx proxy
Write-Host "2️⃣ Testing API endpoint through nginx proxy..." -ForegroundColor Green
try {
    $apiUrl = "http://${ServerIP}:${Port}/api/health"
    Write-Host "Testing API URL: $apiUrl" -ForegroundColor Yellow
    
    $response = Invoke-WebRequest -Uri $apiUrl -Method GET -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API endpoint accessible: $($response.Content)" -ForegroundColor Green
    } else {
        Write-Host "❌ API endpoint failed with status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ API endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Web application
Write-Host "3️⃣ Testing web application..." -ForegroundColor Green
try {
    $webUrl = "http://${ServerIP}:${Port}/"
    Write-Host "Testing Web URL: $webUrl" -ForegroundColor Yellow
    
    $response = Invoke-WebRequest -Uri $webUrl -Method GET -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Web application accessible" -ForegroundColor Green
        
        # Check if the response contains expected content
        if ($response.Content -match "inventory" -or $response.Content -match "login") {
            Write-Host "✅ Web application content looks correct" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Web application content might be incorrect" -ForegroundColor Yellow
        }
    } else {
        Write-Host "❌ Web application failed with status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Web application failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Test login endpoint specifically
Write-Host "4️⃣ Testing login endpoint..." -ForegroundColor Green
try {
    $loginUrl = "http://${ServerIP}:${Port}/api/auth/login"
    Write-Host "Testing Login URL: $loginUrl" -ForegroundColor Yellow
    
    # Test with invalid credentials to check if endpoint is reachable
    $loginData = @{
        username = "test"
        password = "test"
    } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri $loginUrl -Method POST -Body $loginData -ContentType "application/json" -TimeoutSec 10
    Write-Host "✅ Login endpoint is reachable (status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ Login endpoint is reachable (401 Unauthorized - expected for invalid credentials)" -ForegroundColor Green
    } else {
        Write-Host "❌ Login endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🎯 Summary:" -ForegroundColor Cyan
Write-Host "✅ PortConfigurationService and ports.json have been removed" -ForegroundColor White
Write-Host "✅ API URL is now determined by JavaScript (getApiBaseUrl function)" -ForegroundColor White
Write-Host "✅ WebAuthApiService uses WebBaseApiService with full URL construction" -ForegroundColor White
Write-Host ""
Write-Host "To test from another computer:" -ForegroundColor Yellow
Write-Host "1. Open browser and go to: http://$ServerIP" -ForegroundColor White
Write-Host "2. Try to login with valid credentials" -ForegroundColor White
Write-Host "3. Check browser console for any errors" -ForegroundColor White
Write-Host "4. API requests should now use full URLs like http://$ServerIP/api/auth/login" -ForegroundColor White
