#Requires -Version 5.1
<#
.SYNOPSIS
    Downloads the external command-line tools Nexus depends on (yt-dlp, FFmpeg and
    ffprobe) from their official release sources and verifies each download against the
    publisher's SHA-256 checksum before installing it.

.DESCRIPTION
    Nexus shells out to two third-party programs at runtime: yt-dlp (media extraction)
    and FFmpeg/ffprobe (muxing, remuxing and conversion). These binaries are NOT stored
    in source control and are NEVER downloaded from unofficial mirrors. This script
    fetches them from trusted, official locations only:

      * yt-dlp.exe            https://github.com/yt-dlp/yt-dlp  (official GitHub releases)
      * ffmpeg.exe / ffprobe  https://www.gyan.dev/ffmpeg/builds ("gyan.dev" is the
                              Windows build provider listed on the official
                              https://ffmpeg.org/download.html page)

    Every artifact is downloaded over TLS and its SHA-256 hash is compared against the
    checksum published alongside it. A file is only moved into the output folder after
    its checksum verifies, so a partial or tampered download can never replace a good
    binary. The script takes no untrusted input and never passes anything to cmd.exe or
    Invoke-Expression.

.PARAMETER OutputDir
    Destination "tools" folder. Defaults to src\Nexus.App\tools, which the app project
    copies next to Nexus.exe at build/publish time so DependencyManager can resolve the
    tools without any system-wide install.

.PARAMETER SkipYtDlp
    Skip downloading yt-dlp.

.PARAMETER SkipFfmpeg
    Skip downloading FFmpeg and ffprobe.

.PARAMETER Force
    Re-download and overwrite even if a verified binary already exists.

.EXAMPLE
    pwsh ./scripts/fetch-tools.ps1

.EXAMPLE
    # Populate a published build's tools folder instead of the source tree.
    pwsh ./scripts/fetch-tools.ps1 -OutputDir .\dist\Nexus-0.1.0-win-x64\tools

.NOTES
    Runs on Windows PowerShell 5.1 or PowerShell 7+. Requires network access to
    github.com and gyan.dev.
#>
[CmdletBinding()]
param(
    [string]$OutputDir,
    [switch]$SkipYtDlp,
    [switch]$SkipFfmpeg,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'  # dramatically speeds up Invoke-WebRequest

# --- Official, trusted download endpoints ------------------------------------------
$YtDlpExeUrl  = 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe'
$YtDlpSumsUrl = 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS'
$FfmpegZipUrl = 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip'
$FfmpegSumUrl = 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip.sha256'

# Force a modern TLS floor; older Windows PowerShell defaults to TLS 1.0/1.1.
try {
    [Net.ServicePointManager]::SecurityProtocol =
        [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
} catch {
    # Enum member unavailable on very old frameworks; the platform default still applies.
}

# --- Resolve output folder ---------------------------------------------------------
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDir) {
    $OutputDir = Join-Path $repoRoot 'src\Nexus.App\tools'
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$OutputDir = (Resolve-Path $OutputDir).Path

Write-Host ""
Write-Host "Nexus tool fetcher" -ForegroundColor White
Write-Host "  output: $OutputDir" -ForegroundColor DarkGray
Write-Host ""

# --- Helpers -----------------------------------------------------------------------
function Get-RemoteText {
    param([Parameter(Mandatory)][string]$Url)
    $content = (Invoke-WebRequest -Uri $Url -UseBasicParsing -MaximumRedirection 5).Content
    # Windows PowerShell 5.1 returns the body as a Byte[] (not a string) when the
    # response content type is non-text -- e.g. GitHub release assets, which are
    # served as application/octet-stream. Decode to UTF-8 text so the checksum
    # parsers always receive a string to split/scan.
    if ($content -is [byte[]]) {
        $content = [System.Text.Encoding]::UTF8.GetString($content)
    }
    return $content
}

function Save-RemoteFile {
    param(
        [Parameter(Mandatory)][string]$Url,
        [Parameter(Mandatory)][string]$Path
    )
    $attempt = 0
    while ($true) {
        $attempt++
        try {
            Write-Host "  downloading $Url" -ForegroundColor DarkGray
            Invoke-WebRequest -Uri $Url -OutFile $Path -UseBasicParsing -MaximumRedirection 5
            return
        } catch {
            if ($attempt -ge 3) { throw }
            Write-Warning "  download failed (attempt $attempt/3): $($_.Exception.Message). Retrying..."
            Start-Sleep -Seconds ([Math]::Min(10, $attempt * 3))
        }
    }
}

function Assert-Sha256 {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Expected
    )
    $expectedHash = $Expected.Trim().ToUpperInvariant()
    if ($expectedHash.Length -ne 64) {
        throw "Publisher checksum for '$([IO.Path]::GetFileName($Path))' is not a 64-character SHA-256 hash."
    }
    $actual = (Get-FileHash -Algorithm SHA256 -Path $Path).Hash
    if ($actual -ne $expectedHash) {
        throw ("Checksum mismatch for {0}`n  expected {1}`n  actual   {2}" -f `
            [IO.Path]::GetFileName($Path), $expectedHash, $actual)
    }
    Write-Host "  checksum OK  $($actual.Substring(0,16))..." -ForegroundColor Green
}

# Extract the first 64-hex token from a checksum file (handles "hash", "hash *file"
# and "hash  file" forms).
function Get-Sha256Token {
    param([Parameter(Mandatory)][string]$Text)
    $m = [regex]::Match($Text, '[0-9A-Fa-f]{64}')
    if (-not $m.Success) { throw "No SHA-256 hash found in checksum document." }
    return $m.Value
}

# --- yt-dlp ------------------------------------------------------------------------
if (-not $SkipYtDlp) {
    $target = Join-Path $OutputDir 'yt-dlp.exe'
    if ((Test-Path $target) -and -not $Force) {
        Write-Host "yt-dlp.exe already present (use -Force to re-download); skipping." -ForegroundColor Yellow
    } else {
        Write-Host "yt-dlp  (official GitHub release)" -ForegroundColor Cyan
        $tmp = Join-Path ([IO.Path]::GetTempPath()) ("yt-dlp-{0}.exe.tmp" -f $PID)
        try {
            Save-RemoteFile -Url $YtDlpExeUrl -Path $tmp

            # SHA2-256SUMS lists "<hash>  <filename>" per line (sha256sum format).
            $sums = Get-RemoteText -Url $YtDlpSumsUrl
            $expected = $null
            foreach ($line in ($sums -split "`n")) {
                $tokens = $line.Trim() -split '\s+'
                if ($tokens.Length -ge 2 -and $tokens[-1] -eq 'yt-dlp.exe') {
                    $expected = $tokens[0]
                    break
                }
            }
            if (-not $expected) { throw "Could not locate 'yt-dlp.exe' in SHA2-256SUMS." }

            Assert-Sha256 -Path $tmp -Expected $expected
            Move-Item -LiteralPath $tmp -Destination $target -Force
            Write-Host "  installed -> $target" -ForegroundColor Green
        } finally {
            if (Test-Path $tmp) { Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue }
        }
    }
    Write-Host ""
}

