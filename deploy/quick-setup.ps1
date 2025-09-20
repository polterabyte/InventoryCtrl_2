# Быстрая настройка для развертывания warehouse.cuby
param(
    [string]$Environment = "production",
    [string]$Domain = "warehouse.cuby",
    [string]$Email = "admin@warehouse.cuby"
)

Write-Host "🚀 Быстрая настройка для развертывания $Domain..." -ForegroundColor Green

# 1. Создание необходимых директорий
Write-Host "`n1. Создание директорий..." -ForegroundColor Cyan
$directories = @(
    "nginx/ssl",
    "logs",
    "backups"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "   ✅ Создана директория: $dir" -ForegroundColor Green
    } else {
        Write-Host "   ✅ Директория уже существует: $dir" -ForegroundColor Green
    }
}

# 2. Создание .env файла если не существует
Write-Host "`n2. Настройка переменных окружения..." -ForegroundColor Cyan
if (!(Test-Path ".env")) {
    if (Test-Path "env.$Environment") {
        Copy-Item "env.$Environment" ".env"
        Write-Host "   ✅ Создан .env файл из env.$Environment" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Файл env.$Environment не найден" -ForegroundColor Red
        Write-Host "   Создайте файл env.$Environment с необходимыми переменными" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ✅ .env файл уже существует" -ForegroundColor Green
}

# 3. Генерация SSL сертификатов (self-signed)
Write-Host "`n3. Генерация SSL сертификатов..." -ForegroundColor Cyan
$domains = @("warehouse.cuby", "staging.warehouse.cuby", "test.warehouse.cuby")

foreach ($domain in $domains) {
    $certFile = "nginx/ssl/$domain.crt"
    $keyFile = "nginx/ssl/$domain.key"
    
    if (!(Test-Path $certFile) -or !(Test-Path $keyFile)) {
        Write-Host "   ⚠️  Сертификат для $domain не найден" -ForegroundColor Yellow
        Write-Host "   Запустите: .\generate-ssl-warehouse.ps1" -ForegroundColor Cyan
    } else {
        Write-Host "   ✅ Сертификат для $domain найден" -ForegroundColor Green
    }
}

# 4. Проверка Docker
Write-Host "`n4. Проверка Docker..." -ForegroundColor Cyan
try {
    $dockerVersion = docker --version
    if ($dockerVersion) {
        Write-Host "   ✅ Docker установлен: $dockerVersion" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Docker не найден. Установите Docker:" -ForegroundColor Red
        Write-Host "   https://docs.docker.com/get-docker/" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Docker не установлен. Установите Docker:" -ForegroundColor Red
    Write-Host "   https://docs.docker.com/get-docker/" -ForegroundColor Cyan
}

# 5. Проверка Docker Compose
Write-Host "`n5. Проверка Docker Compose..." -ForegroundColor Cyan
try {
    $composeVersion = docker-compose --version
    if ($composeVersion) {
        Write-Host "   ✅ Docker Compose установлен: $composeVersion" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Docker Compose не найден. Установите Docker Compose:" -ForegroundColor Red
        Write-Host "   https://docs.docker.com/compose/install/" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Docker Compose не установлен. Установите Docker Compose:" -ForegroundColor Red
    Write-Host "   https://docs.docker.com/compose/install/" -ForegroundColor Cyan
}

# 6. Проверка портов
Write-Host "`n6. Проверка портов..." -ForegroundColor Cyan
$ports = @(80, 443)
foreach ($port in $ports) {
    try {
        $connection = Test-NetConnection -ComputerName "localhost" -Port $port -WarningAction SilentlyContinue
        if ($connection.TcpTestSucceeded) {
            Write-Host "   ✅ Порт $port открыт" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Порт $port закрыт. Убедитесь, что порт свободен" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   ⚠️  Не удалось проверить порт $port" -ForegroundColor Yellow
    }
}

# 7. Создание systemd сервиса (для Linux)
Write-Host "`n7. Создание systemd сервиса..." -ForegroundColor Cyan
$serviceFile = @"
[Unit]
Description=Warehouse Inventory Management
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=$PWD
ExecStart=/usr/local/bin/docker-compose -f docker-compose.prod.yml up -d
ExecStop=/usr/local/bin/docker-compose -f docker-compose.prod.yml down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
"@

$servicePath = "warehouse.service"
if (!(Test-Path $servicePath)) {
    $serviceFile | Out-File -FilePath $servicePath -Encoding UTF8
    Write-Host "   ✅ Создан файл $servicePath" -ForegroundColor Green
    Write-Host "   Скопируйте его в /etc/systemd/system/ на Linux сервере" -ForegroundColor Yellow
} else {
    Write-Host "   ✅ Файл $servicePath уже существует" -ForegroundColor Green
}

# 8. Создание скрипта бэкапа
Write-Host "`n8. Создание скрипта бэкапа..." -ForegroundColor Cyan
$backupScript = @"
#!/bin/bash
DATE=`date +%Y%m%d_%H%M%S`
BACKUP_DIR="$PWD/backups"
mkdir -p `$BACKUP_DIR

# Бэкап базы данных
docker exec inventory-postgres-prod pg_dump -U postgres inventorydb > `$BACKUP_DIR/db_`$DATE.sql

# Бэкап конфигураций
tar -czf `$BACKUP_DIR/config_`$DATE.tar.gz nginx/ *.yml *.env

# Удалить старые бэкапы (старше 30 дней)
find `$BACKUP_DIR -name "*.sql" -mtime +30 -delete
find `$BACKUP_DIR -name "*.tar.gz" -mtime +30 -delete
"@

$backupPath = "backup.sh"
if (!(Test-Path $backupPath)) {
    $backupScript | Out-File -FilePath $backupPath -Encoding UTF8
    Write-Host "   ✅ Создан скрипт бэкапа: $backupPath" -ForegroundColor Green
} else {
    Write-Host "   ✅ Скрипт бэкапа уже существует" -ForegroundColor Green
}

# Итоговый результат
Write-Host "`n" + "="*60 -ForegroundColor Gray
Write-Host "🎉 Быстрая настройка завершена!" -ForegroundColor Green
Write-Host "="*60 -ForegroundColor Gray

Write-Host "`n📋 Следующие шаги:" -ForegroundColor Yellow
Write-Host "1. Настройте DNS записи для $Domain" -ForegroundColor White
Write-Host "2. Сгенерируйте SSL сертификаты: .\generate-ssl-warehouse.ps1" -ForegroundColor White
Write-Host "3. Проверьте готовность: .\deploy\check-deployment-readiness.ps1" -ForegroundColor White
Write-Host "4. Разверните приложение: .\deploy\deploy-$Environment.ps1" -ForegroundColor White

Write-Host "`n🔧 Дополнительные настройки:" -ForegroundColor Yellow
Write-Host "• Настройте firewall для портов 80 и 443" -ForegroundColor White
Write-Host "• Настройте мониторинг и алерты" -ForegroundColor White
Write-Host "• Настройте автоматическое резервное копирование" -ForegroundColor White
Write-Host "• Протестируйте на staging окружении" -ForegroundColor White
