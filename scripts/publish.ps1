#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes Nexus as a self-contained, portable Windows build and packages it as a zip.

.DESCRIPTION
    Runs `dotnet publish` for the Nexus.App project targeting win-x64 with a bundled
    .NET runtime (self-contained), so the resulting folder runs on a machine without
    the .NET runtime installed. External tools (yt-dlp, FFmpeg) are then fetched into a
    "tools" subfolder next to Nexus.exe via scripts/fetch-tools.ps1, and the whole
    folder is compressed into dist\Nexus-<version>-win-x64-portable.zip.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.PARAMETER Runtime
    Runtime identifier. Defaults to win-x64.

.PARAMETER SingleFile
    Produce a single-file executable (Nexus.exe) instead of a folder of DLLs. The
    external tools still live in a separate "tools" folder beside the exe.

.PARAMETER NoTools
    Do not fetch yt-dlp/FFmpeg into the published output. Useful for CI that provisions
    the tools separately.

.PARAMETER NoZip
    Leave the published folder in place but do not create a zip archive.

.PARAMETER OutputRoot
    Root folder for build artifacts. Defaults to the repo's dist\ folder.

.EXAMPLE
    pwsh ./scripts/publish.ps1

.EXAMPLE
    pwsh ./scripts/publish.ps1 -SingleFile -NoTools
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [switch]$SingleFile,
    [switch]$NoTools,
    [switch]$NoZip,
    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project  = Join-Path $repoRoot 'src\Nexus.App\Nexus.App.csproj'
if (-not $OutputRoot) { $OutputRoot = Join-Path $repoRoot 'dist' }

# Read the product version straight from Directory.Build.props so artifacts are named
# consistently with the assembly.
[xml]$props = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props')
$version = @($props.Project.PropertyGroup.Version | Where-Object { $_ })[0]
if (-not $version) { $version = '0.0.0' }

$publishDir = Join-Path $OutputRoot ("Nexus-{0}-{1}" -f $version, $Runtime)
$single = if ($SingleFile) { 'true' } else { 'false' }

Write-Host ""
Write-Host "Publishing Nexus $version" -ForegroundColor White
Write-Host "  configuration : $Configuration" -ForegroundColor DarkGray
Write-Host "  runtime       : $Runtime (self-contained)" -ForegroundColor DarkGray
Write-Host "  single file   : $single" -ForegroundColor DarkGray
Write-Host "  output        : $publishDir" -ForegroundColor DarkGray
Write-Host ""

if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }

$publishArgs = @(
    'publish', $project,
    '-c', $Configuration,
    '-r', $Runtime,
    '--self-contained', 'true',
    "-p:PublishSingleFile=$single",
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:DebugType=none',
    '-p:DebugSymbols=false',
    '-o', $publishDir
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

if (-not $NoTools) {
    Write-Host ""
    Write-Host "Fetching bundled tools into the published output..." -ForegroundColor Cyan
    $toolsDir = Join-Path $publishDir 'tools'
    & (Join-Path $PSScriptRoot 'fetch-tools.ps1') -OutputDir $toolsDir
}

if (-not $NoZip) {
    $zipPath = Join-Path $OutputRoot ("Nexus-{0}-{1}-portable.zip" -f $version, $Runtime)
    if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
    Write-Host ""
    Write-Host "Creating portable archive $zipPath..." -ForegroundColor Cyan
    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath
    Write-Host ""
    Write-Host "Portable folder : $publishDir" -ForegroundColor Green
    Write-Host "Portable zip    : $zipPath" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Portable folder : $publishDir" -ForegroundColor Green
}
