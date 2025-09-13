# Watch-Ports.ps1
# Watches for changes in ports.json and automatically applies them

param(
    [string]$ConfigPath = "ports.json",
    [string]$ProjectRoot = ".",
    [int]$IntervalSeconds = 2
)

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Get-FileHash {
    param([string]$FilePath)
    if (Test-Path $FilePath) {
        return (Get-FileHash $FilePath -Algorithm MD5).Hash
    }
    return $null
}

Write-ColorOutput "=== Port Configuration Watcher ===" -Color Green
Write-ColorOutput "Watching for changes in: $ConfigPath" -Color Cyan
Write-ColorOutput "Press Ctrl+C to stop watching" -Color Yellow
Write-Host ""

$lastHash = Get-FileHash $ConfigPath

while ($true) {
    try {
        $currentHash = Get-FileHash $ConfigPath
        
        if ($currentHash -and $currentHash -ne $lastHash) {
            Write-ColorOutput "Detected changes in $ConfigPath" -Color Yellow
            Write-ColorOutput "Applying port configuration..." -Color Cyan
            
            # Apply port configuration
            & "$PSScriptRoot\Apply-PortConfig.ps1" -ConfigPath $ConfigPath -ProjectRoot $ProjectRoot
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorOutput "Port configuration applied successfully" -Color Green
            } else {
                Write-ColorOutput "Failed to apply port configuration" -Color Red
            }
            
            $lastHash = $currentHash
            Write-Host ""
        }
        
        Start-Sleep -Seconds $IntervalSeconds
    }
    catch {
        Write-ColorOutput "Error in watcher: $($_.Exception.Message)" -Color Red
        Start-Sleep -Seconds $IntervalSeconds
    }
}
