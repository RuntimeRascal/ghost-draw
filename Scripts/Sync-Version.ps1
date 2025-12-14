# Sync version from package.json to all project files
param(
    [switch]$Verify,  # Only check, don't modify
    [switch]$WhatIf   # Show what would be changed
)

$ErrorActionPreference = "Stop"

# Read version from package.json
$packageJson = Get-Content "package.json" -Raw | ConvertFrom-Json
$version = $packageJson.version
Write-Host "Source version (package.json): $version" -ForegroundColor Cyan

# Parse version components
if ($version -match '^(\d+)\.(\d+)\.(\d+)$') {
    $major = $matches[1]
    $minor = $matches[2]
    $patch = $matches[3]
    $fourPartVersion = "$major.$minor.$patch.0"
} else {
    Write-Host "ERROR: Invalid version format in package.json: $version" -ForegroundColor Red
    exit 1
}

# Check GhostDraw.csproj
Write-Host "`nChecking GhostDraw.csproj..." -ForegroundColor Yellow
$csprojPath = "Src\GhostDraw\GhostDraw.csproj"
[xml]$csproj = Get-Content $csprojPath
$currentCsprojVersion = $csproj.Project.PropertyGroup.Version

if ($currentCsprojVersion -ne $version) {
    Write-Host "  Current: $currentCsprojVersion" -ForegroundColor Red
    Write-Host "  Expected: $version" -ForegroundColor Green

    if (-not $Verify -and -not $WhatIf) {
        $csproj.Project.PropertyGroup.Version = $version
        $csproj.Save($csprojPath)
        Write-Host "  UPDATED" -ForegroundColor Green
    }
} else {
    Write-Host "  OK: $currentCsprojVersion" -ForegroundColor Green
}

# Check Package.appxmanifest
Write-Host "`nChecking Package.appxmanifest..." -ForegroundColor Yellow
$manifestPath = "Package\Package.appxmanifest"
if (Test-Path $manifestPath) {
    [xml]$manifest = Get-Content $manifestPath
    $currentManifestVersion = $manifest.Package.Identity.Version

    if ($currentManifestVersion -ne $fourPartVersion) {
        Write-Host "  Current: $currentManifestVersion" -ForegroundColor Red
        Write-Host "  Expected: $fourPartVersion" -ForegroundColor Green

        if (-not $Verify -and -not $WhatIf) {
            $manifest.Package.Identity.Version = $fourPartVersion
            $manifest.Save($manifestPath)
            Write-Host "  UPDATED" -ForegroundColor Green
        }
    } else {
        Write-Host "  OK: $currentManifestVersion" -ForegroundColor Green
    }
} else {
    Write-Host "  Not found (will be created later)" -ForegroundColor Gray
}

# Check Installer version
Write-Host "`nChecking Installer\GhostDraw.Installer.wixproj..." -ForegroundColor Yellow
$wixprojPath = "Installer\GhostDraw.Installer.wixproj"
[xml]$wixproj = Get-Content $wixprojPath
$currentWixVersion = $wixproj.Project.PropertyGroup.Version

if ($currentWixVersion -ne $version) {
    Write-Host "  Current: $currentWixVersion" -ForegroundColor Red
    Write-Host "  Expected: $version" -ForegroundColor Green

    if (-not $Verify -and -not $WhatIf) {
        $wixproj.Project.PropertyGroup.Version = $version
        $wixproj.Save($wixprojPath)
        Write-Host "  UPDATED" -ForegroundColor Green
    }
} else {
    Write-Host "  OK: $currentWixVersion" -ForegroundColor Green
}

Write-Host "`n=====================================" -ForegroundColor Cyan
if ($Verify) {
    Write-Host " Version Verification Complete" -ForegroundColor Yellow
} elseif ($WhatIf) {
    Write-Host " Version Check Complete (What-If)" -ForegroundColor Yellow
} else {
    Write-Host " Version Synchronization Complete" -ForegroundColor Green
}
Write-Host "=====================================" -ForegroundColor Cyan
