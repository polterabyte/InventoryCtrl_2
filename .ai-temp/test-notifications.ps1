# Test Notifications Page
Write-Host "🧪 Testing Notifications Page..." -ForegroundColor Yellow

# Wait a bit for the application to fully start
Start-Sleep -Seconds 5

# Open the notifications page in the default browser
$notificationsUrl = "https://localhost:7001/notifications"
Write-Host "🌐 Opening notifications page: $notificationsUrl" -ForegroundColor Cyan

try {
    Start-Process $notificationsUrl
    Write-Host "✅ Notifications page opened successfully!" -ForegroundColor Green
    Write-Host "Please check if the page loads without the INotificationService error." -ForegroundColor Cyan
} catch {
    Write-Host "❌ Failed to open notifications page: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "🔍 You can also manually navigate to: $notificationsUrl" -ForegroundColor Cyan
