# PowerShell script to generate VAPID keys for Production deployment
# This script generates VAPID keys and updates only production environment files

param(
    [string]$Environment = "production"
)

Write-Host "Generating VAPID keys for $Environment environment..." -ForegroundColor Green

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "Node.js version: $nodeVersion" -ForegroundColor Yellow
} catch {
    Write-Host "Node.js is not installed. Please install Node.js first." -ForegroundColor Red
    Write-Host "Download from: https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Check if web-push package is installed globally
try {
    $webPushVersion = npm list -g web-push --depth=0
    if ($webPushVersion -match "web-push@") {
        Write-Host "web-push package is installed globally" -ForegroundColor Yellow
    } else {
        Write-Host "Installing web-push package globally..." -ForegroundColor Yellow
        npm install -g web-push
    }
} catch {
    Write-Host "Installing web-push package globally..." -ForegroundColor Yellow
    npm install -g web-push
}

# Generate VAPID keys
Write-Host "Generating VAPID keys..." -ForegroundColor Yellow
$vapidKeys = node -e "const webpush = require('web-push'); const keys = webpush.generateVAPIDKeys(); console.log(JSON.stringify(keys));"

# Parse the JSON output
$keys = $vapidKeys | ConvertFrom-Json

Write-Host "VAPID Keys generated successfully!" -ForegroundColor Green
Write-Host "Public Key: $($keys.publicKey)" -ForegroundColor Cyan
Write-Host "Private Key: $($keys.privateKey)" -ForegroundColor Cyan

# Update environment file based on parameter
$envFile = switch ($Environment.ToLower()) {
    "production" { "env.production" }
    "staging" { "env.staging" }
    "test" { "env.test" }
    default { "env.production" }
}

if (Test-Path $envFile) {
    Write-Host "Updating $envFile..." -ForegroundColor Yellow
    
    $envContent = Get-Content $envFile -Raw
    # Use more specific regex to avoid capturing separators
    $envContent = $envContent -replace "VAPID_PUBLIC_KEY=.*$", "VAPID_PUBLIC_KEY=$($keys.publicKey)"
    $envContent = $envContent -replace "VAPID_PRIVATE_KEY=.*$", "VAPID_PRIVATE_KEY=$($keys.privateKey)"
    Set-Content $envFile $envContent
    Write-Host "$envFile updated successfully!" -ForegroundColor Green
} else {
    Write-Host "$envFile not found!" -ForegroundColor Red
    exit 1
}

# Update Service Worker with public key
$swPath = "src/Inventory.Web.Client/wwwroot/sw.js"
if (Test-Path $swPath) {
    Write-Host "Updating Service Worker with public key..." -ForegroundColor Yellow
    
    $swContent = Get-Content $swPath -Raw
    $swContent = $swContent -replace "YOUR_VAPID_PUBLIC_KEY_HERE", $keys.publicKey
    Set-Content $swPath $swContent
    Write-Host "Service Worker updated successfully!" -ForegroundColor Green
} else {
    Write-Host "Service Worker not found at $swPath" -ForegroundColor Yellow
}

# Update Push Notifications JavaScript with public key
$pushJsPath = "src/Inventory.Web.Client/wwwroot/js/push-notifications.js"
if (Test-Path $pushJsPath) {
    Write-Host "Updating Push Notifications JavaScript with public key..." -ForegroundColor Yellow
    
    $pushJsContent = Get-Content $pushJsPath -Raw
    $pushJsContent = $pushJsContent -replace "YOUR_VAPID_PUBLIC_KEY_HERE", $keys.publicKey
    Set-Content $pushJsPath $pushJsContent
    Write-Host "Push Notifications JavaScript updated successfully!" -ForegroundColor Green
} else {
    Write-Host "Push Notifications JavaScript not found at $pushJsPath" -ForegroundColor Yellow
}

Write-Host "`nVAPID keys generation for $Environment completed!" -ForegroundColor Green
Write-Host "`nIMPORTANT SECURITY NOTES:" -ForegroundColor Red
Write-Host "1. Keep the private key secure and never commit it to version control" -ForegroundColor Yellow
Write-Host "2. The private key is now stored in $envFile" -ForegroundColor Yellow
Write-Host "3. The public key can be safely shared with clients" -ForegroundColor Yellow
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Deploy with Docker using: docker-compose -f docker-compose.prod.yml up -d" -ForegroundColor White
Write-Host "2. Verify VAPID configuration in production" -ForegroundColor White
Write-Host "3. Test push notifications functionality" -ForegroundColor White

# Show the generated keys for manual verification
Write-Host "`nGenerated VAPID Keys:" -ForegroundColor Cyan
Write-Host "Public Key: $($keys.publicKey)" -ForegroundColor White
Write-Host "Private Key: $($keys.privateKey)" -ForegroundColor White
