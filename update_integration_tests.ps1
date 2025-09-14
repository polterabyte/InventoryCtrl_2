# PowerShell script to add authentication to all integration tests

$testFiles = @(
    "test/Inventory.IntegrationTests/Controllers/CategoryControllerIntegrationTests.cs",
    "test/Inventory.IntegrationTests/Controllers/DashboardControllerIntegrationTests.cs",
    "test/Inventory.IntegrationTests/Controllers/SimpleIntegrationTests.cs"
)

foreach ($file in $testFiles) {
    Write-Host "Processing $file..."
    
    $content = Get-Content $file -Raw
    
    # Pattern to match test methods that need authentication
    $pattern = '(\[Fact\]\s+public async Task \w+\(\)\s*\{\s*// Arrange\s*)(await SeedTestDataAsync\(\);|// Act)'
    
    # Replacement pattern that adds InitializeAsync and SetAuthHeaderAsync
    $replacement = '$1await InitializeAsync();
        await SeedTestDataAsync();
        await SetAuthHeaderAsync();

        // Act'
    
    # Apply the replacement
    $newContent = $content -replace $pattern, $replacement
    
    # Also handle tests that don't call SeedTestDataAsync
    $pattern2 = '(\[Fact\]\s+public async Task \w+\(\)\s*\{\s*// Arrange\s*)(// Act)'
    $replacement2 = '$1await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act'
    
    $newContent = $newContent -replace $pattern2, $replacement2
    
    # Write the updated content back to the file
    Set-Content $file -Value $newContent -NoNewline
    
    Write-Host "Updated $file"
}

Write-Host "All test files have been updated!"
