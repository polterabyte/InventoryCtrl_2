$json = Get-Content .ai-temp/login.json -Raw
$bytes = [System.Text.Encoding]::UTF8.GetBytes($json)

try {
    $response = Invoke-WebRequest -Uri "http://192.168.139.96/api/auth/login" -Method POST -Body $bytes -ContentType "application/json" -UseBasicParsing
    Write-Host "Status: $($response.StatusCode)"
    Write-Host "Content: $($response.Content)"
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody"
    }
}
