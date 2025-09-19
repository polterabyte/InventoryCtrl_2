# Simple Firewall Check Script
param(
    [switch]$AddRules,
    [switch]$RemoveRules
)

Write-Host "Firewall Check for Inventory App" -ForegroundColor Cyan
Write-Host ""

$ports = @(80, 5000)

# Check firewall status
Write-Host "Checking Windows Firewall status..." -ForegroundColor Yellow
$profiles = @("Domain", "Private", "Public")

foreach ($profile in $profiles) {
    try {
        $status = Get-NetFirewallProfile -Profile $profile | Select-Object Name, Enabled
        $statusText = if ($status.Enabled) { "Enabled" } else { "Disabled" }
        $color = if ($status.Enabled) { "Green" } else { "Red" }
        Write-Host "  $($profile): $statusText" -ForegroundColor $color
    } catch {
        Write-Host "  $($profile): Error checking status" -ForegroundColor Red
    }
}

Write-Host ""

# Check port rules
Write-Host "Checking firewall rules for ports: $($ports -join ', ')" -ForegroundColor Yellow

foreach ($port in $ports) {
    Write-Host "  Port ${port}:" -ForegroundColor White
    
    try {
        $inboundRules = Get-NetFirewallRule | Where-Object { 
            $_.DisplayName -like "*$port*" -and 
            $_.Direction -eq "Inbound" -and 
            $_.Enabled -eq "True" 
        }
        
        $outboundRules = Get-NetFirewallRule | Where-Object { 
            $_.DisplayName -like "*$port*" -and 
            $_.Direction -eq "Outbound" -and 
            $_.Enabled -eq "True" 
        }
        
        Write-Host "    Inbound rules: $($inboundRules.Count)" -ForegroundColor $(if ($inboundRules.Count -gt 0) { "Green" } else { "Red" })
        Write-Host "    Outbound rules: $($outboundRules.Count)" -ForegroundColor $(if ($outboundRules.Count -gt 0) { "Green" } else { "Red" })
        
        if ($inboundRules.Count -gt 0) {
            foreach ($rule in $inboundRules) {
                Write-Host "      - $($rule.DisplayName)" -ForegroundColor Green
            }
        }
        
    } catch {
        Write-Host "    Error checking rules for port $port" -ForegroundColor Red
    }
}

Write-Host ""

# Check if ports are listening
Write-Host "Checking if ports are being used..." -ForegroundColor Yellow

foreach ($port in $ports) {
    try {
        $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
        if ($connections) {
            foreach ($conn in $connections) {
                $processName = (Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue).ProcessName
                Write-Host "  Port ${port}: Used by $processName (PID: $($conn.OwningProcess))" -ForegroundColor Green
            }
        } else {
            Write-Host "  Port ${port}: Not in use" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  Port ${port}: Error checking usage" -ForegroundColor Red
    }
}

Write-Host ""

# Add rules if requested
if ($AddRules) {
    Write-Host "Adding firewall rules..." -ForegroundColor Yellow
    
    foreach ($port in $ports) {
        $ruleName = "Inventory_App_Port_$port"
        
        try {
            # Remove existing rules
            Remove-NetFirewallRule -DisplayName "*$ruleName*" -ErrorAction SilentlyContinue
            
            # Add inbound rule
            New-NetFirewallRule -DisplayName "${ruleName}_Inbound" -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow -Profile Any | Out-Null
            Write-Host "  Added inbound rule for port $port" -ForegroundColor Green
            
            # Add outbound rule
            New-NetFirewallRule -DisplayName "${ruleName}_Outbound" -Direction Outbound -Protocol TCP -LocalPort $port -Action Allow -Profile Any | Out-Null
            Write-Host "  Added outbound rule for port $port" -ForegroundColor Green
            
        } catch {
            Write-Host "  Error adding rules for port ${port}: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Remove rules if requested
if ($RemoveRules) {
    Write-Host "Removing firewall rules..." -ForegroundColor Yellow
    
    foreach ($port in $ports) {
        $ruleName = "Inventory_App_Port_$port"
        
        try {
            Remove-NetFirewallRule -DisplayName "*$ruleName*" -ErrorAction SilentlyContinue
            Write-Host "  Removed rules for port $port" -ForegroundColor Green
        } catch {
            Write-Host "  Error removing rules for port ${port}: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Instructions:" -ForegroundColor White
Write-Host "  1. To add firewall rules: .\firewall-check-simple.ps1 -AddRules" -ForegroundColor White
Write-Host "  2. To remove firewall rules: .\firewall-check-simple.ps1 -RemoveRules" -ForegroundColor White
Write-Host "  3. To test external access: telnet [SERVER_IP] 80" -ForegroundColor White
