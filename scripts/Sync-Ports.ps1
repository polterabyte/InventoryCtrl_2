# Sync-Ports.ps1
# Synchronizes ports between ports.json and appsettings files

param(
    [string]$ConfigPath = "ports.json",
    [string]$ProjectRoot = ".",
    [switch]$FromAppSettings,
    [switch]$ToAppSettings
)

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Sync-FromPortsJson {
    param(
        [string]$ConfigPath,
        [string]$ProjectRoot
    )
    
    Write-ColorOutput "=== Syncing from ports.json to appsettings ===" -Color Green
    
    if (-not (Test-Path $ConfigPath)) {
        Write-ColorOutput "Error: ports.json not found" -Color Red
        return $false
    }
    
    try {
        $portConfig = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        
        # Note: Ports are now read directly from ports.json at runtime
        # No need to create separate appsettings.Ports.json file
        Write-ColorOutput "Ports will be read directly from ports.json at runtime" -Color Green
        
        return $true
    }
    catch {
        Write-ColorOutput "Error syncing from ports.json: $($_.Exception.Message)" -Color Red
        return $false
    }
}

function Sync-ToPortsJson {
    param(
        [string]$ProjectRoot
    )
    
    Write-ColorOutput "=== Syncing from appsettings to ports.json ===" -Color Green
    
    $apiPortsSettings = Join-Path $ProjectRoot "src\Inventory.API\appsettings.Ports.json"
    if (-not (Test-Path $apiPortsSettings)) {
        Write-ColorOutput "Error: appsettings.Ports.json not found" -Color Red
        return $false
    }
    
    try {
        $apiPortsConfig = Get-Content $apiPortsSettings -Raw | ConvertFrom-Json
        
        # Create ports.json from appsettings
        $portConfig = @{
            api = @{
                http = $apiPortsConfig.Ports.Api.Http
                https = $apiPortsConfig.Ports.Api.Https
                urls = "https://localhost:$($apiPortsConfig.Ports.Api.Https);http://localhost:$($apiPortsConfig.Ports.Api.Http)"
            }
            web = @{
                http = $apiPortsConfig.Ports.Web.Http
                https = $apiPortsConfig.Ports.Web.Https
                urls = "https://localhost:$($apiPortsConfig.Ports.Web.Https);http://localhost:$($apiPortsConfig.Ports.Web.Http)"
            }
            database = @{
                port = 5432
            }
            cors = @{
                allowedOrigins = $apiPortsConfig.Cors.AllowedOrigins
            }
            launchUrls = @{
                api = "https://localhost:$($apiPortsConfig.Ports.Api.Https)"
                web = "https://localhost:$($apiPortsConfig.Ports.Web.Https)"
            }
        }
        
        $portConfig | ConvertTo-Json -Depth 10 | Set-Content "ports.json" -Encoding UTF8
        Write-ColorOutput "Updated ports.json" -Color Green
        
        return $true
    }
    catch {
        Write-ColorOutput "Error syncing to ports.json: $($_.Exception.Message)" -Color Red
        return $false
    }
}

# Main execution
if ($FromAppSettings) {
    if (Sync-ToPortsJson -ProjectRoot $ProjectRoot) {
        Write-ColorOutput "Successfully synced from appsettings to ports.json" -Color Green
    } else {
        Write-ColorOutput "Failed to sync from appsettings" -Color Red
        exit 1
    }
} elseif ($ToAppSettings) {
    if (Sync-FromPortsJson -ConfigPath $ConfigPath -ProjectRoot $ProjectRoot) {
        Write-ColorOutput "Successfully synced from ports.json to appsettings" -Color Green
    } else {
        Write-ColorOutput "Failed to sync from ports.json" -Color Red
        exit 1
    }
} else {
    # Default: sync from ports.json to appsettings
    if (Sync-FromPortsJson -ConfigPath $ConfigPath -ProjectRoot $ProjectRoot) {
        Write-ColorOutput "Successfully synced from ports.json to appsettings" -Color Green
    } else {
        Write-ColorOutput "Failed to sync from ports.json" -Color Red
        exit 1
    }
}
