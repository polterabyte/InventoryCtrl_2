# build-with-ports.ps1
# Build script that automatically applies port configuration

param(
    [string]$Configuration = "Debug",
    [string]$ProjectPath = ".",
    [switch]$Clean,
    [switch]$Watch
)

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

Write-ColorOutput "=== Building with Port Configuration ===" -Color Green

# Step 1: Apply port configuration
Write-ColorOutput "1. Applying port configuration..." -Color Cyan
& "$PSScriptRoot\scripts\Sync-Ports.ps1" -ToAppSettings

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "Failed to apply port configuration" -Color Red
    exit 1
}

# Step 2: Clean if requested
if ($Clean) {
    Write-ColorOutput "2. Cleaning solution..." -Color Cyan
    dotnet clean "$ProjectPath\InventoryCtrl_2.sln" --configuration $Configuration
}

# Step 3: Build solution
Write-ColorOutput "3. Building solution..." -Color Cyan
dotnet build "$ProjectPath\InventoryCtrl_2.sln" --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "Build failed" -Color Red
    exit 1
}

Write-ColorOutput "Build completed successfully!" -Color Green

# Step 4: Start watching if requested
if ($Watch) {
    Write-ColorOutput "4. Starting port configuration watcher..." -Color Cyan
    Write-ColorOutput "Modify ports.json to automatically apply changes" -Color Yellow
    & "$PSScriptRoot\scripts\Watch-Ports.ps1"
}
