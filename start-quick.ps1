# Быстрый запуск приложения
# Inventory Control System v2 - Quick Start

param(
    [switch]$ApiOnly,
    [switch]$ClientOnly
)

Write-Host "⚡ Быстрый запуск Inventory Control System v2" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

$startScript = Join-Path (Split-Path -Parent $PSScriptRoot) "start-apps.ps1"

if (Test-Path $startScript) {
    $args = @("-Quick")
    if ($ApiOnly) { $args += "-ApiOnly" }
    if ($ClientOnly) { $args += "-ClientOnly" }
    
    & $startScript @args
} else {
    Write-Host "❌ Основной скрипт запуска не найден: $startScript" -ForegroundColor Red
    exit 1
}
