<#
.SYNOPSIS
    Builds the Android APK and the Windows desktop release, then zips both into a single
    fit-build-YYYY-MM-DD.zip for the GitHub Release upload (upute.md §9.2).

.DESCRIPTION
    - flutter clean + flutter build apk --release (UI/driving_school_mobile), with
      API_BASE_URL from API_BASE_URL_MOBILE in .env (http://10.0.2.2:5000, the standard Android
      emulator host address, upute.md §9.2.1).
    - flutter clean + flutter build windows --release (UI/driving_school_desktop), with
      API_BASE_URL from API_BASE_URL_DESKTOP in .env (http://localhost:5000, upute.md §9.2.2).
    - Zips both outputs into fit-build-<date>.zip, written OUTSIDE the git repository (default:
      a sibling "releases" folder next to the repo root) so the archive is never accidentally
      committed. The archive's internal layout mirrors upute.md §9.2's expected paths exactly:
        UI/driving_school_mobile/build/app/outputs/flutter-apk/app-release.apk
        UI/driving_school_desktop/build/windows/x64/runner/Release/...

.PARAMETER OutputDirectory
    Where to write the zip. Defaults to "..\eautoskola-releases" relative to the repo root (a
    sibling of the repo, i.e. outside git). Pass an absolute path to use another location.

.PARAMETER SkipBuild
    Skip the flutter clean/build steps and only zip whatever is already in each app's build
    output. Useful for re-packaging without rebuilding.

.EXAMPLE
    ./scripts/build-release.ps1

.EXAMPLE
    ./scripts/build-release.ps1 -OutputDirectory D:\releases
#>

[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$mobileDir = Join-Path $repoRoot 'UI\driving_school_mobile'
$desktopDir = Join-Path $repoRoot 'UI\driving_school_desktop'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path (Split-Path -Parent $repoRoot) 'eautoskola-releases'
}

# The API addresses live in .env (upute.md §3.3), never in this script.
function Get-EnvValue {
    param([string]$Key)
    $envFile = Join-Path $repoRoot '.env'
    if (-not (Test-Path $envFile)) {
        throw ".env not found at $envFile - unzip .env-tajne.zip into the repository root first."
    }
    $line = Get-Content $envFile | Where-Object { $_ -match "^\s*$Key=(.+)$" } | Select-Object -First 1
    if (-not $line) {
        throw "$Key is missing in .env"
    }
    return ($line -replace "^\s*$Key=", '').Trim()
}
$mobileApiUrl = Get-EnvValue 'API_BASE_URL_MOBILE'
$desktopApiUrl = Get-EnvValue 'API_BASE_URL_DESKTOP'

function Invoke-Checked {
    param([string]$Command, [string]$WorkingDirectory)
    Write-Host ">> ($WorkingDirectory) $Command" -ForegroundColor Cyan
    Push-Location $WorkingDirectory
    try {
        Invoke-Expression $Command
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code $LASTEXITCODE`: $Command"
        }
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipBuild) {
    Write-Host "`n=== Mobile (Android APK) ===" -ForegroundColor Yellow
    Invoke-Checked -WorkingDirectory $mobileDir -Command 'flutter clean'
    Invoke-Checked -WorkingDirectory $mobileDir -Command "flutter build apk --release --dart-define=API_BASE_URL=$mobileApiUrl"

    Write-Host "`n=== Desktop (Windows) ===" -ForegroundColor Yellow
    Invoke-Checked -WorkingDirectory $desktopDir -Command 'flutter clean'
    Invoke-Checked -WorkingDirectory $desktopDir -Command "flutter build windows --release --dart-define=API_BASE_URL=$desktopApiUrl"
}
else {
    Write-Host 'Skipping build steps (-SkipBuild); packaging existing build output.' -ForegroundColor Yellow
}

$apkPath = Join-Path $mobileDir 'build\app\outputs\flutter-apk\app-release.apk'
$windowsReleaseDir = Join-Path $desktopDir 'build\windows\x64\runner\Release'

if (-not (Test-Path $apkPath)) {
    throw "APK not found at $apkPath - did the mobile build succeed?"
}
if (-not (Test-Path $windowsReleaseDir)) {
    throw "Windows Release folder not found at $windowsReleaseDir - did the desktop build succeed?"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

# Stage into a temp folder so the zip's internal paths match upute.md §9.2 exactly, instead of
# whatever Compress-Archive would do with the raw absolute paths.
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "eautoskola-release-staging-$(Get-Random)"
$mobileStagingTarget = Join-Path $stagingRoot 'UI\driving_school_mobile\build\app\outputs\flutter-apk'
$desktopStagingTarget = Join-Path $stagingRoot 'UI\driving_school_desktop\build\windows\x64\runner\Release'

try {
    New-Item -ItemType Directory -Force -Path $mobileStagingTarget | Out-Null
    Copy-Item -Path $apkPath -Destination $mobileStagingTarget

    New-Item -ItemType Directory -Force -Path (Split-Path $desktopStagingTarget) | Out-Null
    Copy-Item -Path $windowsReleaseDir -Destination $desktopStagingTarget -Recurse

    $dateStamp = Get-Date -Format 'yyyy-MM-dd'
    $zipPath = Join-Path $OutputDirectory "fit-build-$dateStamp.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force -Confirm:$false
    }

    # ZipFile (not Compress-Archive): Windows PowerShell 5.1 writes backslashes into the entry names,
    # which other unzip tools then turn into odd file names.
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stagingRoot, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

    Write-Host "`nCreated $zipPath" -ForegroundColor Green
    Write-Host '(outside the git repository - attach this file to the GitHub Release, never commit it)' -ForegroundColor Green
}
finally {
    if (Test-Path $stagingRoot) {
        Remove-Item $stagingRoot -Recurse -Force -Confirm:$false
    }
}
