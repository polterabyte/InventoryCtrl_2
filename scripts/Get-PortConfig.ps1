# Get-PortConfig.ps1
# Utility script to read port configuration from ports.json

param(
    [string]$ConfigPath = "ports.json"
)

function Get-PortConfig {
    param([string]$Path = $ConfigPath)
    
    if (-not (Test-Path $Path)) {
        Write-Error "Port configuration file not found: $Path"
        return $null
    }
    
    try {
        $config = Get-Content $Path -Raw | ConvertFrom-Json
        return $config
    }
    catch {
        Write-Error "Failed to parse port configuration: $($_.Exception.Message)"
        return $null
    }
}

function Get-ApiPorts {
    param([string]$Path = $ConfigPath)
    
    $config = Get-PortConfig -Path $Path
    if ($config -and $config.api) {
        return @{
            Http = $config.api.http
            Https = $config.api.https
            Urls = $config.api.urls
        }
    }
    return $null
}

function Get-WebPorts {
    param([string]$Path = $ConfigPath)
    
    $config = Get-PortConfig -Path $Path
    if ($config -and $config.web) {
        return @{
            Http = $config.web.http
            Https = $config.web.https
            Urls = $config.web.urls
        }
    }
    return $null
}

function Get-LaunchUrls {
    param([string]$Path = $ConfigPath)
    
    $config = Get-PortConfig -Path $Path
    if ($config -and $config.launchUrls) {
        return $config.launchUrls
    }
    return $null
}

# Export functions for use in other scripts
Export-ModuleMember -Function Get-PortConfig, Get-ApiPorts, Get-WebPorts, Get-LaunchUrls
