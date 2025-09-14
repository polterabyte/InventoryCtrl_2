# Coverage Report Generator for Inventory Control System
# Generates detailed coverage reports from test results

param(
    [string]$OutputFormat = "html",
    [string]$InputDirectory = "test-results",
    [string]$OutputDirectory = "coverage-reports",
    [switch]$OpenReport,
    [switch]$Help
)

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Show-Help {
    Write-ColorOutput "Coverage Report Generator for Inventory Control System" -Color Cyan
    Write-ColorOutput ""
    Write-ColorOutput "Usage: .\Generate-Coverage-Report.ps1 [-OutputFormat <format>] [-InputDirectory <dir>] [-OutputDirectory <dir>] [-OpenReport] [-Help]" -Color Cyan
    Write-ColorOutput ""
    Write-ColorOutput "Parameters:" -Color Yellow
    Write-ColorOutput "  -OutputFormat    Output format (html, json, lcov, cobertura)" -Color Gray
    Write-ColorOutput "  -InputDirectory  Directory containing coverage files (default: test-results)" -Color Gray
    Write-ColorOutput "  -OutputDirectory Directory for generated reports (default: coverage-reports)" -Color Gray
    Write-ColorOutput "  -OpenReport      Open HTML report in browser after generation" -Color Gray
    Write-ColorOutput "  -Help            Show this help message" -Color Gray
    Write-ColorOutput ""
    Write-ColorOutput "Examples:" -Color Yellow
    Write-ColorOutput "  .\Generate-Coverage-Report.ps1                    # Generate HTML report" -Color Gray
    Write-ColorOutput "  .\Generate-Coverage-Report.ps1 -OutputFormat json  # Generate JSON report" -Color Gray
    Write-ColorOutput "  .\Generate-Coverage-Report.ps1 -OpenReport         # Generate and open HTML report" -Color Gray
}

if ($Help) {
    Show-Help
    exit 0
}

Write-ColorOutput "=== Coverage Report Generator ===" -Color Green

# Check if ReportGenerator tool is installed
$reportGeneratorPath = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
if (-not $reportGeneratorPath) {
    Write-ColorOutput "📦 Installing ReportGenerator tool..." -Color Yellow
    dotnet tool install --global dotnet-reportgenerator-globaltool
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Failed to install ReportGenerator tool" -Color Red
        exit 1
    }
    Write-ColorOutput "✅ ReportGenerator tool installed" -Color Green
}

# Check if input directory exists
if (!(Test-Path $InputDirectory)) {
    Write-ColorOutput "❌ Input directory '$InputDirectory' does not exist" -Color Red
    exit 1
}

# Create output directory
if (!(Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
    Write-ColorOutput "📁 Created output directory: $OutputDirectory" -Color Gray
}

# Find coverage files
$coverageFiles = Get-ChildItem -Path $InputDirectory -Recurse -Filter "coverage.cobertura.xml"
if ($coverageFiles.Count -eq 0) {
    Write-ColorOutput "❌ No coverage files found in '$InputDirectory'" -Color Red
    Write-ColorOutput "💡 Make sure to run tests with coverage first:" -Color Yellow
    Write-ColorOutput "   dotnet test --collect:'XPlat Code Coverage'" -Color Gray
    exit 1
}

Write-ColorOutput "📊 Found $($coverageFiles.Count) coverage file(s)" -Color Cyan

# Prepare input files argument
$inputFiles = ($coverageFiles | ForEach-Object { "`"$($_.FullName)`"" }) -join ";"

try {
    Write-ColorOutput "🔄 Generating coverage report..." -Color Yellow
    
    # Generate report
    $reportArgs = @(
        "-reports:$inputFiles",
        "-targetdir:$OutputDirectory",
        "-reporttypes:$OutputFormat"
    )
    
    if ($VerbosePreference -eq 'Continue') {
        $reportArgs += "-verbosity:Verbose"
    }
    
    reportgenerator @reportArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Coverage report generated successfully" -Color Green
        
        # Show summary
        $reportFiles = Get-ChildItem -Path $OutputDirectory -Recurse
        Write-ColorOutput "📄 Generated files:" -Color Cyan
        foreach ($file in $reportFiles) {
            $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
            Write-ColorOutput "  📄 $relativePath" -Color Gray
        }
        
        # Open HTML report if requested
        if ($OpenReport -and $OutputFormat -eq "html") {
            $htmlReport = Get-ChildItem -Path $OutputDirectory -Recurse -Filter "index.html" | Select-Object -First 1
            if ($htmlReport) {
                Write-ColorOutput "🌐 Opening HTML report in browser..." -Color Yellow
                Start-Process $htmlReport.FullName
            }
        }
        
    } else {
        Write-ColorOutput "❌ Failed to generate coverage report" -Color Red
        exit 1
    }
    
} catch {
    Write-ColorOutput "❌ Error generating coverage report: $($_.Exception.Message)" -Color Red
    exit 1
}

Write-ColorOutput ""
Write-ColorOutput "=== Coverage Report Generation Complete ===" -Color Green

# Show coverage summary if available
$summaryFile = Get-ChildItem -Path $OutputDirectory -Filter "Summary.xml" -ErrorAction SilentlyContinue
if ($summaryFile) {
    Write-ColorOutput "📊 Coverage Summary:" -Color Cyan
    try {
        [xml]$summary = Get-Content $summaryFile.FullName
        $coverage = $summary.Summary.Coverage
        Write-ColorOutput "  Line Coverage: $($coverage.linecoverage)%" -Color Gray
        Write-ColorOutput "  Branch Coverage: $($coverage.branchcoverage)%" -Color Gray
        Write-ColorOutput "  Method Coverage: $($coverage.methodcoverage)%" -Color Gray
    } catch {
        Write-ColorOutput "  Unable to parse coverage summary" -Color Yellow
    }
}
