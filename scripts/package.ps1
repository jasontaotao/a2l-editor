# scripts/package.ps1 - publish self-contained single-file exe (v0.1.1)
#
# Wraps `dotnet publish` for the CLI project. Outputs to <projectRoot>/publish/.

param([string]$Configuration = "Release")
$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot

$publishDir = Join-Path $ProjectRoot "publish"
Remove-Item -Recurse -Force $publishDir -ErrorAction SilentlyContinue

dotnet publish $ProjectRoot/src/A2lEditor.Cli/A2lEditor.Cli.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

Write-Host "Published to $publishDir"
Get-ChildItem $publishDir