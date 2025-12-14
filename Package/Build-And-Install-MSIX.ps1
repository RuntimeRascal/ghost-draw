param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'

# Resolve key paths so script works when invoked from repo root (npm) or directly inside Package
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
$PackageJsonPath = Join-Path $RepoRoot "package.json"

Push-Location $ScriptDir

function Get-Version {
    param([string]$PassedVersion)
    if ($PassedVersion -and $PassedVersion.Trim().Length -gt 0) {
        return $PassedVersion.Trim()
    }
    if (-not (Test-Path $PackageJsonPath)) {
        throw "package.json not found at $PackageJsonPath. Pass -Version explicitly."
    }
    $package = Get-Content $PackageJsonPath -Raw | ConvertFrom-Json
    if (-not $package.version) {
        throw "package.json missing 'version'. Pass -Version explicitly."
    }
    return $package.version.Trim()
}

try {
    $resolvedVersion = Get-Version -PassedVersion $Version
    Write-Host "Using version: $resolvedVersion" -ForegroundColor Cyan

    # Cert prep
    $pfx = Join-Path $ScriptDir "GhostDraw_TestCert.pfx"
    $cer = Join-Path $ScriptDir "GhostDraw_TestCert.cer"
    if ((-not (Test-Path $pfx)) -or (-not (Test-Path $cer))) {
        Write-Host "Test cert not found; creating and installing..." -ForegroundColor Yellow
        pwsh -NoProfile -File (Join-Path $ScriptDir "create-test-cert.ps1")
        pwsh -NoProfile -File (Join-Path $ScriptDir "install-cert.ps1")
    }

    # Clean previous AppPackages to avoid stale artifacts
    $appPackagesDir = Join-Path $ScriptDir "AppPackages"
    if (Test-Path $appPackagesDir) {
        Write-Host "Removing existing AppPackages directory..." -ForegroundColor Yellow
        Remove-Item -Recurse -Force $appPackagesDir
    }

    # Build
    Write-Host "Building MSIX (Debug) ..." -ForegroundColor Cyan
    pwsh -NoProfile -File (Join-Path $ScriptDir "build-msix.ps1") -Version $resolvedVersion -Configuration Debug

    # Uninstall existing
    $existing = Get-AppxPackage -Name "*GhostDraw*" -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "Removing existing GhostDraw package..." -ForegroundColor Yellow
        $existing | Remove-AppxPackage
    }

    # Install new
    $packagePath = Join-Path $ScriptDir ("AppPackages\GhostDraw.Package_{0}_x64_Debug_Test\GhostDraw.Package_{0}_x64_Debug.msix" -f $resolvedVersion)
    if (-not (Test-Path $packagePath)) {
        throw "Built package not found at $packagePath"
    }

    Write-Host "Installing $packagePath" -ForegroundColor Cyan
    Add-AppxPackage -Path $packagePath

    Write-Host "MSIX install complete." -ForegroundColor Green
}
catch {
        Write-Error $_
        throw
    }
    finally {
        Pop-Location
    }
