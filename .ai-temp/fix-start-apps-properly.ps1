# Fix start-apps.ps1 properly
Write-Host "Fixing start-apps.ps1 properly..." -ForegroundColor Green

# Read the current file
$content = Get-Content "start-apps.ps1" -Raw

# Add the Stop-ExistingProcesses function after the existing functions
$stopFunction = @'

# Function to stop existing processes that might conflict
function Stop-ExistingProcesses {
    Write-Host "Stopping existing processes that might conflict..." -ForegroundColor Yellow
    
    # Stop all dotnet processes
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if ($dotnetProcesses) {
        Write-Host "Found $($dotnetProcesses.Count) dotnet processes, stopping..." -ForegroundColor Yellow
        $dotnetProcesses | Stop-Process -Force
        Start-Sleep -Seconds 2
        Write-Host "✅ Dotnet processes stopped" -ForegroundColor Green
    } else {
        Write-Host "No existing dotnet processes found" -ForegroundColor Green
    }
    
    # Stop processes on specific ports
    $portsToCheck = @(5000, 7000, 5001, 7001)
    foreach ($port in $portsToCheck) {
        try {
            $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
            if ($connections) {
                $processes = $connections | ForEach-Object { 
                    try { Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue } 
                    catch { $null }
                } | Where-Object { $_.ProcessName -ne "System" -and $_ -ne $null }
                
                if ($processes) {
                    Write-Host "Found processes on port $port, stopping..." -ForegroundColor Yellow
                    $processes | Stop-Process -Force
                    Start-Sleep -Seconds 1
                    Write-Host "✅ Processes on port $port stopped" -ForegroundColor Green
                }
            }
        } catch {
            # Port might not be in use, continue
        }
    }
    
    # Wait a bit for ports to be released
    Start-Sleep -Seconds 3
    Write-Host "✅ Port cleanup completed" -ForegroundColor Green
}

'@

# Insert the function after the Wait-ForServer function
$content = $content -replace '(function Wait-ForServer \{.*?\})', "`$1$stopFunction"

# Add call to Stop-ExistingProcesses at the beginning of the main execution
$content = $content -replace '(Write-Host "=== Inventory Control Application Starter ===" "Cyan")', "`$1`n`n    # Stop existing processes first`n    Stop-ExistingProcesses"

# Write the improved content back
Set-Content "start-apps.ps1" $content

Write-Host "Fixed start-apps.ps1 properly!" -ForegroundColor Green
Write-Host ""
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "1. Added Stop-ExistingProcesses function" -ForegroundColor White
Write-Host "2. Added call to stop processes at the beginning" -ForegroundColor White
Write-Host "3. Proper error handling for process operations" -ForegroundColor White
Write-Host ""
Write-Host "The script will now stop conflicting processes before starting new ones" -ForegroundColor Green
