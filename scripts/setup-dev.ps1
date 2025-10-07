Param()
$ErrorActionPreference = 'Stop'

Write-Host "Ensuring dotnet tool manifest and tools..." -ForegroundColor Cyan
if (!(Test-Path ".config/dotnet-tools.json")) {
  dotnet new tool-manifest | Out-Null
}

dotnet tool restore

# Install/update key tools (idempotent)
dotnet tool update dotnet-outdated-tool --local | Out-Null
dotnet tool update dotnet-reportgenerator-globaltool --local | Out-Null
dotnet tool update coverlet.console --local | Out-Null
dotnet tool update dotnet-format --local | Out-Null

Write-Host "Tools ready. Run 'dotnet tool list --local' to verify." -ForegroundColor Green

