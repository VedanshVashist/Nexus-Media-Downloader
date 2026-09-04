#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the Nexus Windows installer with Inno Setup.

.DESCRIPTION
    Produces a self-contained publish (via scripts/publish.ps1, without a zip) and then
    compiles installer\Nexus.iss with the Inno Setup command-line compiler (ISCC.exe) to
    produce dist\Nexus-<version>-setup.exe.

    Inno Setup is a free installer authoring system: https://jrsoftware.org/isinfo.php

.PARAMETER Configuration
    Build configuration passed to publish. Defaults to Release.

.PARAMETER Runtime
    Runtime identifier passed to publish. Defaults to win-x64.

.PARAMETER IsccPath
    Full path to ISCC.exe. If omitted, common install locations are probed and, failing
    that, PATH is searched.

.PARAMETER SkipPublish
    Reuse an existing publish under dist\ instead of rebuilding.

.EXAMPLE
    pwsh ./scripts/build-installer.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$IsccPath,
    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

[xml]$props = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props')
$version = @($props.Project.PropertyGroup.Version | Where-Object { $_ })[0]
if (-not $version) { $version = '0.0.0' }

$publishDir = Join-Path $repoRoot ("dist\Nexus-{0}-{1}" -f $version, $Runtime)

if (-not $SkipPublish) {
    & (Join-Path $PSScriptRoot 'publish.ps1') -Configuration $Configuration -Runtime $Runtime -NoZip
    if ($LASTEXITCODE -ne 0) { throw "publish.ps1 failed with exit code $LASTEXITCODE." }
}

if (-not (Test-Path $publishDir)) {
    throw "Publish output not found at '$publishDir'. Run without -SkipPublish first."
}

# Locate the Inno Setup compiler.
if (-not $IsccPath) {
    $bases = @(${env:ProgramFiles(x86)}, $env:ProgramFiles) | Where-Object { $_ }
    $candidates = $bases | ForEach-Object { Join-Path $_ 'Inno Setup 6\ISCC.exe' }
    $IsccPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $IsccPath) {
        $cmd = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
        if ($cmd) { $IsccPath = $cmd.Source }
    }
}
if (-not $IsccPath -or -not (Test-Path $IsccPath)) {
    throw "Inno Setup compiler (ISCC.exe) was not found. Install it from " +
          "https://jrsoftware.org/isdl.php or pass -IsccPath <path to ISCC.exe>."
}

$iss = Join-Path $repoRoot 'installer\Nexus.iss'

Write-Host ""
Write-Host "Compiling installer" -ForegroundColor White
Write-Host "  ISCC        : $IsccPath" -ForegroundColor DarkGray
Write-Host "  script      : $iss" -ForegroundColor DarkGray
Write-Host "  publish dir : $publishDir" -ForegroundColor DarkGray
Write-Host "  version     : $version" -ForegroundColor DarkGray
Write-Host ""

& $IsccPath "/DAppVersion=$version" "/DPublishDir=$publishDir" $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE." }

Write-Host ""
Write-Host "Installer written to $(Join-Path $repoRoot 'dist')\Nexus-$version-setup.exe" -ForegroundColor Green
