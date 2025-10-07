Param(
  [string]$Configuration = 'Release',
  [string]$Solution = 'InventoryCtrl.sln'
)

$ErrorActionPreference = 'Stop'

Write-Host "== CI Check ==" -ForegroundColor Cyan
Write-Host "Restoring tools and packages..." -ForegroundColor Cyan
dotnet tool restore
dotnet restore $Solution -nologo

Write-Host "Formatting verification..." -ForegroundColor Cyan
dotnet format $Solution --verify-no-changes --severity info

Write-Host "Building with analyzers (SARIF export)..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path artifacts | Out-Null
dotnet build $Solution -c $Configuration -nologo -v m -p:ContinuousIntegrationBuild=true -p:ErrorLog=artifacts/analyzers.sarif

Write-Host "Running unit tests with coverage..." -ForegroundColor Cyan
./test/run-tests.ps1 -Project unit -Configuration $Configuration -Coverage | Write-Host

Write-Host "Generating coverage HTML report..." -ForegroundColor Cyan
$covFiles = Get-ChildItem -Recurse -Path . -Filter "coverage.cobertura.xml" | Select-Object -ExpandProperty FullName
if ($covFiles) {
  dotnet tool run reportgenerator -reports:"$($covFiles -join ';')" -targetdir:"artifacts/coverage" -reporttypes:"HtmlInline_AzurePipelines;Cobertura"
  Write-Host "Coverage report generated at artifacts/coverage/index.html" -ForegroundColor Green
} else {
  Write-Warning "No coverage.cobertura.xml found. Skipping HTML report."
}

Write-Host "Quick analyzer summary..." -ForegroundColor Cyan
./scripts/quick-lint.ps1 -Solution $Solution -Top 15 -Sarif artifacts/analyzers.sarif -Json artifacts/warnings-summary.json

Write-Host "CI check completed." -ForegroundColor Green
