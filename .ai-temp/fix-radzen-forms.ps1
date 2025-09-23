# Script to fix context conflict in RadzenTemplateForm inside AuthorizeView

$filesToFix = @(
    "src/Inventory.UI/Components/Admin/ProductModelManagementWidget.razor",
    "src/Inventory.UI/Components/Admin/UnitOfMeasureManagementWidget.razor",
    "src/Inventory.UI/Components/Admin/WarehouseManagementWidget.razor",
    "src/Inventory.UI/Components/Admin/ProductGroupManagementWidget.razor",
    "src/Inventory.UI/Components/Admin/ManufacturerManagementWidget.razor"
)

foreach ($file in $filesToFix) {
    if (Test-Path $file) {
        Write-Host "Processing file: $file"
        
        $content = Get-Content $file -Raw
        $originalContent = $content
        
        # Find RadzenTemplateForm with Submit and replace
        $pattern = '(<RadzenTemplateForm[^>]*Submit[^>]*>)(\s*<DataAnnotationsValidator />)'
        $replacement = '$1<ChildContent Context="formContext">$2'
        
        $content = $content -replace $pattern, $replacement
        
        # Find closing RadzenTemplateForm tag and add closing ChildContent
        $pattern = '(\s*)(</RadzenTemplateForm>)'
        $replacement = '$1</ChildContent>$2'
        
        $content = $content -replace $pattern, $replacement
        
        if ($content -ne $originalContent) {
            Set-Content $file -Value $content -NoNewline
            Write-Host "File $file fixed"
        } else {
            Write-Host "File $file does not need changes"
        }
    } else {
        Write-Host "File $file not found"
    }
}

Write-Host "Fix completed"