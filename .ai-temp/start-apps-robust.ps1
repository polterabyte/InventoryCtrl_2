# Robust Start Applications Script
# This version waits longer and checks multiple endpoints

param(
    [switch]$SkipBrowser,
    [switch]$Verbose
)

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

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

function Wait-ForServiceRobust {
    param(
        [string[]]$Urls,
        [int]$TimeoutSeconds = 90,
        [int]$IntervalSeconds = 3
    )
    
    $elapsed = 0
    Write-ColorOutput "Waiting for service to be ready..." "Yellow"
    
    while ($elapsed -lt $TimeoutSeconds) {
        foreach ($url in $Urls) {
            try {
                $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
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
        
        if ($elapsed % 15 -eq 0) {
            Write-ColorOutput "Still waiting... ($elapsed/$TimeoutSeconds seconds)" "Yellow"
            
            # Show process info
            $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
            if ($dotnetProcesses) {
                Write-ColorOutput "  Dotnet processes running: $($dotnetProcesses.Count)" "Gray"
            }
        }
    }
    
    Write-ColorOutput "❌ Service failed to start within $TimeoutSeconds seconds" "Red"
    return $false
}

function Start-ApplicationWithRetry {
    param(
        [string]$ProjectPath,
        [string]$ApplicationName,
        [int]$MaxRetries = 2
    )
    
    for ($i = 1; $i -le $MaxRetries; $i++) {
        Write-ColorOutput "Starting $ApplicationName (attempt $i/$MaxRetries)..." "Yellow"
        
        $process = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", $ProjectPath -WindowStyle Minimized -PassThru
        
        if ($process) {
            Write-ColorOutput "✅ $ApplicationName process started (PID: $($process.Id))" "Green"
            return $process
        } else {
            Write-ColorOutput "❌ Failed to start $ApplicationName (attempt $i)" "Red"
            if ($i -lt $MaxRetries) {
                Start-Sleep -Seconds 5
            }
        }
    }
    
    return $null
}

# Main execution
try {
    Write-ColorOutput "=== Robust Inventory Control Application Starter ===" "Cyan"
    Write-ColorOutput ""
    
    # Load port configuration
    Write-ColorOutput "Loading port configuration..." "Yellow"
    $portsConfig = Get-Content "ports.json" | ConvertFrom-Json
    Write-ColorOutput "✅ Port configuration loaded" "Green"
    
    # Stop existing processes
    Write-ColorOutput "Stopping existing dotnet processes..." "Yellow"
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 3
    Write-ColorOutput "✅ Existing processes stopped" "Green"
    
    # Check ports
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
    $apiProject = "src/Inventory.API"
    $apiProcess = Start-ApplicationWithRetry -ProjectPath $apiProject -ApplicationName "API Server"
    
    if (-not $apiProcess) {
        Write-ColorOutput "❌ Failed to start API server after retries" "Red"
        exit 1
    }
    
    # Wait for API with multiple URLs and longer timeout
    $apiUrls = @(
        "http://localhost:$apiHttpPort",
        "https://localhost:$apiHttpsPort",
        "http://localhost:$apiHttpPort/swagger",
        "http://localhost:$apiHttpPort/health",
        "http://localhost:$apiHttpPort/api"
    )
    
    Write-ColorOutput "Waiting for API server to be ready (this may take up to 90 seconds)..." "Yellow"
    $apiReady = Wait-ForServiceRobust -Urls $apiUrls -TimeoutSeconds 90
    
    if (-not $apiReady) {
        Write-ColorOutput "❌ API server failed to start" "Red"
        $apiProcess | Stop-Process -Force
        exit 1
    }
    
    Write-ColorOutput ""
    
    # Start Web Client
    $webProject = "src/Inventory.Web.Client"
    $webProcess = Start-ApplicationWithRetry -ProjectPath $webProject -ApplicationName "Web Client"
    
    if (-not $webProcess) {
        Write-ColorOutput "❌ Failed to start Web client" "Red"
        $apiProcess | Stop-Process -Force
        exit 1
    }
    
    # Wait for Web with multiple URLs
    $webUrls = @(
        "http://localhost:$webHttpPort",
        "https://localhost:$webHttpsPort"
    )
    
    Write-ColorOutput "Waiting for Web client to be ready..." "Yellow"
    $webReady = Wait-ForServiceRobust -Urls $webUrls -TimeoutSeconds 60
    
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
            
            Start-Sleep -Seconds 10
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
