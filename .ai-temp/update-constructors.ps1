# Скрипт для обновления конструкторов API сервисов с добавлением IApiErrorHandler
$files = @(
    "src/Inventory.Web.Client/Services/WebManufacturerApiService.cs",
    "src/Inventory.Web.Client/Services/WebCategoryApiService.cs",
    "src/Inventory.Web.Client/Services/WebDashboardApiService.cs",
    "src/Inventory.Web.Client/Services/WebAuditApiService.cs",
    "src/Inventory.Web.Client/Services/WebLocationApiService.cs", 
    "src/Inventory.Web.Client/Services/WebProductGroupApiService.cs",
    "src/Inventory.Web.Client/Services/WebUnitOfMeasureApiService.cs",
    "src/Inventory.Web.Client/Services/WebProductModelApiService.cs",
    "src/Inventory.Web.Client/Services/WebWarehouseApiService.cs",
    "src/Inventory.Web.Client/Services/WebAuthApiService.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "Updating $file"
        
        # Читаем содержимое файла
        $content = Get-Content $file -Raw
        
        # Добавляем IApiErrorHandler в конструктор
        $content = $content -replace 'IResilientApiService resilientApiService,', 'IResilientApiService resilientApiService, IApiErrorHandler errorHandler,'
        $content = $content -replace ': base\(httpClient, urlBuilderService, resilientApiService, logger\)', ': base(httpClient, urlBuilderService, resilientApiService, errorHandler, logger)'
        
        # Для WebApiServiceBase классов
        $content = $content -replace ': base\(httpClient, urlBuilderService, resilientApiService, logger\)', ': base(httpClient, urlBuilderService, resilientApiService, errorHandler, logger)'
        
        # Сохраняем файл
        Set-Content $file -Value $content -NoNewline
        Write-Host "Updated $file"
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "All files updated!"
