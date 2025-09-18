# Diagnostic script to check what's happening with the applications

param(
    [switch]$Verbose
)

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Test-Url {
    param(
        [string]$Url,
        [string]$Description
    )
    
    try {
        Write-ColorOutput "Testing ${Description}: $Url" "Yellow"
        $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
        Write-ColorOutput "✅ $Description responded with status $($response.StatusCode)" "Green"
        return $true
    }
    catch {
        Write-ColorOutput "❌ $Description failed: $($_.Exception.Message)" "Red"
        return $false
    }
}

function Get-ProcessInfo {
    param([string]$ProcessName)
    
    $processes = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    if ($processes) {
        Write-ColorOutput "Found $($processes.Count) $ProcessName process(es):" "Green"
        foreach ($proc in $processes) {
            Write-ColorOutput "  PID: $($proc.Id), CPU: $($proc.CPU), Memory: $([math]::Round($proc.WorkingSet64/1MB, 2)) MB" "White"
        }
    } else {
        Write-ColorOutput "No $ProcessName processes found" "Red"
    }
}

function Get-PortInfo {
    param([int]$Port)
    
    $connections = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if ($connections) {
        Write-ColorOutput "Port $Port is in use:" "Yellow"
        foreach ($conn in $connections) {
            Write-ColorOutput "  State: $($conn.State), Process: $($conn.OwningProcess)" "White"
        }
    } else {
        Write-ColorOutput "Port $Port is free" "Green"
    }
}

# Main diagnostic
Write-ColorOutput "=== Startup Diagnostic ===" "Cyan"
Write-ColorOutput ""

# Load configuration
$portsConfig = Get-Content "ports.json" | ConvertFrom-Json

# Check processes
Write-ColorOutput "=== Process Information ===" "Cyan"
Get-ProcessInfo "dotnet"

Write-ColorOutput ""
Write-ColorOutput "=== Port Information ===" "Cyan"
Get-PortInfo $portsConfig.api.http
Get-PortInfo $portsConfig.api.https
Get-PortInfo $portsConfig.web.http
Get-PortInfo $portsConfig.web.https

Write-ColorOutput ""
Write-ColorOutput "=== URL Testing ===" "Cyan"

# Test API URLs
$apiHttpUrl = "http://localhost:$($portsConfig.api.http)"
$apiHttpsUrl = "https://localhost:$($portsConfig.api.https)"

Test-Url $apiHttpUrl "API HTTP"
Test-Url $apiHttpsUrl "API HTTPS"

# Test API endpoints
$apiEndpoints = @(
    "$apiHttpUrl/swagger",
    "$apiHttpUrl/health",
    "$apiHttpUrl/api",
    "$apiHttpsUrl/swagger",
    "$apiHttpsUrl/health",
    "$apiHttpsUrl/api"
)

foreach ($endpoint in $apiEndpoints) {
    Test-Url $endpoint "API Endpoint"
}

Write-ColorOutput ""
Write-ColorOutput "=== Web Client Testing ===" "Cyan"

# Test Web URLs
$webHttpUrl = "http://localhost:$($portsConfig.web.http)"
$webHttpsUrl = "https://localhost:$($portsConfig.web.https)"

Test-Url $webHttpUrl "Web HTTP"
Test-Url $webHttpsUrl "Web HTTPS"

Write-ColorOutput ""
Write-ColorOutput "=== Project Structure Check ===" "Cyan"

# Check project files
$projects = @(
    "src/Inventory.API",
    "src/Inventory.Web.Client"
)

foreach ($project in $projects) {
    if (Test-Path $project) {
        Write-ColorOutput "✅ $project exists" "Green"
        
        # Check for .csproj file
        $csprojFile = Get-ChildItem -Path $project -Filter "*.csproj" -ErrorAction SilentlyContinue
        if ($csprojFile) {
            Write-ColorOutput "  ✅ Project file: $($csprojFile.Name)" "Green"
        } else {
            Write-ColorOutput "  ❌ No .csproj file found" "Red"
        }
    } else {
        Write-ColorOutput "❌ $project not found" "Red"
    }
}

Write-ColorOutput ""
Write-ColorOutput "=== Recent Logs ===" "Cyan"

# Check for recent log files
$logPath = "src/Inventory.API/logs"
if (Test-Path $logPath) {
    $latestLog = Get-ChildItem -Path $logPath -Filter "*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Write-ColorOutput "Latest log file: $($latestLog.Name)" "Yellow"
        Write-ColorOutput "Last modified: $($latestLog.LastWriteTime)" "White"
        
        if ($Verbose) {
            Write-ColorOutput "Last 10 lines of log:" "Yellow"
            $logContent = Get-Content $latestLog.FullName -Tail 10
            foreach ($line in $logContent) {
                Write-ColorOutput "  $line" "Gray"
            }
        }
    }
} else {
    Write-ColorOutput "No log directory found" "Yellow"
}

Write-ColorOutput ""
Write-ColorOutput "=== Diagnostic Complete ===" "Cyan"