# --- FFmpeg + ffprobe --------------------------------------------------------------
if (-not $SkipFfmpeg) {
    $haveBoth = (Test-Path (Join-Path $OutputDir 'ffmpeg.exe')) -and
                (Test-Path (Join-Path $OutputDir 'ffprobe.exe'))
    if ($haveBoth -and -not $Force) {
        Write-Host "ffmpeg.exe / ffprobe.exe already present (use -Force to re-download); skipping." -ForegroundColor Yellow
    } else {
        Write-Host "FFmpeg + ffprobe  (gyan.dev build, linked from ffmpeg.org)" -ForegroundColor Cyan
        $zip = Join-Path ([IO.Path]::GetTempPath()) ("ffmpeg-{0}.zip" -f $PID)
        $extractDir = Join-Path ([IO.Path]::GetTempPath()) ("ffmpeg-{0}" -f $PID)
        try {
            Save-RemoteFile -Url $FfmpegZipUrl -Path $zip
            $expected = Get-Sha256Token -Text (Get-RemoteText -Url $FfmpegSumUrl)
            Assert-Sha256 -Path $zip -Expected $expected

            if (Test-Path $extractDir) { Remove-Item -Recurse -Force $extractDir }
            Expand-Archive -LiteralPath $zip -DestinationPath $extractDir -Force

            foreach ($exe in @('ffmpeg.exe', 'ffprobe.exe')) {
                $found = Get-ChildItem -Path $extractDir -Recurse -Filter $exe -File |
                         Select-Object -First 1
                if (-not $found) { throw "'$exe' was not found inside the FFmpeg archive." }
                Copy-Item -LiteralPath $found.FullName -Destination (Join-Path $OutputDir $exe) -Force
                Write-Host "  installed -> $(Join-Path $OutputDir $exe)" -ForegroundColor Green
            }
        } finally {
            if (Test-Path $extractDir) { Remove-Item -Recurse -Force $extractDir -ErrorAction SilentlyContinue }
            if (Test-Path $zip)        { Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue }
        }
    }
    Write-Host ""
}

# --- Summary -----------------------------------------------------------------------
Write-Host "Tools in $OutputDir :" -ForegroundColor White
Get-ChildItem -Path $OutputDir -Filter *.exe -File -ErrorAction SilentlyContinue |
    ForEach-Object {
        $mb = [Math]::Round($_.Length / 1MB, 1)
        Write-Host ("  {0,-14} {1,6} MB" -f $_.Name, $mb) -ForegroundColor Gray
    }
Write-Host ""
Write-Host "Done." -ForegroundColor Green
