# Improve start-apps.ps1 to better handle port conflicts
Write-Host "Improving start-apps.ps1 to handle port conflicts..." -ForegroundColor Green

# Read the current file
$content = Get-Content "start-apps.ps1" -Raw

# Replace the Stop-ExistingProcesses function with an improved version
$newStopFunction = @'
function Stop-ExistingProcesses {
    Write-ColorOutput "Stopping existing processes that might conflict..." "Yellow"
    
    # Stop all dotnet processes
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if ($dotnetProcesses) {
        Write-ColorOutput "Found $($dotnetProcesses.Count) dotnet processes, stopping..." "Yellow"
        $dotnetProcesses | Stop-Process -Force
        Start-Sleep -Seconds 2
        Write-ColorOutput "✅ Dotnet processes stopped" "Green"
    } else {
        Write-ColorOutput "No existing dotnet processes found" "Green"
    }
    
    # Stop processes on specific ports
    $portsToCheck = @(5000, 7000, 5001, 7001)
    foreach ($port in $portsToCheck) {
        try {
            $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
            if ($connections) {
                $processes = $connections | ForEach-Object { Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue } | Where-Object { $_.ProcessName -ne "System" }
                if ($processes) {
                    Write-ColorOutput "Found processes on port $port, stopping..." "Yellow"
                    $processes | Stop-Process -Force
                    Start-Sleep -Seconds 1
                    Write-ColorOutput "✅ Processes on port $port stopped" "Green"
                }
            }
        } catch {
            # Port might not be in use, continue
        }
    }
    
    # Wait a bit for ports to be released
    Start-Sleep -Seconds 3
    Write-ColorOutput "✅ Port cleanup completed" "Green"
}
'@

# Replace the function in the content
$content = $content -replace '(?s)function Stop-ExistingProcesses \{.*?\}', $newStopFunction

# Also improve the port availability check to be more informative
$newPortCheck = @'
function Check-PortAvailability {
    param([PSCustomObject]$Ports)
    
    Write-ColorOutput "Checking port availability..." "Yellow"
    $allPortsFree = $true
    
    # Check API ports
    if ($Ports.api) {
        if ($Ports.api.http) {
            $port = $Ports.api.http
            if (Test-PortAvailable -Port $port) {
                Write-ColorOutput "✅ Port $port (API HTTP) is available" "Green"
            } else {
                Write-ColorOutput "❌ Port $port (API HTTP) is in use" "Red"
                $allPortsFree = $false
                
                # Show what's using the port
                try {
                    $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
                    if ($connection) {
                        $process = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
                        if ($process) {
                            Write-ColorOutput "   Process using port: $($process.ProcessName) (PID: $($process.Id))" "Red"
                        }
                    }
                } catch {
                    Write-ColorOutput "   Could not identify process using port $port" "Red"
                }
            }
        }
        
        if ($Ports.api.https) {
            $port = $Ports.api.https
            if (Test-PortAvailable -Port $port) {
                Write-ColorOutput "✅ Port $port (API HTTPS) is available" "Green"
            } else {
                Write-ColorOutput "❌ Port $port (API HTTPS) is in use" "Red"
                $allPortsFree = $false
                
                # Show what's using the port
                try {
                    $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
                    if ($connection) {
                        $process = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
                        if ($process) {
                            Write-ColorOutput "   Process using port: $($process.ProcessName) (PID: $($process.Id))" "Red"
                        }
                    }
                } catch {
                    Write-ColorOutput "   Could not identify process using port $port" "Red"
                }
            }
        }
    }
    
    # Check Web ports
    if ($Ports.web) {
        if ($Ports.web.http) {
            $port = $Ports.web.http
            if (Test-PortAvailable -Port $port) {
                Write-ColorOutput "✅ Port $port (Web HTTP) is available" "Green"
            } else {
                Write-ColorOutput "❌ Port $port (Web HTTP) is in use" "Red"
                $allPortsFree = $false
                
                # Show what's using the port
                try {
                    $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
                    if ($connection) {
                        $process = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
                        if ($process) {
                            Write-ColorOutput "   Process using port: $($process.ProcessName) (PID: $($process.Id))" "Red"
                        }
                    }
                } catch {
                    Write-ColorOutput "   Could not identify process using port $port" "Red"
                }
            }
        }
        
        if ($Ports.web.https) {
            $port = $Ports.web.https
            if (Test-PortAvailable -Port $port) {
                Write-ColorOutput "✅ Port $port (Web HTTPS) is available" "Green"
            } else {
                Write-ColorOutput "❌ Port $port (Web HTTPS) is in use" "Red"
                $allPortsFree = $false
                
                # Show what's using the port
                try {
                    $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
                    if ($connection) {
                        $process = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
                        if ($process) {
                            Write-ColorOutput "   Process using port: $($process.ProcessName) (PID: $($process.Id))" "Red"
                        }
                    }
                } catch {
                    Write-ColorOutput "   Could not identify process using port $port" "Red"
                }
            }
        }
    }
    
    return $allPortsFree
}
'@

# Replace the port check function
$content = $content -replace '(?s)function Check-PortAvailability \{.*?\}', $newPortCheck

# Write the improved content back
Set-Content "start-apps.ps1" $content

Write-Host "Improved start-apps.ps1!" -ForegroundColor Green
Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Enhanced Stop-ExistingProcesses to stop processes on specific ports" -ForegroundColor White
Write-Host "2. Added process identification for port conflicts" -ForegroundColor White
Write-Host "3. Better error messages showing which process is using a port" -ForegroundColor White
Write-Host "4. More thorough cleanup before starting new processes" -ForegroundColor White
Write-Host ""
Write-Host "Now start-apps.ps1 will:" -ForegroundColor Yellow
Write-Host "- Stop all dotnet processes" -ForegroundColor White
Write-Host "- Stop processes on ports 5000, 7000, 5001, 7001" -ForegroundColor White
Write-Host "- Show which process is using a port if there's a conflict" -ForegroundColor White
Write-Host "- Wait for ports to be released before starting new processes" -ForegroundColor White
