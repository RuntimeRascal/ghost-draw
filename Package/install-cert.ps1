# Install test certificate to Trusted Root
# This script must be run as Administrator

param(
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

Write-Host "Installing GhostDraw Test Certificate..." -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator', then run this script again" -ForegroundColor Yellow
    exit 1
}

# Import certificate to Trusted Root
$certPath = Join-Path $PSScriptRoot "GhostDraw_TestCert.cer"

if (-not (Test-Path $certPath)) {
    Write-Host "ERROR: Certificate file not found: $certPath" -ForegroundColor Red
    exit 1
}

Write-Host "Importing certificate to Trusted Root Certification Authorities..." -ForegroundColor Yellow

Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\LocalMachine\Root

Write-Host ""
Write-Host "Certificate installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "You can now install the MSIX package by running:" -ForegroundColor Cyan
Write-Host "  Add-AppxPackage -Path AppPackages\GhostDraw.Package_1.0.17.0_x64_Debug_Test\GhostDraw.Package_1.0.17.0_x64_Debug.msix" -ForegroundColor White
