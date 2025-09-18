# Fast Start Applications Script
# Optimized version with shorter timeouts and better fallback logic

param(
    [switch]$SkipBrowser,
    [switch]$Verbose
)

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Function to check if port is available
function Test-PortAvailable {
    param([int]$Port)
    try {
        $connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
        return $null -eq $connection
    }
    catch {
        return $true
    }
}

# Function to wait for service with multiple URL attempts
function Wait-ForServiceFast {
    param(
        [string[]]$Urls,
        [int]$TimeoutSeconds = 30,
        [int]$IntervalSeconds = 1
    )
    
    $elapsed = 0
    Write-ColorOutput "Waiting for service..." "Yellow"
    
    while ($elapsed -lt $TimeoutSeconds) {
        foreach ($url in $Urls) {
            try {
                $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 3 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-ColorOutput "✅ Service is ready at $url" "Green"
                    return $true
                }
            }
            catch {
                # Continue to next URL
            }
        }
        
        Start-Sleep -Seconds $IntervalSeconds
        $elapsed += $IntervalSeconds
        
        if ($elapsed % 5 -eq 0) {
            Write-ColorOutput "Still waiting... ($elapsed/$TimeoutSeconds seconds)" "Yellow"
        }
    }
    
    Write-ColorOutput "❌ Service failed to start within $TimeoutSeconds seconds" "Red"
    return $false
}

# Main execution
try {
    Write-ColorOutput "=== Fast Inventory Control Application Starter ===" "Cyan"
    Write-ColorOutput ""
    
    # Load port configuration
    Write-ColorOutput "Loading port configuration..." "Yellow"
    $portsConfig = Get-Content "ports.json" | ConvertFrom-Json
    Write-ColorOutput "✅ Port configuration loaded" "Green"
    
    # Stop existing processes
    Write-ColorOutput "Stopping existing dotnet processes..." "Yellow"
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
    Write-ColorOutput "✅ Existing processes stopped" "Green"
    
    # Check ports quickly
    Write-ColorOutput "Checking ports..." "Yellow"
    $apiHttpPort = $portsConfig.api.http
    $apiHttpsPort = $portsConfig.api.https
    $webHttpPort = $portsConfig.web.http
    $webHttpsPort = $portsConfig.web.https
    
    $portsFree = $true
    if (-not (Test-PortAvailable -Port $apiHttpPort)) { Write-ColorOutput "❌ Port $apiHttpPort is in use" "Red"; $portsFree = $false }
    if (-not (Test-PortAvailable -Port $apiHttpsPort)) { Write-ColorOutput "❌ Port $apiHttpsPort is in use" "Red"; $portsFree = $false }
    if (-not (Test-PortAvailable -Port $webHttpPort)) { Write-ColorOutput "❌ Port $webHttpPort is in use" "Red"; $portsFree = $false }
    if (-not (Test-PortAvailable -Port $webHttpsPort)) { Write-ColorOutput "❌ Port $webHttpsPort is in use" "Red"; $portsFree = $false }
    
    if (-not $portsFree) {
        Write-ColorOutput "❌ Some ports are not available" "Red"
        exit 1
    }
    
    Write-ColorOutput "✅ All ports are available" "Green"
    Write-ColorOutput ""
    
    # Start API Server
    Write-ColorOutput "Starting API server..." "Yellow"
    $apiProject = "src/Inventory.API"
    $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", $apiProject -WindowStyle Minimized -PassThru
    Write-ColorOutput "✅ API server process started (PID: $($apiProcess.Id))" "Green"
    
    # Wait for API with multiple URLs
    $apiUrls = @(
        "http://localhost:$apiHttpPort",
        "https://localhost:$apiHttpsPort"
    )
    $apiReady = Wait-ForServiceFast -Urls $apiUrls -TimeoutSeconds 30
    
    if (-not $apiReady) {
        Write-ColorOutput "❌ API server failed to start" "Red"
        $apiProcess | Stop-Process -Force
        exit 1
    }
    
    Write-ColorOutput ""
    
    # Start Web Client
    Write-ColorOutput "Starting Web client..." "Yellow"
    $webProject = "src/Inventory.Web.Client"
    $webProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", $webProject -WindowStyle Minimized -PassThru
    Write-ColorOutput "✅ Web client process started (PID: $($webProcess.Id))" "Green"
    
    # Wait for Web with multiple URLs
    $webUrls = @(
        "http://localhost:$webHttpPort",
        "https://localhost:$webHttpsPort"
    )
    $webReady = Wait-ForServiceFast -Urls $webUrls -TimeoutSeconds 30
    
    if (-not $webReady) {
        Write-ColorOutput "❌ Web client failed to start" "Red"
        $webProcess | Stop-Process -Force
        $apiProcess | Stop-Process -Force
        exit 1
    }
    
    Write-ColorOutput ""
    Write-ColorOutput "=== Applications Started Successfully ===" "Green"
    Write-ColorOutput "API: http://localhost:$apiHttpPort | https://localhost:$apiHttpsPort" "Cyan"
    Write-ColorOutput "Web: http://localhost:$webHttpPort | https://localhost:$webHttpsPort" "Cyan"
    Write-ColorOutput ""
    
    # Open browser
    if (-not $SkipBrowser) {
        $webUrl = "https://localhost:$webHttpsPort"
        Write-ColorOutput "Opening browser to $webUrl..." "Yellow"
        try {
            Start-Process $webUrl
            Write-ColorOutput "✅ Browser opened" "Green"
        }
        catch {
            Write-ColorOutput "❌ Failed to open browser: $($_.Exception.Message)" "Red"
            Write-ColorOutput "Please manually open: $webUrl" "Yellow"
        }
    }
    
    Write-ColorOutput ""
    Write-ColorOutput "=== Process Information ===" "Cyan"
    Write-ColorOutput "API Process ID: $($apiProcess.Id)" "White"
    Write-ColorOutput "Web Process ID: $($webProcess.Id)" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Applications are running! Press Ctrl+C to stop." "Green"
    
    # Simple monitoring loop
    try {
        while ($true) {
            $apiRunning = Get-Process -Id $apiProcess.Id -ErrorAction SilentlyContinue
            $webRunning = Get-Process -Id $webProcess.Id -ErrorAction SilentlyContinue
            
            if (-not $apiRunning) {
                Write-ColorOutput "❌ API server stopped" "Red"
                break
            }
            
            if (-not $webRunning) {
                Write-ColorOutput "❌ Web client stopped" "Red"
                break
            }
            
            Start-Sleep -Seconds 5
        }
    }
    catch {
        # Ctrl+C or other interruption
    }
}
catch {
    Write-ColorOutput "❌ Error: $($_.Exception.Message)" "Red"
    exit 1
}
finally {
    Write-ColorOutput ""
    Write-ColorOutput "Stopping applications..." "Yellow"
    
    if ($apiProcess -and -not $apiProcess.HasExited) {
        $apiProcess | Stop-Process -Force
        Write-ColorOutput "✅ API stopped" "Green"
    }
    
    if ($webProcess -and -not $webProcess.HasExited) {
        $webProcess | Stop-Process -Force
        Write-ColorOutput "✅ Web stopped" "Green"
    }
    
    Write-ColorOutput "=== Finished ===" "Cyan"
}
