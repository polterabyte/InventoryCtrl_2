# Apply-PortConfig.ps1
# Applies port configuration from ports.json to .NET project files

param(
    [string]$ConfigPath = "ports.json",
    [string]$ProjectRoot = ".",
    [switch]$Force
)

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Apply-PortConfiguration {
    param(
        [string]$ConfigPath,
        [string]$ProjectRoot
    )
    
    Write-ColorOutput "=== Applying Port Configuration ===" -Color Green
    
    # Check if config file exists
    if (-not (Test-Path $ConfigPath)) {
        Write-ColorOutput "Error: Port configuration file not found: $ConfigPath" -Color Red
        return $false
    }
    
    try {
        # Load port configuration
        $portConfig = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        Write-ColorOutput "Loaded port configuration from $ConfigPath" -Color Cyan
        
        # Apply to API launchSettings.json
        $apiLaunchSettings = Join-Path $ProjectRoot "src\Inventory.API\Properties\launchSettings.json"
        if (Test-Path $apiLaunchSettings) {
            Write-ColorOutput "Updating API launchSettings.json..." -Color Yellow
            $apiSettings = Get-Content $apiLaunchSettings -Raw | ConvertFrom-Json
            
            # Update HTTP profile
            $apiSettings.profiles.http.applicationUrl = "http://localhost:$($portConfig.api.http)"
            
            # Update HTTPS profile
            $apiSettings.profiles.https.applicationUrl = "https://localhost:$($portConfig.api.https);http://localhost:$($portConfig.api.http)"
            
            $apiSettings | ConvertTo-Json -Depth 10 | Set-Content $apiLaunchSettings -Encoding UTF8
            Write-ColorOutput "API launchSettings.json updated" -Color Green
        }
        
        # Apply to Web launchSettings.json
        $webLaunchSettings = Join-Path $ProjectRoot "src\Inventory.Web\Properties\launchSettings.json"
        if (Test-Path $webLaunchSettings) {
            Write-ColorOutput "Updating Web launchSettings.json..." -Color Yellow
            $webSettings = Get-Content $webLaunchSettings -Raw | ConvertFrom-Json
            
            # Update HTTP profile
            $webSettings.profiles.http.applicationUrl = "http://localhost:$($portConfig.web.http)"
            
            # Update HTTPS profile
            $webSettings.profiles.https.applicationUrl = "https://localhost:$($portConfig.web.https);http://localhost:$($portConfig.web.http)"
            
            $webSettings | ConvertTo-Json -Depth 10 | Set-Content $webLaunchSettings -Encoding UTF8
            Write-ColorOutput "Web launchSettings.json updated" -Color Green
        }
        
        # Apply to API appsettings.json (CORS)
        $apiAppSettings = Join-Path $ProjectRoot "src\Inventory.API\appsettings.json"
        if (Test-Path $apiAppSettings) {
            Write-ColorOutput "Updating API appsettings.json (CORS)..." -Color Yellow
            $apiAppConfig = Get-Content $apiAppSettings -Raw | ConvertFrom-Json
            
            # Update CORS allowed origins
            $apiAppConfig.Cors.AllowedOrigins = @(
                "http://localhost:$($portConfig.api.http)",
                "https://localhost:$($portConfig.api.https)",
                "http://localhost:$($portConfig.web.http)",
                "https://localhost:$($portConfig.web.https)",
                "http://10.0.2.2:8080",
                "capacitor://localhost",
                "https://yourmobileapp.com"
            )
            
            $apiAppConfig | ConvertTo-Json -Depth 10 | Set-Content $apiAppSettings -Encoding UTF8
            Write-ColorOutput "API appsettings.json updated" -Color Green
        }
        
        # Update Web Program.cs to use dynamic API URL
        $webProgramCs = Join-Path $ProjectRoot "src\Inventory.Web\Program.cs"
        if (Test-Path $webProgramCs) {
            Write-ColorOutput "Updating Web Program.cs..." -Color Yellow
            $programContent = Get-Content $webProgramCs -Raw
            
            # Replace hardcoded API URL with dynamic one
            $newApiUrl = "https://localhost:$($portConfig.api.https)"
            $programContent = $programContent -replace 'BaseAddress = new Uri\("http://localhost:5000"\)', "BaseAddress = new Uri(`"$newApiUrl`")"
            
            Set-Content $webProgramCs -Value $programContent -Encoding UTF8
            Write-ColorOutput "Web Program.cs updated with API URL: $newApiUrl" -Color Green
        }
        
        Write-ColorOutput "=== Port Configuration Applied Successfully ===" -Color Green
        return $true
    }
    catch {
        Write-ColorOutput "Error applying port configuration: $($_.Exception.Message)" -Color Red
        return $false
    }
}

# Main execution
if (Apply-PortConfiguration -ConfigPath $ConfigPath -ProjectRoot $ProjectRoot) {
    Write-ColorOutput "Port configuration has been applied to all project files." -Color Green
    Write-ColorOutput "You can now build and run the applications with the new port settings." -Color Cyan
} else {
    Write-ColorOutput "Failed to apply port configuration." -Color Red
    exit 1
}
