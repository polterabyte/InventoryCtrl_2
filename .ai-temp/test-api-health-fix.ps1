# Тестовый скрипт для проверки исправления API URL
Write-Host "Testing API URL fix..." -ForegroundColor Green

# Проверяем, что приложение запущено
$clientUrl = "https://localhost:5001"
$apiUrl = "https://localhost:7000"

Write-Host "Checking if client is running on $clientUrl..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $clientUrl -Method GET -SkipCertificateCheck -TimeoutSec 10
    Write-Host "✓ Client is running (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "✗ Client is not running: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "Checking if API is running on $apiUrl..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$apiUrl/health" -Method GET -SkipCertificateCheck -TimeoutSec 10
    Write-Host "✓ API is running (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "✗ API is not running: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please start the API first using: dotnet run --project src/Inventory.API" -ForegroundColor Yellow
    exit 1
}

Write-Host "Testing login endpoint..." -ForegroundColor Yellow
try {
    $loginData = @{
        Username = "admin"
        Password = "admin123"
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "$apiUrl/api/auth/login" -Method POST -Body $loginData -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 10
    Write-Host "✓ Login endpoint is working (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "✗ Login endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response body: $responseBody" -ForegroundColor Red
    }
}

Write-Host "Test completed!" -ForegroundColor Green