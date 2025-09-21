# Test SignalR Connection Script
# This script tests the SignalR connection to verify it's working properly

Write-Host "🧪 Testing SignalR Connection..." -ForegroundColor Yellow

# Test 1: Check if nginx is responding
Write-Host "`n1️⃣ Testing nginx response..." -ForegroundColor Blue
try {
    $response = Invoke-WebRequest -Uri "https://staging.warehouse.cuby/health" -TimeoutSec 10
    Write-Host "✅ Nginx is responding: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ Nginx not responding: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Check if API is responding
Write-Host "`n2️⃣ Testing API response..." -ForegroundColor Blue
try {
    $response = Invoke-WebRequest -Uri "https://staging.warehouse.cuby/api/health" -TimeoutSec 10
    Write-Host "✅ API is responding: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ API not responding: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Check SignalR endpoint
Write-Host "`n3️⃣ Testing SignalR endpoint..." -ForegroundColor Blue
try {
    $response = Invoke-WebRequest -Uri "https://staging.warehouse.cuby/notificationHub" -TimeoutSec 10
    Write-Host "✅ SignalR endpoint is responding: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ SignalR endpoint not responding: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Check WebSocket upgrade
Write-Host "`n4️⃣ Testing WebSocket upgrade..." -ForegroundColor Blue
try {
    $headers = @{
        "Upgrade" = "websocket"
        "Connection" = "Upgrade"
        "Sec-WebSocket-Key" = "dGhlIHNhbXBsZSBub25jZQ=="
        "Sec-WebSocket-Version" = "13"
    }
    $response = Invoke-WebRequest -Uri "https://staging.warehouse.cuby/notificationHub" -Headers $headers -TimeoutSec 10
    Write-Host "✅ WebSocket upgrade supported: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ WebSocket upgrade failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎯 Test completed! Check the results above." -ForegroundColor Cyan
Write-Host "🌐 Open https://staging.warehouse.cuby in your browser to test SignalR in the UI" -ForegroundColor Cyan
