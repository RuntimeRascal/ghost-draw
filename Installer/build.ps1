# GhostDraw Installer Build Script
# This script builds both the application and the installer

param(
    [string]$Configuration = "Release",
    [string]$Version = "2.0.0.0"
)

function Get-NormalizedVersion {
    param([string]$InputVersion)

    if ($InputVersion -match '^\d+\.\d+\.\d+\.\d+$') {
        return $InputVersion
    }

    if ($InputVersion -match '^\d+\.\d+\.\d+$') {
        return "$InputVersion.0"
    }

    throw "Version must be 3- or 4-part (Major.Minor.Build.Revision)"
}

$normalizedVersion = Get-NormalizedVersion -InputVersion $Version

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Building GhostDraw Installer v$normalizedVersion" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build the main application (self-contained)
Write-Host "[1/4] Building GhostDraw application..." -ForegroundColor Yellow
dotnet publish ..\Src\GhostDraw\GhostDraw.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:Version=$normalizedVersion

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build application" -ForegroundColor Red
    exit 1
}

Write-Host "Application built successfully" -ForegroundColor Green
Write-Host ""

# Step 2: Generate file list from publish folder
Write-Host "[2/4] Generating component list from publish folder..." -ForegroundColor Yellow
$PublishPath = "..\Src\GhostDraw\bin\Release\net8.0-windows\win-x64\publish"
$HeatOutput = "HarvestedFiles.wxs"

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File "Generate-FileList.ps1" -PublishPath $PublishPath -OutputFile $HeatOutput

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to generate file list" -ForegroundColor Red
    exit 1
}

$fileCount = (Get-ChildItem -Path $PublishPath -Recurse -File).Count
Write-Host "Generated components for $fileCount files" -ForegroundColor Green
Write-Host ""

# Step 3: Build the installer
Write-Host "[3/4] Building installer MSI..." -ForegroundColor Yellow
dotnet build GhostDraw.Installer.wixproj `
    -c $Configuration `
    -p:Version=$normalizedVersion `
    -p:PublishDir="..\Src\GhostDraw\bin\Release\net8.0-windows\win-x64\publish\"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build installer" -ForegroundColor Red
    exit 1
}

Write-Host "Installer built successfully" -ForegroundColor Green
Write-Host ""

# Step 4: Show output location
$OutputPath = "bin\x64\$Configuration\GhostDrawSetup-$normalizedVersion.msi"
Write-Host "[4/4] Installer created:" -ForegroundColor Yellow
Write-Host "  $OutputPath" -ForegroundColor White
Write-Host ""

# Get file size
if (Test-Path $OutputPath) {
    $FileSize = (Get-Item $OutputPath).Length / 1MB
    Write-Host "File size: $([math]::Round($FileSize, 2)) MB" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Build Complete! " -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
