# PowerShell script to fix remaining ApiResponse patterns
$files = @(
    "src/Inventory.API/Controllers/CategoryController.cs",
    "src/Inventory.API/Controllers/UserController.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "Processing $file..."
        
        # Read file content
        $content = Get-Content $file -Raw
        
        # Fix common patterns
        $content = $content -replace 'new ApiResponse<([^>]+)>\s*\{\s*Success = true,?\s*Data = ([^}]+)\s*\}', 'ApiResponse<$1>.Success($2)'
        $content = $content -replace 'new ApiResponse<([^>]+)>\s*\{\s*Success = false,?\s*ErrorMessage = "([^"]+)"\s*\}', 'ApiResponse<$1>.Failure("$2")'
        $content = $content -replace 'new PagedApiResponse<([^>]+)>\s*\{\s*Success = true,?\s*Data = ([^}]+)\s*\}', 'PagedApiResponse<$1>.Success($2)'
        $content = $content -replace 'new PagedApiResponse<([^>]+)>\s*\{\s*Success = false,?\s*ErrorMessage = "([^"]+)"\s*\}', 'PagedApiResponse<$1>.Failure("$2")'
        
        # Fix multi-line patterns
        $content = $content -replace 'new ApiResponse<([^>]+)>\s*\{\s*Success = true,?\s*Data = ([^}]+)\s*\}', 'ApiResponse<$1>.Success($2)'
        $content = $content -replace 'new ApiResponse<([^>]+)>\s*\{\s*Success = false,?\s*ErrorMessage = "([^"]+)"\s*\}', 'ApiResponse<$1>.Failure("$2")'
        
        # Write back
        Set-Content $file -Value $content -NoNewline
        Write-Host "Updated $file"
    }
}

Write-Host "All files updated!"
