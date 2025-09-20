# PowerShell script to generate VAPID keys for Web Push Notifications
# This script generates VAPID keys and updates the configuration files

Write-Host "Generating VAPID keys for Web Push Notifications..." -ForegroundColor Green

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

# Update appsettings.json
$appSettingsPath = "src/Inventory.API/appsettings.json"
if (Test-Path $appSettingsPath) {
    Write-Host "Updating appsettings.json..." -ForegroundColor Yellow
    
    $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
    $appSettings.Vapid.PublicKey = $keys.publicKey
    $appSettings.Vapid.PrivateKey = $keys.privateKey
    
    $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
    Write-Host "appsettings.json updated successfully!" -ForegroundColor Green
} else {
    Write-Host "appsettings.json not found at $appSettingsPath" -ForegroundColor Red
}

# Update appsettings.Development.json
$devSettingsPath = "src/Inventory.API/appsettings.Development.json"
if (Test-Path $devSettingsPath) {
    Write-Host "Updating appsettings.Development.json..." -ForegroundColor Yellow
    
    $devSettings = Get-Content $devSettingsPath | ConvertFrom-Json
    if (-not $devSettings.Vapid) {
        $devSettings | Add-Member -Type NoteProperty -Name "Vapid" -Value @{}
    }
    $devSettings.Vapid.PublicKey = $keys.publicKey
    $devSettings.Vapid.PrivateKey = $keys.privateKey
    
    $devSettings | ConvertTo-Json -Depth 10 | Set-Content $devSettingsPath
    Write-Host "appsettings.Development.json updated successfully!" -ForegroundColor Green
} else {
    Write-Host "appsettings.Development.json not found at $devSettingsPath" -ForegroundColor Yellow
}

# Update appsettings.Production.json
$prodSettingsPath = "src/Inventory.API/appsettings.Production.json"
if (Test-Path $prodSettingsPath) {
    Write-Host "Updating appsettings.Production.json..." -ForegroundColor Yellow
    
    $prodSettings = Get-Content $prodSettingsPath | ConvertFrom-Json
    if (-not $prodSettings.Vapid) {
        $prodSettings | Add-Member -Type NoteProperty -Name "Vapid" -Value @{}
    }
    $prodSettings.Vapid.PublicKey = $keys.publicKey
    $prodSettings.Vapid.PrivateKey = $keys.privateKey
    
    $prodSettings | ConvertTo-Json -Depth 10 | Set-Content $prodSettingsPath
    Write-Host "appsettings.Production.json updated successfully!" -ForegroundColor Green
} else {
    Write-Host "appsettings.Production.json not found at $prodSettingsPath" -ForegroundColor Yellow
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

# Update environment files
Write-Host "Updating environment files..." -ForegroundColor Yellow

# Update env.production
$envProdPath = "deploy/env.production"
if (Test-Path $envProdPath) {
    Write-Host "Updating env.production..." -ForegroundColor Yellow
    $envProdContent = Get-Content $envProdPath -Raw
    $envProdContent = $envProdContent -replace "VAPID_PUBLIC_KEY=.*$", "VAPID_PUBLIC_KEY=$($keys.publicKey)"
    $envProdContent = $envProdContent -replace "VAPID_PRIVATE_KEY=.*$", "VAPID_PRIVATE_KEY=$($keys.privateKey)"
    Set-Content $envProdPath $envProdContent
    Write-Host "env.production updated successfully!" -ForegroundColor Green
}

# Update env.staging
$envStagingPath = "deploy/env.staging"
if (Test-Path $envStagingPath) {
    Write-Host "Updating env.staging..." -ForegroundColor Yellow
    $envStagingContent = Get-Content $envStagingPath -Raw
    $envStagingContent = $envStagingContent -replace "VAPID_PUBLIC_KEY=.*$", "VAPID_PUBLIC_KEY=$($keys.publicKey)"
    $envStagingContent = $envStagingContent -replace "VAPID_PRIVATE_KEY=.*$", "VAPID_PRIVATE_KEY=$($keys.privateKey)"
    Set-Content $envStagingPath $envStagingContent
    Write-Host "env.staging updated successfully!" -ForegroundColor Green
}

# Update env.test
$envTestPath = "deploy/env.test"
if (Test-Path $envTestPath) {
    Write-Host "Updating env.test..." -ForegroundColor Yellow
    $envTestContent = Get-Content $envTestPath -Raw
    $envTestContent = $envTestContent -replace "VAPID_PUBLIC_KEY=.*$", "VAPID_PUBLIC_KEY=$($keys.publicKey)"
    $envTestContent = $envTestContent -replace "VAPID_PRIVATE_KEY=.*$", "VAPID_PRIVATE_KEY=$($keys.privateKey)"
    Set-Content $envTestPath $envTestContent
    Write-Host "env.test updated successfully!" -ForegroundColor Green
}

Write-Host "`nVAPID keys generation completed!" -ForegroundColor Green
Write-Host "`nIMPORTANT SECURITY NOTES:" -ForegroundColor Red
Write-Host "1. Keep the private key secure and never commit it to version control" -ForegroundColor Yellow
Write-Host "2. Use environment variables for production deployments" -ForegroundColor Yellow
Write-Host "3. The public key can be safely shared with clients" -ForegroundColor Yellow
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Run database migration to add PushSubscriptions table" -ForegroundColor White
Write-Host "2. Test push notifications in development" -ForegroundColor White
Write-Host "3. Deploy with Docker using updated environment files" -ForegroundColor White
Write-Host "4. Verify VAPID configuration in production" -ForegroundColor White
