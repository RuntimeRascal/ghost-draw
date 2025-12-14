# GhostDraw MSIX Build Script
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$CreateUploadPackage = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Building GhostDraw MSIX v$Version" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Update version in manifest
Write-Host "[1/4] Updating package version in manifest..." -ForegroundColor Yellow
$manifestPath = "Package.appxmanifest"
[xml]$manifest = Get-Content $manifestPath
$manifest.Package.Identity.Version = "$Version.0"
$manifest.Save($manifestPath)
Write-Host "Manifest version updated to $Version.0" -ForegroundColor Green
Write-Host ""

# Build the application
Write-Host "[2/4] Building GhostDraw application..." -ForegroundColor Yellow
dotnet build ..\Src\GhostDraw\GhostDraw.csproj -c $Configuration -r win-x64

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build application" -ForegroundColor Red
    exit 1
}
Write-Host "Application built successfully" -ForegroundColor Green
Write-Host ""

# Build MSIX package
if ($CreateUploadPackage) {
    Write-Host "[3/4] Building MSIX package for Microsoft Store..." -ForegroundColor Yellow
    $buildMode = "StoreUpload"
    $bundle = "Always"
} else {
    Write-Host "[3/4] Building MSIX package for local testing..." -ForegroundColor Yellow
    $buildMode = "SideloadOnly"
    $bundle = "Never"
}

$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# Set signing based on build mode
if ($CreateUploadPackage) {
    # Store upload packages must be unsigned (Store will sign them)
    & $msbuild GhostDraw.Package.wapproj `
        /p:Configuration=$Configuration `
        /p:Platform=x64 `
        /p:AppxBundle=$bundle `
        /p:UapAppxPackageBuildMode=$buildMode `
        /p:AppxPackageSigningEnabled=false `
        /p:PackageCertificateKeyFile="" `
        /p:GenerateAppInstallerFile=false
} else {
    # Local sideload packages need test certificate signing
    & $msbuild GhostDraw.Package.wapproj `
        /p:Configuration=$Configuration `
        /p:Platform=x64 `
        /p:AppxBundle=$bundle `
        /p:UapAppxPackageBuildMode=$buildMode `
        /p:GenerateAppInstallerFile=false
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build MSIX package" -ForegroundColor Red
    exit 1
}
Write-Host "MSIX package built successfully" -ForegroundColor Green
Write-Host ""

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Build Complete! " -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan

# Show output location
if ($CreateUploadPackage) {
    $outputPath = "AppPackages\GhostDraw.Package_${Version}.0"
    if (Test-Path $outputPath) {
        Write-Host "Output: $outputPath" -ForegroundColor White
        Get-ChildItem $outputPath -Filter "*.msixupload" | ForEach-Object {
            $size = [math]::Round($_.Length / 1MB, 2)
            Write-Host "  - $($_.Name) ($size MB)" -ForegroundColor Gray
        }
        Write-Host ""
        Write-Host "Upload this .msixupload file to Partner Center for Store submission" -ForegroundColor Cyan
    }
} else {
    $outputPath = "AppPackages\GhostDraw.Package_${Version}.0_Test"
    if (Test-Path $outputPath) {
        Write-Host "Output: $outputPath" -ForegroundColor White
        Get-ChildItem $outputPath -Filter "*.msix" | ForEach-Object {
            $size = [math]::Round($_.Length / 1MB, 2)
            Write-Host "  - $($_.Name) ($size MB)" -ForegroundColor Gray
        }
        Write-Host ""
        Write-Host "Install locally with: Add-AppxPackage -Path '$outputPath\GhostDraw.Package_${Version}.0_x64.msix'" -ForegroundColor Cyan
    }
}
