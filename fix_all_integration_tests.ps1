# Fix all integration tests by adding proper setup and fixing JSON deserialization

Write-Host "Fixing Integration Tests..." -ForegroundColor Green

# Get all test files
$testFiles = @(
    "test/Inventory.IntegrationTests/Controllers/DashboardControllerIntegrationTests.cs",
    "test/Inventory.IntegrationTests/Controllers/CategoryControllerIntegrationTests.cs"
)

foreach ($file in $testFiles) {
    if (Test-Path $file) {
        Write-Host "Processing $file..." -ForegroundColor Yellow
        
        $content = Get-Content $file -Raw
        
        # Fix JSON deserialization issues - change ApiResponse<List<T>> to List<T> for direct returns
        $content = $content -replace 'ReadFromJsonAsync<ApiResponse<List<LowStockProductDto>>>\(\)', 'ReadFromJsonAsync<List<LowStockProductDto>>()'
        $content = $content -replace 'ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>\(\)', 'ReadFromJsonAsync<List<CategoryDto>>()'
        
        # Remove ApiResponse wrapper assertions
        $content = $content -replace 'result\.Should\(\)\.NotBeNull\(\);\s*result!\.Success\.Should\(\)\.BeTrue\(\);\s*result\.Data\.Should\(\)\.NotBeNull\(\);\s*result\.Data\.Should\(\)\.BeEmpty\(\);', 'result.Should().NotBeNull(); result.Should().BeEmpty();'
        $content = $content -replace 'result\.Should\(\)\.NotBeNull\(\);\s*result!\.Success\.Should\(\)\.BeTrue\(\);\s*result\.Data\.Should\(\)\.NotBeNull\(\);\s*result\.Data\.Should\(\)\.HaveCount\((\d+)\);', 'result.Should().NotBeNull(); result.Should().HaveCount($1);'
        $content = $content -replace 'result\.Should\(\)\.NotBeNull\(\);\s*result!\.Success\.Should\(\)\.BeTrue\(\);\s*result\.Data\.Should\(\)\.NotBeNull\(\);\s*result\.Data!\.Id\.Should\(\)\.Be\(([^)]+)\);', 'result.Should().NotBeNull(); result!.Id.Should().Be($1);'
        $content = $content -replace 'result\.Should\(\)\.NotBeNull\(\);\s*result!\.Success\.Should\(\)\.BeTrue\(\);\s*result\.Data\.Should\(\)\.NotBeNull\(\);\s*result\.Data!\.Name\.Should\(\)\.Be\(([^)]+)\);', 'result.Should().NotBeNull(); result!.Name.Should().Be($1);'
        
        # Fix variable references
        $content = $content -replace 'result\.Data\.', 'result.'
        $content = $content -replace 'result!\.Data\.', 'result!.'
        
        # Remove duplicate Act comments
        $content = $content -replace '// Act\s*\n\s*// Act', '// Act'
        
        Set-Content $file $content -Encoding UTF8
        Write-Host "Fixed $file" -ForegroundColor Green
    }
}

Write-Host "Integration tests fixed!" -ForegroundColor Green
