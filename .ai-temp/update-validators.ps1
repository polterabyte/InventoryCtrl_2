# Скрипт для добавления IRequestValidator в конструкторы API сервисов
$files = @(
    "src/Inventory.Web.Client/Services/WebManufacturerApiService.cs",
    "src/Inventory.Web.Client/Services/WebCategoryApiService.cs",
    "src/Inventory.Web.Client/Services/WebDashboardApiService.cs",
    "src/Inventory.Web.Client/Services/WebAuthApiService.cs",
    "src/Inventory.Web.Client/Services/WebAuditApiService.cs",
    "src/Inventory.Web.Client/Services/WebLocationApiService.cs",
    "src/Inventory.Web.Client/Services/WebProductGroupApiService.cs",
    "src/Inventory.Web.Client/Services/WebProductModelApiService.cs",
    "src/Inventory.Web.Client/Services/WebUnitOfMeasureApiService.cs",
    "src/Inventory.Web.Client/Services/WebWarehouseApiService.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "Updating $file"
        
        # Читаем содержимое файла
        $content = Get-Content $file -Raw
        
        # Добавляем IRequestValidator в конструктор
        $content = $content -replace 'IApiErrorHandler errorHandler,', 'IApiErrorHandler errorHandler,`n        IRequestValidator requestValidator,'
        $content = $content -replace ': base\(httpClient, urlBuilderService, resilientApiService, errorHandler, logger\)', ': base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, logger)'
        
        # Сохраняем файл
        Set-Content $file -Value $content -NoNewline
        Write-Host "Updated $file"
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "All files updated with IRequestValidator!"
