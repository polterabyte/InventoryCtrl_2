# Скрипт для обновления конструкторов API сервисов
$files = @(
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
        
        # Заменяем старый конструктор на новый
        $content = $content -replace 'IApiUrlService apiUrlService,', 'IUrlBuilderService urlBuilderService,'
        $content = $content -replace 'IJSRuntime jsRuntime\)', ')'
        $content = $content -replace ': base\(httpClient, apiUrlService, resilientApiService, logger, jsRuntime\)', ': base(httpClient, urlBuilderService, resilientApiService, logger)'
        
        # Удаляем using Microsoft.JSInterop; если он больше не нужен
        $content = $content -replace 'using Microsoft\.JSInterop;\r?\n', ''
        
        # Сохраняем файл
        Set-Content $file -Value $content -NoNewline
        Write-Host "Updated $file"
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "All files updated!"
