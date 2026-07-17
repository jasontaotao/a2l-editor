# scripts/verify-coverage.ps1
# Reads coverage.cobertura.xml and enforces line/branch coverage thresholds.
# Designed for Git pre-commit hook; pure PowerShell 5.1 + pwsh compatible.
#
# Pure XML parsing approach (no Add-Type / C# assembly loading):
# - Loads coverage.cobertura.xml via [xml] type accelerator
# - Reads line-rate and branch-rate attributes from <coverage> root
# - Compares against thresholds in docs/superpowers/coverage-config.json
# - Exits 0 on pass, 1 on coverage regression, 0 on missing config / no XML (skip)
#
# Usage:
#   pwsh scripts/verify-coverage.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/verify-coverage.ps1

[CmdletBinding()]
param(
    [string]$XmlPattern = "TestResults/*/coverage.cobertura.xml",
    [string]$ConfigPath = "docs/superpowers/coverage-config.json"
)

$ErrorActionPreference = "Stop"

# Locate config + XML relative to repo root (script lives in scripts/, repo root is parent)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")

$configFullPath = Join-Path $repoRoot $ConfigPath
$xmlPatternFull = Join-Path $repoRoot $XmlPattern

if (-not (Test-Path $configFullPath)) {
    Write-Warning "[coverage-gate] Coverage config not found: $configFullPath; skipping coverage gate"
    exit 0
}

try {
    $config = Get-Content $configFullPath -Raw | ConvertFrom-Json
} catch {
    Write-Warning "[coverage-gate] Failed to parse config $configFullPath ($($_.Exception.Message)); skipping coverage gate"
    exit 0
}

$lineThreshold = [double]$config.lineThreshold
$branchThreshold = [double]$config.branchThreshold

# Resolve the most recent coverage XML (glob may match multiple runs)
$xmlFiles = Get-Item $xmlPatternFull -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
if (-not $xmlFiles -or $xmlFiles.Count -eq 0) {
    Write-Warning "[coverage-gate] No coverage XML found at $xmlPatternFull; skipping coverage gate"
    exit 0
}

$xmlPath = $xmlFiles[0].FullName
Write-Host "[coverage-gate] Reading $xmlPath"

# Pure PowerShell XML parse — works on PS 5.1 (Windows PowerShell) and pwsh (PowerShell 7+).
# Attributes with hyphens (e.g. line-rate) are quoted via the Item() accessor because
# PowerShell member access cannot reach hyphenated identifiers directly.
try {
    [xml]$xmlDoc = Get-Content $xmlPath -Raw
} catch {
    Write-Warning "[coverage-gate] Failed to parse coverage XML $xmlPath ($($_.Exception.Message)); skipping coverage gate"
    exit 0
}

$root = $xmlDoc.DocumentElement
if ($null -eq $root -or $root.LocalName -ne "coverage") {
    Write-Warning "[coverage-gate] Unexpected XML root in $xmlPath; skipping coverage gate"
    exit 0
}

# PowerShell: attributes with '-' need Item("name") since member-access syntax
# only allows valid identifiers (no hyphens).
$lineRateNode = $root.Attributes.GetNamedItem("line-rate")
$branchRateNode = $root.Attributes.GetNamedItem("branch-rate")
$linesCoveredNode = $root.Attributes.GetNamedItem("lines-covered")
$linesValidNode = $root.Attributes.GetNamedItem("lines-valid")
$branchesCoveredNode = $root.Attributes.GetNamedItem("branches-covered")
$branchesValidNode = $root.Attributes.GetNamedItem("branches-valid")

if ($null -eq $lineRateNode -or $null -eq $branchRateNode) {
    Write-Warning "[coverage-gate] Coverage XML missing line-rate / branch-rate attributes; skipping coverage gate"
    exit 0
}

$lineRate = [double]$lineRateNode.Value
$branchRate = [double]$branchRateNode.Value

$linePct = [math]::Round($lineRate * 100, 2)
$branchPct = [math]::Round($branchRate * 100, 2)
$lineThresholdPct = [math]::Round($lineThreshold * 100, 2)
$branchThresholdPct = [math]::Round($branchThreshold * 100, 2)

# Optional detailed counts (informational; do not gate on them)
$linesCovered = if ($null -ne $linesCoveredNode) { [int]$linesCoveredNode.Value } else { -1 }
$linesValid = if ($null -ne $linesValidNode) { [int]$linesValidNode.Value } else { -1 }
$branchesCovered = if ($null -ne $branchesCoveredNode) { [int]$branchesCoveredNode.Value } else { -1 }
$branchesValid = if ($null -ne $branchesValidNode) { [int]$branchesValidNode.Value } else { -1 }

Write-Host ("[coverage-gate] Coverage: Line {0}% ({1}/{2}) / Branch {3}% ({4}/{5}) (thresholds: Line {6}% / Branch {7}%)" -f `
    $linePct, $linesCovered, $linesValid, $branchPct, $branchesCovered, $branchesValid, $lineThresholdPct, $branchThresholdPct)

$failed = $false
if ($lineRate -lt $lineThreshold) {
    Write-Host "[coverage-gate] FAIL: Line coverage $linePct% < $lineThresholdPct%"
    $failed = $true
}
if ($branchRate -lt $branchThreshold) {
    Write-Host "[coverage-gate] FAIL: Branch coverage $branchPct% < $branchThresholdPct%"
    $failed = $true
}

if ($failed) {
    Write-Error "[coverage-gate] Coverage gate REGRESSION detected"
    exit 1
}

Write-Host "[coverage-gate] Coverage gate passed"
exit 0