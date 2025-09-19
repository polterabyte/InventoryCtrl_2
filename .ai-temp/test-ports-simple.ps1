# Simple Port Test Script
param(
    [Parameter(Mandatory=$true)]
    [string]$ServerIP
)

Write-Host "Testing ports on server: $ServerIP" -ForegroundColor Cyan
Write-Host ""

$ports = @(80, 5000)

foreach ($port in $ports) {
    Write-Host "Testing port $port..." -ForegroundColor Yellow
    
    # Test with TcpClient
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $connect = $tcpClient.BeginConnect($ServerIP, $port, $null, $null)
        $wait = $connect.AsyncWaitHandle.WaitOne(3000, $false)
        
        if ($wait) {
            $tcpClient.EndConnect($connect)
            $tcpClient.Close()
            Write-Host "  Port ${port}: OPEN" -ForegroundColor Green
        } else {
            $tcpClient.Close()
            Write-Host "  Port ${port}: CLOSED" -ForegroundColor Red
        }
    } catch {
        Write-Host "  Port ${port}: ERROR - $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Test HTTP for web ports
    if ($port -eq 80) {
        try {
            $url = "http://$ServerIP`:$port"
            $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 5 -ErrorAction Stop
            Write-Host "  HTTP Response: $($response.StatusCode)" -ForegroundColor Green
        } catch {
            Write-Host "  HTTP Response: ERROR - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

Write-Host "Test completed!" -ForegroundColor Cyan
Write-Host ""
Write-Host "If ports are CLOSED, you need to:" -ForegroundColor White
Write-Host "1. Add firewall rules (run as Administrator):" -ForegroundColor White
Write-Host "   New-NetFirewallRule -DisplayName 'Inventory_App_Port_80_Inbound' -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow -Profile Any" -ForegroundColor Gray
Write-Host "   New-NetFirewallRule -DisplayName 'Inventory_App_Port_5000_Inbound' -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Any" -ForegroundColor Gray
Write-Host "2. Check router port forwarding" -ForegroundColor White
Write-Host "3. Make sure Docker containers are running" -ForegroundColor White
