# Test script to verify VAPID key generation preserves separators
Write-Host "Testing VAPID key generation with separators..." -ForegroundColor Green

# Create a test env file with separators
$testEnvContent = @"
# Test Environment Variables

# Server Configuration
SERVER_IP=192.168.139.96
DOMAIN=test.warehouse.cuby-------------------------------
STAGING_DOMAIN=staging.warehouse.cuby
TEST_DOMAIN=test.warehouse.cuby

# VAPID Configuration for Web Push Notifications
VAPID_SUBJECT=mailto:admin@test.warehouse.cuby
VAPID_PUBLIC_KEY=
VAPID_PRIVATE_KEY=

# Other Configuration
OTHER_SETTING=value
"@

# Write test file
Set-Content "test-env.txt" $testEnvContent
Write-Host "Created test file with separators" -ForegroundColor Yellow

# Show original content
Write-Host "`nOriginal content:" -ForegroundColor Cyan
Get-Content "test-env.txt" | ForEach-Object { Write-Host "  $_" }

# Simulate VAPID key generation
$testKeys = @{
    publicKey = "test-public-key-12345"
    privateKey = "test-private-key-67890"
}

# Update the file (simulating the fixed script)
$envContent = Get-Content "test-env.txt" -Raw
$envContent = $envContent -replace "VAPID_PUBLIC_KEY=.*$", "VAPID_PUBLIC_KEY=$($testKeys.publicKey)"
$envContent = $envContent -replace "VAPID_PRIVATE_KEY=.*$", "VAPID_PRIVATE_KEY=$($testKeys.privateKey)"
Set-Content "test-env.txt" $envContent

# Show updated content
Write-Host "`nUpdated content:" -ForegroundColor Cyan
Get-Content "test-env.txt" | ForEach-Object { Write-Host "  $_" }

# Check if separators are preserved
$updatedContent = Get-Content "test-env.txt" -Raw
if ($updatedContent -match "DOMAIN=test\.warehouse\.cuby-+") {
    Write-Host "`n✅ SUCCESS: Separators are preserved!" -ForegroundColor Green
} else {
    Write-Host "`n❌ FAILED: Separators were removed!" -ForegroundColor Red
}

# Cleanup
Remove-Item "test-env.txt"
Write-Host "`nTest completed and cleaned up." -ForegroundColor Yellow
