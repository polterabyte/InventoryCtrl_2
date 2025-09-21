# Скрипт для исправления конструкторов API сервисов
$files = @(
    "src/Inventory.Web.Client/Services/WebLocationApiService.cs",
    "src/Inventory.Web.Client/Services/WebProductGroupApiService.cs",
    "src/Inventory.Web.Client/Services/WebProductModelApiService.cs",
    "src/Inventory.Web.Client/Services/WebUnitOfMeasureApiService.cs",
    "src/Inventory.Web.Client/Services/WebWarehouseApiService.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "Fixing $file"
        
        # Читаем содержимое файла
        $content = Get-Content $file -Raw
        
        # Исправляем проблемные конструкторы - убираем лишние запятые и форматируем
        $content = $content -replace 'public \w+ApiService\([^)]+,\s*\)', 'public WebApiService('
        
        # Более точное исправление - ищем строки с лишними запятыми
        $lines = $content -split "`n"
        $fixedLines = @()
        
        foreach ($line in $lines) {
            if ($line -match 'public \w+ApiService\([^)]+,\s*\)') {
                # Исправляем конструктор
                $fixedLine = $line -replace ',\s*\)', ')'
                $fixedLine = $fixedLine -replace 'public \w+ApiService\(', "public WebApiService(`n        "
                $fixedLine = $fixedLine -replace 'HttpClient httpClient,', "HttpClient httpClient,`n        "
                $fixedLine = $fixedLine -replace 'IUrlBuilderService urlBuilderService,', "IUrlBuilderService urlBuilderService,`n        "
                $fixedLine = $fixedLine -replace 'IResilientApiService resilientApiService,', "IResilientApiService resilientApiService,`n        "
                $fixedLine = $fixedLine -replace 'IApiErrorHandler errorHandler,', "IApiErrorHandler errorHandler,`n        "
                $fixedLine = $fixedLine -replace 'ILogger<\w+> logger\)', "ILogger<WebApiService> logger)`n        "
                $fixedLine = $fixedLine -replace ': base\(', ": base(`n            "
                $fixedLine = $fixedLine -replace 'httpClient, urlBuilderService, resilientApiService, errorHandler, logger\)', "httpClient, urlBuilderService, resilientApiService, errorHandler, logger)"
                $fixedLines += $fixedLine
            } else {
                $fixedLines += $line
            }
        }
        
        $content = $fixedLines -join "`n"
        
        # Сохраняем файл
        Set-Content $file -Value $content -NoNewline
        Write-Host "Fixed $file"
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "All files fixed!"
