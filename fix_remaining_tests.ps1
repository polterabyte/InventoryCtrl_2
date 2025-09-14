# PowerShell script to fix remaining integration tests

$testFiles = @(
    "test/Inventory.IntegrationTests/Controllers/CategoryControllerIntegrationTests.cs",
    "test/Inventory.IntegrationTests/Controllers/DashboardControllerIntegrationTests.cs",
    "test/Inventory.IntegrationTests/Controllers/SimpleIntegrationTests.cs"
)

foreach ($file in $testFiles) {
    Write-Host "Processing $file..."
    
    $content = Get-Content $file -Raw
    
    # Pattern to match test methods that don't have authentication setup
    $pattern = '(\[Fact\]\s+public async Task \w+\(\)\s*\{\s*// Arrange\s*)(// Act)'
    
    # Replacement pattern that adds InitializeAsync and SetAuthHeaderAsync
    $replacement = '$1await InitializeAsync();
        await SetAuthHeaderAsync();

        // Act'
    
    # Apply the replacement
    $newContent = $content -replace $pattern, $replacement
    
    # Write the updated content back to the file
    Set-Content $file -Value $newContent -NoNewline
    
    Write-Host "Updated $file"
}

Write-Host "All remaining test files have been updated!"
