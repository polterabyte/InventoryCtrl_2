# Restart Staging Containers Script
# This script restarts the staging containers to apply SignalR configuration changes

Write-Host "🔄 Restarting Staging Containers..." -ForegroundColor Yellow

# Stop staging containers
Write-Host "⏹️ Stopping staging containers..." -ForegroundColor Blue
docker-compose -f docker-compose.staging.yml down

# Wait a moment
Start-Sleep -Seconds 3

# Start staging containers
Write-Host "▶️ Starting staging containers..." -ForegroundColor Blue
docker-compose -f docker-compose.staging.yml up -d

# Wait for containers to start
Write-Host "⏳ Waiting for containers to start..." -ForegroundColor Blue
Start-Sleep -Seconds 10

# Check container status
Write-Host "📊 Container Status:" -ForegroundColor Green
docker-compose -f docker-compose.staging.yml ps

# Check logs for any errors
Write-Host "📋 Checking container logs for errors..." -ForegroundColor Blue
Write-Host "API Container Logs:" -ForegroundColor Yellow
docker logs inventory-api-staging --tail 20

Write-Host "Nginx Container Logs:" -ForegroundColor Yellow
docker logs inventory-nginx-staging --tail 20

Write-Host "✅ Staging containers restarted successfully!" -ForegroundColor Green
Write-Host "🌐 You can now test SignalR connection at: https://staging.warehouse.cuby" -ForegroundColor Cyan
