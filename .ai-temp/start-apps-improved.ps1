# Enhanced Start Applications Script
# This script provides improved functionality for starting the Inventory Control applications
# with proper dependency management, port configuration, and browser integration

param(
    [switch]$SkipBrowser,
    [switch]$Verbose,
    [switch]$Force,
    [int]$ApiTimeoutSeconds = 60,
    [int]$WebTimeoutSeconds = 60,
    [string]$ConfigFile = "ports.json"
)

# Enhanced logging function
function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO",
        [string]$Color = "White"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    if ($Verbose -or $Level -in @("ERROR", "WARN")) {
        Write-Host $logMessage -ForegroundColor $Color
    } elseif ($Level -eq "INFO") {
        Write-Host $logMessage -ForegroundColor $Color
    }
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

# Function to wait for service to be ready with health check
function Wait-ForService {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 60,
        [int]$IntervalSeconds = 2,
        [string]$ServiceName = "Service"
    )
    
    $elapsed = 0
    Write-Log "Waiting for $ServiceName at $Url..." "INFO" "Yellow"
    
    while ($elapsed -lt $TimeoutSeconds) {
        try {
            # Try to make a request to the service
            $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Log "✅ $ServiceName is ready at $Url" "INFO" "Green"
                return $true
            }
        }
        catch {
            # Service not ready yet, continue waiting
        }
        
        Start-Sleep -Seconds $IntervalSeconds
        $elapsed += $IntervalSeconds
        
        if ($elapsed % 10 -eq 0) {
            Write-Log "Still waiting for $ServiceName... ($elapsed/$TimeoutSeconds seconds)" "INFO" "Yellow"
        }
    }
    
    Write-Log "❌ $ServiceName failed to start within $TimeoutSeconds seconds" "ERROR" "Red"
    return $false
}

# Function to stop existing processes gracefully
function Stop-ExistingProcesses {
    Write-Log "Checking for existing dotnet processes..." "INFO" "Yellow"
    
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if ($dotnetProcesses) {
        Write-Log "Found $($dotnetProcesses.Count) existing dotnet process(es)" "INFO" "Yellow"
        
        if ($Force) {
            $dotnetProcesses | Stop-Process -Force
            Write-Log "Forcefully stopped all dotnet processes" "INFO" "Green"
        } else {
            # Try graceful shutdown first
            foreach ($process in $dotnetProcesses) {
                try {
                    $process.CloseMainWindow()
                    Start-Sleep -Seconds 2
                    if (-not $process.HasExited) {
                        $process.Kill()
                    }
                }
                catch {
                    $process.Kill()
                }
            }
            Write-Log "Gracefully stopped existing dotnet processes" "INFO" "Green"
        }
        
        Start-Sleep -Seconds 3
    } else {
        Write-Log "No existing dotnet processes found" "INFO" "Green"
    }
}

# Function to validate port configuration
function Test-PortConfiguration {
    param([PSCustomObject]$Ports)
    
    Write-Log "Validating port configuration..." "INFO" "Yellow"
    $allPortsFree = $true
    
    # Check API ports
    if ($Ports.api) {
        if ($Ports.api.http) {
            $port = $Ports.api.http
            if (Test-PortAvailable -Port $port) {
                Write-Log "✅ Port $port (API HTTP) is available" "INFO" "Green"
            } else {
                Write-Log "❌ Port $port (API HTTP) is in use" "ERROR" "Red"
                $allPortsFree = $false
            }
        }
        
        if ($Ports.api.https) {
            $port = $Ports.api.https
            if (Test-PortAvailable -Port $port) {
                Write-Log "✅ Port $port (API HTTPS) is available" "INFO" "Green"
            } else {
                Write-Log "❌ Port $port (API HTTPS) is in use" "ERROR" "Red"
                $allPortsFree = $false
            }
        }
    }
    
    # Check Web ports
    if ($Ports.web) {
        if ($Ports.web.http) {
            $port = $Ports.web.http
            if (Test-PortAvailable -Port $port) {
                Write-Log "✅ Port $port (Web HTTP) is available" "INFO" "Green"
            } else {
                Write-Log "❌ Port $port (Web HTTP) is in use" "ERROR" "Red"
                $allPortsFree = $false
            }
        }
        
        if ($Ports.web.https) {
            $port = $Ports.web.https
            if (Test-PortAvailable -Port $port) {
                Write-Log "✅ Port $port (Web HTTPS) is available" "INFO" "Green"
            } else {
                Write-Log "❌ Port $port (Web HTTPS) is in use" "ERROR" "Red"
                $allPortsFree = $false
            }
        }
    }
    
    return $allPortsFree
}

