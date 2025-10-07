Param(
    [string]$Solution = 'InventoryCtrl.sln',
    [int]$Top = 10,
    [string]$Sarif = '',
    [string]$Json = ''
)

$ErrorActionPreference = 'Stop'

Write-Host "Building $Solution to capture analyzer warnings..." -ForegroundColor Cyan

# Ensure artifacts directory if export requested
if ($Sarif -or $Json) {
  $artifacts = Join-Path (Get-Location) 'artifacts'
  if (!(Test-Path $artifacts)) { New-Item -ItemType Directory -Path $artifacts | Out-Null }
}

# Capture output while also printing it
$buildArgs = @('build', $Solution, '-t:Rebuild', '-nologo', '-v', 'm')
$lines = & dotnet @buildArgs 2>&1 | ForEach-Object { $_.ToString() }

Write-Host "`n--- Analyzer Warnings Summary ---" -ForegroundColor Yellow

$warningRegex = [regex]'warning\s+([A-Z]{2,4}\d{4})'
$errorRegex   = [regex]'error\s+([A-Z]{2,4}\d{4})'
$pathRegex    = [regex]'^(?<path>.*?\.cs)\('
$warnLines = $lines | Where-Object { $warningRegex.IsMatch($_) }
$errLines  = $lines | Where-Object { $errorRegex.IsMatch($_) }

if (-not $warnLines) {
    Write-Host "No analyzer warnings found." -ForegroundColor Green
    if ($Json) {
        $obj = [pscustomobject]@{ rules=@(); files=@() }
        $obj | ConvertTo-Json -Depth 5 | Out-File -FilePath $Json -Encoding utf8
        Write-Host "Saved empty JSON summary to $Json" -ForegroundColor DarkGreen
    }
    exit 0
}

$ruleCounts = $warnLines | ForEach-Object {
    ($warningRegex.Match($_).Groups[1].Value)
} | Group-Object | Sort-Object Count -Descending

Write-Host ("Top {0} warning rules:" -f [Math]::Min($Top, $ruleCounts.Count)) -ForegroundColor Yellow
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

# Errors summary
if ($errLines -and $errLines.Count -gt 0) {
    Write-Host "`n--- Compilation Errors Summary ---" -ForegroundColor Red
    $errCounts = $errLines | ForEach-Object { ($errorRegex.Match($_).Groups[1].Value) } | Group-Object | Sort-Object Count -Descending
    Write-Host ("Top {0} error codes:" -f [Math]::Min($Top, $errCounts.Count)) -ForegroundColor Red
    $errCounts | Select-Object -First $Top | ForEach-Object {
        "  {0,-8} {1,5}" -f $_.Name, $_.Count
    } | Write-Host

    $errFiles = $errLines | ForEach-Object {
        $m = $pathRegex.Match($_)
        if ($m.Success) { $m.Groups['path'].Value }
    } | Where-Object { $_ }

    if ($errFiles) {
        Write-Host "`nTop files by errors:" -ForegroundColor Red
        $errFiles | Group-Object | Sort-Object Count -Descending | Select-Object -First 10 | ForEach-Object {
            "  {0}  ({1})" -f $_.Name, $_.Count
        } | Write-Host
    }
}

if ($Json) {
    $rulesJson = $ruleCounts | Select-Object Name,Count
    $filesJson = $srcFiles | Group-Object | Sort-Object Count -Descending | Select-Object -First 50 | ForEach-Object {
        [pscustomobject]@{ path=$_.Name; count=$_.Count }
    }
    $errRulesJson = @()
    $errFilesJson = @()
    if ($errLines -and $errLines.Count -gt 0) {
        $errCounts = $errLines | ForEach-Object { ($errorRegex.Match($_).Groups[1].Value) } | Group-Object | Sort-Object Count -Descending
        $errRulesJson = $errCounts | Select-Object Name,Count
        $errFiles = $errLines | ForEach-Object {
            $m = $pathRegex.Match($_)
            if ($m.Success) { $m.Groups['path'].Value }
        } | Where-Object { $_ }
        if ($errFiles) {
            $errFilesJson = $errFiles | Group-Object | Sort-Object Count -Descending | Select-Object -First 50 | ForEach-Object {
                [pscustomobject]@{ path=$_.Name; count=$_.Count }
            }
        }
    }
    $obj = [pscustomobject]@{ warnings=@{ rules=$rulesJson; files=$filesJson }; errors=@{ rules=$errRulesJson; files=$errFilesJson } }
    $obj | ConvertTo-Json -Depth 5 | Out-File -FilePath $Json -Encoding utf8
    Write-Host "Saved JSON summary to $Json" -ForegroundColor DarkGreen
}

if ($Sarif) {
    Write-Host "Writing SARIF to $Sarif ..." -ForegroundColor DarkCyan
    & dotnet build $Solution -nologo -v m -p:ErrorLog=$Sarif  | Out-Null
}

Write-Host "`nTip: run 'dotnet format analyzers' to auto-fix some style issues." -ForegroundColor DarkCyan
