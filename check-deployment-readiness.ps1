# Скрипт проверки готовности к развертыванию warehouse.cuby
param(
    [string]$Environment = "production"
)

Write-Host "🔍 Проверка готовности к развертыванию warehouse.cuby..." -ForegroundColor Green
Write-Host "Окружение: $Environment" -ForegroundColor Yellow
Write-Host ""

$allChecksPassed = $true

# 1. Проверка Docker
Write-Host "1. Проверка Docker..." -ForegroundColor Cyan
try {
    $dockerVersion = docker --version
    if ($dockerVersion) {
        Write-Host "   ✅ Docker установлен: $dockerVersion" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Docker не найден" -ForegroundColor Red
        $allChecksPassed = $false
    }
} catch {
    Write-Host "   ❌ Docker не установлен" -ForegroundColor Red
    $allChecksPassed = $false
}

try {
    $composeVersion = docker-compose --version
    if ($composeVersion) {
        Write-Host "   ✅ Docker Compose установлен: $composeVersion" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Docker Compose не найден" -ForegroundColor Red
        $allChecksPassed = $false
    }
} catch {
    Write-Host "   ❌ Docker Compose не установлен" -ForegroundColor Red
    $allChecksPassed = $false
}

# 2. Проверка файлов конфигурации
Write-Host "`n2. Проверка файлов конфигурации..." -ForegroundColor Cyan

$requiredFiles = @(
    "docker-compose.yml",
    "docker-compose.prod.yml",
    "nginx/nginx.conf",
    "nginx/conf.d/locations.conf"
)

foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "   ✅ $file найден" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $file не найден" -ForegroundColor Red
        $allChecksPassed = $false
    }
}

# 3. Проверка переменных окружения
Write-Host "`n3. Проверка переменных окружения..." -ForegroundColor Cyan

$envFile = "env.$Environment"
if (Test-Path $envFile) {
    Write-Host "   ✅ $envFile найден" -ForegroundColor Green
    
    # Проверка обязательных переменных
    $envContent = Get-Content $envFile
    $requiredVars = @("POSTGRES_PASSWORD", "JWT_KEY", "DOMAIN")
    
    foreach ($var in $requiredVars) {
        if ($envContent -match "^$var=") {
            Write-Host "   ✅ $var настроен" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $var не настроен" -ForegroundColor Red
            $allChecksPassed = $false
        }
    }
} else {
    Write-Host "   ❌ $envFile не найден" -ForegroundColor Red
    $allChecksPassed = $false
}

# 4. Проверка SSL сертификатов
Write-Host "`n4. Проверка SSL сертификатов..." -ForegroundColor Cyan

$sslDir = "nginx/ssl"
if (Test-Path $sslDir) {
    Write-Host "   ✅ Директория SSL найдена" -ForegroundColor Green
    
    $domains = @("warehouse.cuby", "staging.warehouse.cuby", "test.warehouse.cuby")
    foreach ($domain in $domains) {
        $certFile = "$sslDir/$domain.crt"
        $keyFile = "$sslDir/$domain.key"
        
        if ((Test-Path $certFile) -and (Test-Path $keyFile)) {
            Write-Host "   ✅ Сертификат для $domain найден" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Сертификат для $domain не найден" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "   ⚠️  Директория SSL не найдена" -ForegroundColor Yellow
}

# 5. Проверка портов
Write-Host "`n5. Проверка портов..." -ForegroundColor Cyan

$ports = @(80, 443)
foreach ($port in $ports) {
    try {
        $connection = Test-NetConnection -ComputerName "localhost" -Port $port -WarningAction SilentlyContinue
        if ($connection.TcpTestSucceeded) {
            Write-Host "   ✅ Порт $port открыт" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Порт $port закрыт" -ForegroundColor Red
            $allChecksPassed = $false
        }
    } catch {
        Write-Host "   ⚠️  Не удалось проверить порт $port" -ForegroundColor Yellow
    }
}

# 6. Проверка DNS (если доступно)
Write-Host "`n6. Проверка DNS..." -ForegroundColor Cyan

$domains = @("warehouse.cuby", "staging.warehouse.cuby", "test.warehouse.cuby")
foreach ($domain in $domains) {
    try {
        $dnsResult = Resolve-DnsName -Name $domain -ErrorAction SilentlyContinue
        if ($dnsResult) {
            Write-Host "   ✅ $domain резолвится" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  $domain не резолвится" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   ⚠️  Не удалось проверить DNS для $domain" -ForegroundColor Yellow
    }
}

# 7. Проверка Docker образов
Write-Host "`n7. Проверка Docker образов..." -ForegroundColor Cyan

try {
    $images = docker images --format "table {{.Repository}}:{{.Tag}}"
    if ($images -match "inventory") {
        Write-Host "   ✅ Docker образы найдены" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Docker образы не найдены (будут собраны при развертывании)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Не удалось проверить Docker образы" -ForegroundColor Yellow
}

# Итоговый результат
Write-Host "`n" + "="*50 -ForegroundColor Gray
if ($allChecksPassed) {
    Write-Host "🎉 Система готова к развертыванию!" -ForegroundColor Green
    Write-Host "Можно запускать: .\deploy-$Environment.ps1" -ForegroundColor Cyan
} else {
    Write-Host "❌ Система НЕ готова к развертыванию" -ForegroundColor Red
    Write-Host "Исправьте ошибки выше и запустите проверку снова" -ForegroundColor Yellow
}
Write-Host "="*50 -ForegroundColor Gray

# Дополнительные рекомендации
Write-Host "`n📋 Дополнительные рекомендации:" -ForegroundColor Yellow
Write-Host "1. Убедитесь, что сервер имеет достаточно ресурсов (4GB+ RAM)" -ForegroundColor White
Write-Host "2. Настройте firewall для портов 80 и 443" -ForegroundColor White
Write-Host "3. Настройте резервное копирование базы данных" -ForegroundColor White
Write-Host "4. Настройте мониторинг и алерты" -ForegroundColor White
Write-Host "5. Протестируйте развертывание на staging окружении" -ForegroundColor White
