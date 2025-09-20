# Test script for server configuration
# Tests API accessibility and CORS settings

param(
    [string]$ServerIP = "192.168.139.96"
)

Write-Host "Testing server configuration for IP: $ServerIP" -ForegroundColor Green

# Test 1: Health check
Write-Host "`n1. Testing health endpoint..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "http://$ServerIP/health" -Method GET -TimeoutSec 10
    Write-Host "✓ Health check passed" -ForegroundColor Green
    Write-Host "Response: $($healthResponse | ConvertTo-Json)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: API endpoint
Write-Host "`n2. Testing API endpoint..." -ForegroundColor Yellow
try {
    $apiResponse = Invoke-RestMethod -Uri "http://$ServerIP/api/" -Method GET -TimeoutSec 10
    Write-Host "✓ API endpoint accessible" -ForegroundColor Green
} catch {
    Write-Host "✗ API endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: CORS preflight request
Write-Host "`n3. Testing CORS preflight..." -ForegroundColor Yellow
try {
    $headers = @{
        "Origin" = "http://$ServerIP"
        "Access-Control-Request-Method" = "POST"
        "Access-Control-Request-Headers" = "Content-Type,Authorization"
    }
    $corsResponse = Invoke-WebRequest -Uri "http://$ServerIP/api/" -Method OPTIONS -Headers $headers -TimeoutSec 10
    Write-Host "✓ CORS preflight passed" -ForegroundColor Green
    Write-Host "CORS Headers:" -ForegroundColor Cyan
    $corsResponse.Headers | Where-Object { $_.Key -like "*Access-Control*" } | ForEach-Object {
        Write-Host "  $($_.Key): $($_.Value)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "✗ CORS preflight failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: POST request (if API supports it)
Write-Host "`n4. Testing POST request..." -ForegroundColor Yellow
try {
    $postData = @{
        username = "test"
        password = "test"
    } | ConvertTo-Json
    
    $postResponse = Invoke-RestMethod -Uri "http://$ServerIP/api/auth/login" -Method POST -Body $postData -ContentType "application/json" -TimeoutSec 10
    Write-Host "✓ POST request successful" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✓ POST request works (401 expected for invalid credentials)" -ForegroundColor Green
    } else {
        Write-Host "✗ POST request failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nConfiguration test completed!" -ForegroundColor Green
