# Script to fix remaining RadzenTemplateForm context conflicts

$filesToFix = @(
    "src/Inventory.UI/Components/Admin/ProductModelManagementWidget.razor",
    "src/Inventory.UI/Components/Admin/UnitOfMeasureManagementWidget.razor",
    "src/Inventory.UI/Components/Admin/WarehouseManagementWidget.razor",
    "src/Inventory.UI/Components/Admin/ProductGroupManagementWidget.razor"
)

foreach ($file in $filesToFix) {
    if (Test-Path $file) {
        Write-Host "Processing file: $file"
        
        $content = Get-Content $file -Raw
        $originalContent = $content
        
        # Find RadzenTemplateForm with Submit and add ChildContent after it
        $pattern = '(<RadzenTemplateForm[^>]*Submit[^>]*>)(\s*)(<RadzenStack>)'
        $replacement = '$1<ChildContent Context="formContext">$2$3'
        
        $content = $content -replace $pattern, $replacement
        
        # Find the closing RadzenStack before RadzenTemplateForm and add closing ChildContent
        $pattern = '(\s*)(</RadzenStack>)(\s*)(</RadzenTemplateForm>)'
        $replacement = '$1$2$3</ChildContent>$4'
        
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
