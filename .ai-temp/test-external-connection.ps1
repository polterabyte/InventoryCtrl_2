# Скрипт для тестирования подключения с внешнего IP
param(
    [Parameter(Mandatory=$true)]
    [string]$ServerIP,
    
    [Parameter(Mandatory=$false)]
    [int]$ApiPort = 5000,
    
    [Parameter(Mandatory=$false)]
    [int]$WebPort = 80
)

Write-Host "🔍 Тестирование подключения к серверу $ServerIP" -ForegroundColor Cyan
Write-Host ""

# Тест 1: Проверка доступности API
Write-Host "📡 Тест 1: Проверка доступности API на порту $ApiPort" -ForegroundColor Yellow
try {
    $apiUrl = "http://$ServerIP`:$ApiPort/health"
    $response = Invoke-WebRequest -Uri $apiUrl -Method GET -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API доступен: $apiUrl" -ForegroundColor Green
        Write-Host "   Ответ: $($response.Content)" -ForegroundColor Gray
    } else {
        Write-Host "❌ API недоступен: HTTP $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Ошибка подключения к API: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Тест 2: Проверка доступности веб-клиента
Write-Host "🌐 Тест 2: Проверка доступности веб-клиента на порту $WebPort" -ForegroundColor Yellow
try {
    $webUrl = "http://$ServerIP`:$WebPort"
    $response = Invoke-WebRequest -Uri $webUrl -Method GET -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Веб-клиент доступен: $webUrl" -ForegroundColor Green
    } else {
        Write-Host "❌ Веб-клиент недоступен: HTTP $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Ошибка подключения к веб-клиенту: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Тест 3: Проверка CORS
Write-Host "🔒 Тест 3: Проверка CORS заголовков" -ForegroundColor Yellow
try {
    $corsUrl = "http://$ServerIP`:$ApiPort/health"
    $response = Invoke-WebRequest -Uri $corsUrl -Method OPTIONS -Headers @{
        "Origin" = "http://$ServerIP`:$WebPort"
        "Access-Control-Request-Method" = "GET"
        "Access-Control-Request-Headers" = "Content-Type"
    } -TimeoutSec 10
    
    $accessControlAllowOrigin = $response.Headers["Access-Control-Allow-Origin"]
    if ($accessControlAllowOrigin) {
        Write-Host "✅ CORS настроен: Access-Control-Allow-Origin = $accessControlAllowOrigin" -ForegroundColor Green
    } else {
        Write-Host "⚠️  CORS заголовки не найдены" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Ошибка проверки CORS: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Тест 4: Проверка авторизации
Write-Host "🔐 Тест 4: Проверка эндпоинта авторизации" -ForegroundColor Yellow
try {
    $authUrl = "http://$ServerIP`:$ApiPort/api/auth/login"
    $loginData = @{
        username = "admin"
        password = "Admin123!"
    } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri $authUrl -Method POST -Body $loginData -ContentType "application/json" -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Эндпоинт авторизации работает" -ForegroundColor Green
    } else {
        Write-Host "❌ Проблема с авторизацией: HTTP $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Ошибка авторизации: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🏁 Тестирование завершено" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 Инструкции для подключения:" -ForegroundColor White
Write-Host "   1. Откройте браузер и перейдите по адресу: http://$ServerIP`:$WebPort" -ForegroundColor White
Write-Host "   2. Попробуйте войти с учетными данными: admin / Admin123!" -ForegroundColor White
Write-Host "   3. Если возникают ошибки, проверьте консоль браузера (F12)" -ForegroundColor White
