# Clean-BuildArtifacts.ps1
# Cleans obj and bin directories from all projects

param(
    [switch]$Force
)

Write-Host "Cleaning build artifacts..." -ForegroundColor Green

# Get all project directories
$projectDirs = @(
    "src\Inventory.API",
    "src\Inventory.Shared", 
    "src\Inventory.UI",
    "src\Inventory.Web.Client"
)

$cleanedCount = 0

foreach ($dir in $projectDirs) {
    if (Test-Path $dir) {
        # Clean obj directories
        $objDir = Join-Path $dir "obj"
        if (Test-Path $objDir) {
            Write-Host "Cleaning obj directory: $objDir" -ForegroundColor Yellow
            Remove-Item -Path $objDir -Recurse -Force
            $cleanedCount++
        }
        
        # Clean bin directories
        $binDir = Join-Path $dir "bin"
        if (Test-Path $binDir) {
            Write-Host "Cleaning bin directory: $binDir" -ForegroundColor Yellow
            Remove-Item -Path $binDir -Recurse -Force
            $cleanedCount++
        }
    }
}

# Clean solution-level obj/bin if they exist
$solutionObjDir = "obj"
$solutionBinDir = "bin"

if (Test-Path $solutionObjDir) {
    Write-Host "Cleaning solution obj directory: $solutionObjDir" -ForegroundColor Yellow
    Remove-Item -Path $solutionObjDir -Recurse -Force
    $cleanedCount++
}

if (Test-Path $solutionBinDir) {
    Write-Host "Cleaning solution bin directory: $solutionBinDir" -ForegroundColor Yellow
    Remove-Item -Path $solutionBinDir -Recurse -Force
    $cleanedCount++
}

Write-Host "Cleaned $cleanedCount directories" -ForegroundColor Green
Write-Host "Build artifacts cleanup completed!" -ForegroundColor Green
