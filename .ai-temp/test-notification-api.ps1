# Test Notification API
Write-Host "🧪 Testing Notification API..." -ForegroundColor Yellow

$apiUrl = "https://localhost:7000"
$testUserId = "test-user-123"

try {
    # Test getting notification stats
    Write-Host "📊 Testing notification stats endpoint..." -ForegroundColor Cyan
    $statsResponse = Invoke-RestMethod -Uri "$apiUrl/api/notifications/stats?userId=$testUserId" -Method GET -SkipCertificateCheck
    Write-Host "✅ Stats endpoint working: $($statsResponse | ConvertTo-Json)" -ForegroundColor Green
    
    # Test getting user notifications
    Write-Host "📋 Testing user notifications endpoint..." -ForegroundColor Cyan
    $notificationsResponse = Invoke-RestMethod -Uri "$apiUrl/api/notifications/user/$testUserId?page=1&pageSize=10" -Method GET -SkipCertificateCheck
    Write-Host "✅ Notifications endpoint working: $($notificationsResponse | ConvertTo-Json)" -ForegroundColor Green
    
} catch {
    Write-Host "❌ API test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
}

Write-Host "🎉 API testing completed!" -ForegroundColor Green
