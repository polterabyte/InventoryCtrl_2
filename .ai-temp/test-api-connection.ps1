# Test API connectivity from Docker container
Write-Host "Testing API connectivity..." -ForegroundColor Green

# Test API health endpoint
Write-Host "1. Testing API health endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method GET -TimeoutSec 10
    Write-Host "✅ API Health check passed: $($response | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ API Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test API auth endpoint
Write-Host "2. Testing API auth endpoint..." -ForegroundColor Yellow
try {
    $loginData = @{
        username = "admin"
        password = "admin123"
    } | ConvertTo-Json -Compress

    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method POST -Body $loginData -ContentType "application/json" -TimeoutSec 10
    Write-Host "✅ API Auth endpoint accessible: $($response | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ API Auth endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test CORS preflight
Write-Host "3. Testing CORS preflight..." -ForegroundColor Yellow
try {
    $headers = @{
        "Origin" = "http://localhost"
        "Access-Control-Request-Method" = "POST"
        "Access-Control-Request-Headers" = "Content-Type"
    }
    
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" -Method OPTIONS -Headers $headers -TimeoutSec 10
    Write-Host "✅ CORS preflight successful: Status $($response.StatusCode)" -ForegroundColor Green
    Write-Host "CORS Headers: $($response.Headers | ConvertTo-Json)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ CORS preflight failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "API connectivity test completed." -ForegroundColor Green
