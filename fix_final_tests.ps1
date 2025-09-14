# PowerShell script to fix all remaining integration tests

$testFiles = @(
    "test/Inventory.IntegrationTests/Controllers/CategoryControllerIntegrationTests.cs",
    "test/Inventory.IntegrationTests/Controllers/DashboardControllerIntegrationTests.cs",
    "test/Inventory.IntegrationTests/Controllers/SimpleIntegrationTests.cs"
)

foreach ($file in $testFiles) {
    Write-Host "Processing $file..."
    
    $content = Get-Content $file -Raw
    
    # Pattern to match test methods that have InitializeAsync and SetAuthHeaderAsync but no SeedTestDataAsync
    $pattern = '(\[Fact\]\s+public async Task \w+\(\)\s*\{\s*// Arrange\s*await InitializeAsync\(\);\s*await SetAuthHeaderAsync\(\);\s*)(// Act)'
    
    # Replacement pattern that adds SeedTestDataAsync
    $replacement = '$1await SeedTestDataAsync();

        // Act'
    
    # Apply the replacement
    $newContent = $content -replace $pattern, $replacement
    
    # Write the updated content back to the file
    Set-Content $file -Value $newContent -NoNewline
    
    Write-Host "Updated $file"
}

Write-Host "All remaining test files have been updated!"
