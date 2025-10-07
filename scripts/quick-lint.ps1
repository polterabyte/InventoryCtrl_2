Param(
    [string]$Solution = 'InventoryCtrl.sln',
    [int]$Top = 10
)

$ErrorActionPreference = 'Stop'

Write-Host "Building $Solution to capture analyzer warnings..." -ForegroundColor Cyan

# Capture output while also printing it
$lines = & dotnet build $Solution -t:Rebuild -nologo -v m 2>&1 | ForEach-Object { $_.ToString() }

Write-Host "`n--- Analyzer Warnings Summary ---" -ForegroundColor Yellow

$warningRegex = [regex]'warning\s+([A-Z]{2,4}\d{4})'
$pathRegex    = [regex]'^(?<path>.*?\.cs)\('
$warnLines = $lines | Where-Object { $warningRegex.IsMatch($_) }

if (-not $warnLines) {
    Write-Host "No analyzer warnings found." -ForegroundColor Green
    exit 0
}

$ruleCounts = $warnLines | ForEach-Object {
    ($warningRegex.Match($_).Groups[1].Value)
} | Group-Object | Sort-Object Count -Descending

Write-Host ("Top {0} rules:" -f [Math]::Min($Top, $ruleCounts.Count)) -ForegroundColor Yellow
$ruleCounts | Select-Object -First $Top | ForEach-Object {
    "  {0,-8} {1,5}" -f $_.Name, $_.Count
} | Write-Host

# Top files in src with warnings
$srcFiles = $warnLines | ForEach-Object {
    $m = $pathRegex.Match($_)
    if ($m.Success) { $m.Groups['path'].Value }
} | Where-Object { $_ -like '*\src\*' -or $_ -like '*/src/*' }

if ($srcFiles) {
    Write-Host "`nTop files in src by warnings:" -ForegroundColor Yellow
    $srcFiles | Group-Object | Sort-Object Count -Descending | Select-Object -First 10 | ForEach-Object {
        "  {0}  ({1})" -f $_.Name, $_.Count
    } | Write-Host
}

Write-Host "`nTip: run 'dotnet format analyzers' to auto-fix some style issues." -ForegroundColor DarkCyan
