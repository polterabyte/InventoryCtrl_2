# Script to setup local DNS resolution for warehouse domains
# This script adds entries to the Windows hosts file

param(
    [string]$ServerIP = "192.168.139.96",
    [switch]$Remove = $false
)

$hostsFile = "$env:SystemRoot\System32\drivers\etc\hosts"
$domains = @(
    "warehouse.cuby",
    "staging.warehouse.cuby", 
    "test.warehouse.cuby"
)

Write-Host "Setting up local DNS resolution..." -ForegroundColor Green

if ($Remove) {
    Write-Host "Removing warehouse domains from hosts file..." -ForegroundColor Yellow
    
    # Read current hosts file
    $hostsContent = Get-Content $hostsFile -ErrorAction SilentlyContinue
    if ($hostsContent) {
        # Filter out warehouse domains
        $newContent = $hostsContent | Where-Object { 
            $line = $_.Trim()
            -not ($line -match "^\d+\.\d+\.\d+\.\d+" -and 
                  ($domains | ForEach-Object { $line -like "*$_*" }) -contains $true)
        }
        
        # Write back to file
        $newContent | Set-Content $hostsFile
        Write-Host "Warehouse domains removed from hosts file" -ForegroundColor Green
    }
} else {
    Write-Host "Adding warehouse domains to hosts file..." -ForegroundColor Yellow
    
    # Check if running as administrator
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    $isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        Write-Host "This script requires administrator privileges to modify hosts file" -ForegroundColor Red
        Write-Host "Please run PowerShell as Administrator and try again" -ForegroundColor Yellow
        exit 1
    }
    
    # Read current hosts file
    $hostsContent = Get-Content $hostsFile -ErrorAction SilentlyContinue
    $newEntries = @()
    
    foreach ($domain in $domains) {
        # Check if domain already exists
        $existingEntry = $hostsContent | Where-Object { $_ -like "*$domain*" }
        
        if ($existingEntry) {
            Write-Host "Domain $domain already exists in hosts file" -ForegroundColor Yellow
        } else {
            $newEntry = "$ServerIP`t$domain"
            $newEntries += $newEntry
            Write-Host "Adding: $newEntry" -ForegroundColor Green
        }
    }
    
    if ($newEntries.Count -gt 0) {
        # Add new entries to hosts file
        $newEntries | Add-Content $hostsFile
        Write-Host "Added $($newEntries.Count) entries to hosts file" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Current warehouse entries in hosts file:" -ForegroundColor Cyan
    $hostsContent = Get-Content $hostsFile
    $hostsContent | Where-Object { 
        $_.Trim() -match "^\d+\.\d+\.\d+\.\d+" -and 
        ($domains | ForEach-Object { $_ -like "*$_*" }) -contains $true 
    } | ForEach-Object { Write-Host "   $_" -ForegroundColor White }
}

Write-Host ""
Write-Host "You can now access your applications using:" -ForegroundColor Cyan
Write-Host "   Production:  https://warehouse.cuby" -ForegroundColor White
Write-Host "   Staging:     https://staging.warehouse.cuby" -ForegroundColor White  
Write-Host "   Test:        https://test.warehouse.cuby" -ForegroundColor White

Write-Host ""
Write-Host "Note: Changes to hosts file take effect immediately" -ForegroundColor Yellow
Write-Host "If you change your server IP, run this script again" -ForegroundColor Yellow