# Скрипт для исправления конструкторов с IRequestValidator
$files = @(
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
        Write-Host "Fixing $file"
        
        # Читаем содержимое файла
        $content = Get-Content $file -Raw
        
        # Исправляем неправильные символы
        $content = $content -replace '`n', ''
        $content = $content -replace 'IApiErrorHandler errorHandler, IRequestValidator requestValidator,', "IApiErrorHandler errorHandler,`n        IRequestValidator requestValidator,"
        
        # Сохраняем файл
        Set-Content $file -Value $content -NoNewline
        Write-Host "Fixed $file"
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "All files fixed!"
