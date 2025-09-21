# PowerShell script to update ApiResponse usage in controllers
$controllers = @(
    "src/Inventory.API/Controllers/UserController.cs",
    "src/Inventory.API/Controllers/CategoryController.cs", 
    "src/Inventory.API/Controllers/LocationController.cs",
    "src/Inventory.API/Controllers/WarehouseController.cs",
    "src/Inventory.API/Controllers/UnitOfMeasureController.cs",
    "src/Inventory.API/Controllers/ProductController.cs",
    "src/Inventory.API/Controllers/ProductGroupController.cs",
    "src/Inventory.API/Controllers/DashboardController.cs",
    "src/Inventory.API/Controllers/ManufacturerController.cs",
    "src/Inventory.API/Controllers/ProductModelController.cs",
    "src/Inventory.API/Controllers/NotificationController.cs",
    "src/Inventory.API/Controllers/TransactionController.cs",
    "src/Inventory.API/Controllers/ReferenceDataController.cs"
)

foreach ($controller in $controllers) {
    if (Test-Path $controller) {
        Write-Host "Processing $controller..."
        
        # Read file content
        $content = Get-Content $controller -Raw
        
        # Replace patterns
        $content = $content -replace 'new ApiResponse<([^>]+)>\s*\{\s*Success = true,?\s*Data = ([^}]+)\s*\}', 'ApiResponse<$1>.Success($2)'
        $content = $content -replace 'new ApiResponse<([^>]+)>\s*\{\s*Success = false,?\s*ErrorMessage = "([^"]+)"\s*\}', 'ApiResponse<$1>.Failure("$2")'
        $content = $content -replace 'new PagedApiResponse<([^>]+)>\s*\{\s*Success = true,?\s*Data = ([^}]+)\s*\}', 'PagedApiResponse<$1>.Success($2)'
        $content = $content -replace 'new PagedApiResponse<([^>]+)>\s*\{\s*Success = false,?\s*ErrorMessage = "([^"]+)"\s*\}', 'PagedApiResponse<$1>.Failure("$2")'
        
        # Write back
        Set-Content $controller -Value $content -NoNewline
        Write-Host "Updated $controller"
    } else {
        Write-Host "File not found: $controller"
    }
}

Write-Host "All controllers updated!"
