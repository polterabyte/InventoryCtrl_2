# Тест регистрации пользователя
param(
    [string]$ApiUrl = "https://staging.warehouse.cuby/api",
    [string]$TestUsername = "testuser$(Get-Random)",
    [string]$TestEmail = "test$(Get-Random)@example.com",
    [string]$TestPassword = "TestPassword123!"
)

Write-Host "Тестирование регистрации пользователя..." -ForegroundColor Green
Write-Host "API URL: $ApiUrl" -ForegroundColor Yellow
Write-Host "Тестовые данные:" -ForegroundColor Yellow
Write-Host "  Username: $TestUsername" -ForegroundColor Cyan
Write-Host "  Email: $TestEmail" -ForegroundColor Cyan
Write-Host "  Password: $TestPassword" -ForegroundColor Cyan

# Подготовка данных для регистрации
$registerData = @{
    Username = $TestUsername
    Email = $TestEmail
    Password = $TestPassword
    ConfirmPassword = $TestPassword
} | ConvertTo-Json

Write-Host "`nОтправка запроса на регистрацию..." -ForegroundColor Green

try {
    $response = Invoke-RestMethod -Uri "$ApiUrl/auth/register" -Method POST -Body $registerData -ContentType "application/json" -ErrorAction Stop
    
    Write-Host "Успешно!" -ForegroundColor Green
    Write-Host "Ответ сервера:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 3
    
    if ($response.Success) {
        Write-Host "`n✅ Регистрация прошла успешно!" -ForegroundColor Green
    } else {
        Write-Host "`n❌ Регистрация не удалась: $($response.ErrorMessage)" -ForegroundColor Red
    }
} catch {
    Write-Host "`n❌ Ошибка при регистрации:" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Попробуем получить детали ошибки
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        Write-Host "Response Body: $errorBody" -ForegroundColor Red
    }
}

Write-Host "`nТест завершен." -ForegroundColor Green
