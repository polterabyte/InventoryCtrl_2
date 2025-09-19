# Скрипт для проверки и настройки файрвола Windows
param(
    [Parameter(Mandatory=$false)]
    [switch]$AddRules,
    
    [Parameter(Mandatory=$false)]
    [switch]$RemoveRules
)

Write-Host "🔥 Проверка и настройка файрвола Windows" -ForegroundColor Cyan
Write-Host ""

# Функция для проверки статуса файрвола
function Test-FirewallStatus {
    Write-Host "📊 Проверка статуса файрвола..." -ForegroundColor Yellow
    
    $firewallProfiles = @("Domain", "Private", "Public")
    
    foreach ($profile in $firewallProfiles) {
        $status = Get-NetFirewallProfile -Profile $profile | Select-Object Name, Enabled
        $statusText = if ($status.Enabled) { "Включен" } else { "Отключен" }
        $color = if ($status.Enabled) { "Green" } else { "Red" }
        
        Write-Host "   $($profile): $statusText" -ForegroundColor $color
    }
    
    return $status.Enabled
}

# Функция для проверки правил портов
function Test-PortRules {
    param([int[]]$Ports)
    
    Write-Host "🔍 Проверка правил для портов: $($Ports -join ', ')" -ForegroundColor Yellow
    
    foreach ($port in $Ports) {
        $rules = Get-NetFirewallRule -DisplayName "*$port*" -ErrorAction SilentlyContinue
        $inboundRules = Get-NetFirewallRule -DisplayName "*$port*" | Where-Object { $_.Direction -eq "Inbound" } | Where-Object { $_.Enabled -eq "True" }
        $outboundRules = Get-NetFirewallRule -DisplayName "*$port*" | Where-Object { $_.Direction -eq "Outbound" } | Where-Object { $_.Enabled -eq "True" }
        
        Write-Host "   Port $port:" -ForegroundColor White
        Write-Host "     Входящие правила: $($inboundRules.Count)" -ForegroundColor $(if ($inboundRules.Count -gt 0) { "Green" } else { "Red" })
        Write-Host "     Исходящие правила: $($outboundRules.Count)" -ForegroundColor $(if ($outboundRules.Count -gt 0) { "Green" } else { "Red" })
        
        if ($inboundRules.Count -gt 0) {
            foreach ($rule in $inboundRules) {
                Write-Host "       ✅ $($rule.DisplayName)" -ForegroundColor Green
            }
        }
        
        if ($outboundRules.Count -gt 0) {
            foreach ($rule in $outboundRules) {
                Write-Host "       ✅ $($rule.DisplayName)" -ForegroundColor Green
            }
        }
    }
}

# Функция для добавления правил файрвола
function Add-PortRules {
    param([int[]]$Ports)
    
    Write-Host "➕ Добавление правил файрвола для портов: $($Ports -join ', ')" -ForegroundColor Yellow
    
    foreach ($port in $Ports) {
        $ruleName = "Inventory_App_Port_$port"
        
        try {
            # Удаляем существующие правила с таким же именем
            Remove-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
            
            # Добавляем входящее правило
            New-NetFirewallRule -DisplayName "$ruleName_Inbound" -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow -Profile Any | Out-Null
            Write-Host "   ✅ Добавлено входящее правило для порта $port" -ForegroundColor Green
            
            # Добавляем исходящее правило
            New-NetFirewallRule -DisplayName "$ruleName_Outbound" -Direction Outbound -Protocol TCP -LocalPort $port -Action Allow -Profile Any | Out-Null
            Write-Host "   ✅ Добавлено исходящее правило для порта $port" -ForegroundColor Green
            
        } catch {
            Write-Host "   ❌ Ошибка добавления правила для порта $port`: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Функция для удаления правил файрвола
function Remove-PortRules {
    param([int[]]$Ports)
    
    Write-Host "🗑️  Удаление правил файрвола для портов: $($Ports -join ', ')" -ForegroundColor Yellow
    
    foreach ($port in $Ports) {
        $ruleName = "Inventory_App_Port_$port"
        
        try {
            Remove-NetFirewallRule -DisplayName "*$ruleName*" -ErrorAction SilentlyContinue
            Write-Host "   ✅ Удалены правила для порта $port" -ForegroundColor Green
        } catch {
            Write-Host "   ❌ Ошибка удаления правил для порта $port`: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Функция для проверки доступности портов
function Test-PortAccessibility {
    param([int[]]$Ports)
    
    Write-Host "🌐 Проверка доступности портов..." -ForegroundColor Yellow
    
    foreach ($port in $Ports) {
        try {
            $listener = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
            if ($listener) {
                Write-Host "   ✅ Порт $port открыт и прослушивается" -ForegroundColor Green
                Write-Host "      Процесс: $($listener.OwningProcess)" -ForegroundColor Gray
                Write-Host "      Состояние: $($listener.State)" -ForegroundColor Gray
            } else {
                Write-Host "   ⚠️  Порт $port не прослушивается" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "   ❌ Ошибка проверки порта $port`: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Основная логика
$requiredPorts = @(80, 5000)

Write-Host "🔧 Проверка файрвола для портов: $($requiredPorts -join ', ')" -ForegroundColor Cyan
Write-Host ""

# Проверяем, запущен ли скрипт от имени администратора
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "⚠️  ВНИМАНИЕ: Скрипт должен быть запущен от имени администратора для изменения правил файрвола!" -ForegroundColor Yellow
    Write-Host "   Для добавления/удаления правил используйте: .\check-firewall.ps1 -AddRules" -ForegroundColor Yellow
    Write-Host ""
}

# 1. Проверяем статус файрвола
$firewallEnabled = Test-FirewallStatus
Write-Host ""

# 2. Проверяем правила портов
Test-PortRules -Ports $requiredPorts
Write-Host ""

# 3. Проверяем доступность портов
Test-PortAccessibility -Ports $requiredPorts
Write-Host ""

# 4. Добавляем правила если запрошено
if ($AddRules) {
    if ($firewallEnabled) {
        Add-PortRules -Ports $requiredPorts
        Write-Host ""
        Write-Host "🔄 Повторная проверка правил после добавления:" -ForegroundColor Cyan
        Test-PortRules -Ports $requiredPorts
    } else {
        Write-Host "⚠️  Файрвол отключен, правила не требуются" -ForegroundColor Yellow
    }
}

# 5. Удаляем правила если запрошено
if ($RemoveRules) {
    Remove-PortRules -Ports $requiredPorts
    Write-Host ""
    Write-Host "🔄 Повторная проверка правил после удаления:" -ForegroundColor Cyan
    Test-PortRules -Ports $requiredPorts
}

Write-Host ""
Write-Host "📝 Инструкции:" -ForegroundColor White
Write-Host "   1. Для добавления правил файрвола: .\check-firewall.ps1 -AddRules" -ForegroundColor White
Write-Host "   2. Для удаления правил файрвола: .\check-firewall.ps1 -RemoveRules" -ForegroundColor White
Write-Host "   3. Для проверки доступности портов извне используйте: telnet [IP_СЕРВЕРА] 80" -ForegroundColor White
Write-Host "   4. Or use online port checking services" -ForegroundColor White