# Function to start a dotnet application
function Start-DotnetApplication {
    param(
        [string]$ProjectPath,
        [string]$ApplicationName,
        [hashtable]$EnvironmentVariables = @{}
    )
    
    if (-not (Test-Path $ProjectPath)) {
        Write-Log "❌ $ApplicationName project not found at $ProjectPath" "ERROR" "Red"
        return $null
    }
    
    Write-Log "Starting $ApplicationName from $ProjectPath..." "INFO" "Yellow"
    
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = "dotnet"
    $startInfo.Arguments = "run --project `"$ProjectPath`""
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Minimized
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $false
    
    # Set environment variables if provided
    foreach ($envVar in $EnvironmentVariables.Keys) {
        $startInfo.EnvironmentVariables[$envVar] = $EnvironmentVariables[$envVar]
    }
    
    try {
        $process = [System.Diagnostics.Process]::Start($startInfo)
        Write-Log "✅ $ApplicationName process started (PID: $($process.Id))" "INFO" "Green"
        return $process
    }
    catch {
        Write-Log "❌ Failed to start $ApplicationName: $($_.Exception.Message)" "ERROR" "Red"
        return $null
    }
}

# Function to monitor application health
function Start-ApplicationMonitoring {
    param(
        [System.Diagnostics.Process]$ApiProcess,
        [System.Diagnostics.Process]$WebProcess,
        [string]$ApiUrl,
        [string]$WebUrl
    )
    
    Write-Log "Starting application monitoring..." "INFO" "Green"
    Write-Log "API Process ID: $($ApiProcess.Id)" "INFO" "White"
    Write-Log "Web Process ID: $($WebProcess.Id)" "INFO" "White"
    Write-Log ""
    Write-Log "Monitoring applications... (Press Ctrl+C to stop)" "INFO" "Green"
    
    $lastHealthCheck = Get-Date
    
    while ($true) {
        try {
            # Check if processes are still running
            $apiRunning = Get-Process -Id $ApiProcess.Id -ErrorAction SilentlyContinue
            $webRunning = Get-Process -Id $WebProcess.Id -ErrorAction SilentlyContinue
            
            if (-not $apiRunning) {
                Write-Log "❌ API server process stopped unexpectedly" "ERROR" "Red"
                if ($webRunning) {
                    Write-Log "Stopping web client..." "INFO" "Yellow"
                    $WebProcess | Stop-Process -Force
                }
                break
            }
            
            if (-not $webRunning) {
                Write-Log "❌ Web client process stopped unexpectedly" "ERROR" "Red"
                Write-Log "API server is still running" "INFO" "Yellow"
            }
            
            # Periodic health check (every 30 seconds)
            if ((Get-Date) - $lastHealthCheck -gt [TimeSpan]::FromSeconds(30)) {
                try {
                    $apiResponse = Invoke-WebRequest -Uri $ApiUrl -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
                    $webResponse = Invoke-WebRequest -Uri $WebUrl -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
                    
                    if ($apiResponse.StatusCode -eq 200 -and $webResponse.StatusCode -eq 200) {
                        Write-Log "✅ Health check passed - both services responding" "INFO" "Green"
                    }
                }
                catch {
                    Write-Log "⚠️ Health check failed - services may be experiencing issues" "WARN" "Yellow"
                }
                
                $lastHealthCheck = Get-Date
            }
            
            Start-Sleep -Seconds 5
        }
        catch {
            Write-Log "Error in monitoring loop: $($_.Exception.Message)" "ERROR" "Red"
            Start-Sleep -Seconds 5
        }
    }
}

# Main execution
try {
    Write-Log "=== Enhanced Inventory Control Application Starter ===" "INFO" "Cyan"
    Write-Log ""
    
    # Load port configuration
    Write-Log "Loading port configuration from $ConfigFile..." "INFO" "Yellow"
    if (-not (Test-Path $ConfigFile)) {
        Write-Log "❌ $ConfigFile not found in current directory" "ERROR" "Red"
        exit 1
    }
    
    $portsConfig = Get-Content $ConfigFile | ConvertFrom-Json
    Write-Log "✅ Port configuration loaded" "INFO" "Green"
    
    # Stop existing processes
    Stop-ExistingProcesses
    
    # Validate port configuration
    $portsAvailable = Test-PortConfiguration -Ports $portsConfig
    if (-not $portsAvailable) {
        Write-Log "❌ Some ports are not available. Use -Force to stop conflicting processes." "ERROR" "Red"
        exit 1
    }
    
    Write-Log ""
    
    # Start API Server
    $apiProject = "src/Inventory.API"
    $apiProcess = Start-DotnetApplication -ProjectPath $apiProject -ApplicationName "API Server"
    
    if (-not $apiProcess) {
        Write-Log "❌ Failed to start API server" "ERROR" "Red"
        exit 1
    }
    
    # Wait for API server to be ready
    $apiHttpsUrl = $portsConfig.launchUrls.api
    $apiReady = Wait-ForService -Url $apiHttpsUrl -TimeoutSeconds $ApiTimeoutSeconds -ServiceName "API Server"
    
    if (-not $apiReady) {
        Write-Log "❌ API server failed to start. Stopping process..." "ERROR" "Red"
        $apiProcess | Stop-Process -Force
        exit 1
    }
    
    Write-Log ""
    
    # Start Web Client
    $webProject = "src/Inventory.Web.Client"
    $webProcess = Start-DotnetApplication -ProjectPath $webProject -ApplicationName "Web Client"
    
    if (-not $webProcess) {
        Write-Log "❌ Failed to start Web client" "ERROR" "Red"
        $apiProcess | Stop-Process -Force
        exit 1
    }
    
    # Wait for Web client to be ready
    $webHttpsUrl = $portsConfig.launchUrls.web
    $webReady = Wait-ForService -Url $webHttpsUrl -TimeoutSeconds $WebTimeoutSeconds -ServiceName "Web Client"
    
    if (-not $webReady) {
        Write-Log "❌ Web client failed to start. Stopping processes..." "ERROR" "Red"
        $webProcess | Stop-Process -Force
        $apiProcess | Stop-Process -Force
        exit 1
    }
    
    Write-Log ""
    Write-Log "=== Applications Started Successfully ===" "INFO" "Green"
    Write-Log "API Server: $($portsConfig.launchUrls.api)" "INFO" "Cyan"
    Write-Log "Web Client: $($portsConfig.launchUrls.web)" "INFO" "Cyan"
    Write-Log ""
    
    # Open browser to web client
    if (-not $SkipBrowser) {
        Write-Log "Opening browser to web client..." "INFO" "Yellow"
        try {
            Start-Process $webHttpsUrl
            Write-Log "✅ Browser opened to $webHttpsUrl" "INFO" "Green"
        }
        catch {
            Write-Log "❌ Failed to open browser: $($_.Exception.Message)" "ERROR" "Red"
            Write-Log "Please manually open: $webHttpsUrl" "INFO" "Yellow"
        }
    }
    
    # Start monitoring
    Start-ApplicationMonitoring -ApiProcess $apiProcess -WebProcess $webProcess -ApiUrl $apiHttpsUrl -WebUrl $webHttpsUrl
}
catch {
    Write-Log "❌ An error occurred: $($_.Exception.Message)" "ERROR" "Red"
    if ($Verbose) {
        Write-Log "Stack trace: $($_.ScriptStackTrace)" "ERROR" "Red"
    }
    exit 1
}
finally {
    Write-Log ""
    Write-Log "Cleaning up..." "INFO" "Yellow"
    
    # Stop any remaining processes
    if ($apiProcess -and -not $apiProcess.HasExited) {
        $apiProcess | Stop-Process -Force
        Write-Log "✅ API process stopped" "INFO" "Green"
    }
    
    if ($webProcess -and -not $webProcess.HasExited) {
        $webProcess | Stop-Process -Force
        Write-Log "✅ Web process stopped" "INFO" "Green"
    }
    
    Write-Log "=== Application Starter Finished ===" "INFO" "Cyan"
}
