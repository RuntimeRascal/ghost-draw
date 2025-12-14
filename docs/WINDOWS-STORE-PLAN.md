# GhostDraw Microsoft Store Publishing Plan

## Overview

This plan outlines the complete process to create an MSIX package for GhostDraw and publish it to the Microsoft Store, eliminating the need for an expensive code-signing certificate.

**Key Benefits:**
- FREE code signing from Microsoft Store (individual developer registration is now FREE as of Sept 2025)
- No "Windows protected your PC" security warnings
- Automatic updates for users
- Increased trust and discoverability

**Current State:**
- WPF .NET 8 application with WiX v4 MSI installer
- Uses global keyboard hooks (WH_KEYBOARD_LL) for hotkey detection
- Version 1.0.17, self-contained win-x64 deployment
- GitHub Actions CI/CD for MSI builds

## Executive Summary

**Total Effort:** 16-24 hours hands-on + 1-3 days Microsoft Store certification review

**Critical Compatibility Finding:** GhostDraw's global keyboard hooks (WH_KEYBOARD_LL) ARE compatible with MSIX packaging because they use message-based callbacks rather than DLL injection. The `runFullTrust` restricted capability enables full Win32 API access.

**Dual Distribution Strategy:** Maintain both MSI (for enterprise/advanced users) and MSIX (for Store users) with identical functionality and synchronized versions.

---

## Phase 1: Prerequisites & Setup (1 hour)

### 1.1 Development Tools ✅

**Install Windows Application Packaging Support:**
- Visual Studio 2022 with "Universal Windows Platform development" workload
- OR Windows SDK 10.0.19041.0+ (includes MakeAppx.exe, SignTool.exe)

**Verify Installation:**
```powershell
Get-Command MakeAppx.exe
Get-Command SignTool.exe
dotnet --version  # 9.0.308
```

### 1.2 Microsoft Partner Center Account

**Registration (NOW FREE for individuals!):**
1. Visit: https://partner.microsoft.com/dashboard/registration/store
2. Sign in with Microsoft Account
3. Select "Individual" account type (FREE)
4. Complete registration form (contact info, tax info, payout account)
5. Wait for approval (24-48 hours)

**Your Account Details:**
- Publisher display name: **RuntimeRascal2** ✅
- App name reserved: **Ghost Draw** ✅
- Support email: **runtimerascal@outlook.com** ✅
- Privacy policy URL: https://github.com/RuntimeRascal/ghost-draw/blob/main/docs/PRIVACY-POLICY.md ✅
- Website URL: https://github.com/RuntimeRascal/ghost-draw ✅

---

## Phase 2: MSIX Packaging Project (2 hours) ✅

### 2.1 Create Packaging Project Structure ✅

**Directory to create:** `c:\code\github\ghost-draw\Package\` ✅

**Files to create:**

#### `Package\GhostDraw.Package.wapproj`
Windows Application Packaging Project file that references the main GhostDraw.csproj and defines MSIX build configurations.

**Key properties:**
- TargetPlatformVersion: 10.0.22621.0
- TargetPlatformMinVersion: 10.0.17763.0 (Windows 10 version 1809)
- EntryPointProjectUniqueName: Points to `Src\GhostDraw\GhostDraw.csproj`
- Platform: x64 only
- AppxPackageSigningEnabled: false (for local testing), true (for Store)

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup Condition="'$(VisualStudioVersion)' == '' or '$(VisualStudioVersion)' < '15.0'">
    <VisualStudioVersion>15.0</VisualStudioVersion>
  </PropertyGroup>

  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>

  <PropertyGroup>
    <WapProjPath Condition="'$(WapProjPath)'==''">$(MSBuildExtensionsPath)\Microsoft\DesktopBridge\</WapProjPath>
  </PropertyGroup>

  <Import Project="$(WapProjPath)\Microsoft.DesktopBridge.props" />

  <PropertyGroup>
    <ProjectGuid>F7D8E1A2-3B4C-5D6E-7F8A-9B0C1D2E3F4A</ProjectGuid>
    <TargetPlatformVersion>10.0.22621.0</TargetPlatformVersion>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <DefaultLanguage>en-US</DefaultLanguage>
    <AppxPackageSigningEnabled>false</AppxPackageSigningEnabled>
    <EntryPointProjectUniqueName>..\Src\GhostDraw\GhostDraw.csproj</EntryPointProjectUniqueName>
    <GenerateAppInstallerFile>False</GenerateAppInstallerFile>
    <PackageCertificateThumbprint></PackageCertificateThumbprint>
    <AppxAutoIncrementPackageRevision>False</AppxAutoIncrementPackageRevision>
    <GenerateTestCertificate>True</GenerateTestCertificate>
    <AppxBundlePlatforms>x64</AppxBundlePlatforms>
    <HoursBetweenUpdateChecks>0</HoursBetweenUpdateChecks>
  </PropertyGroup>

  <ItemGroup>
    <AppxManifest Include="Package.appxmanifest">
      <SubType>Designer</SubType>
    </AppxManifest>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Src\GhostDraw\GhostDraw.csproj">
      <SkipGetTargetFrameworkProperties>True</SkipGetTargetFrameworkProperties>
    </ProjectReference>
  </ItemGroup>

  <ItemGroup>
    <Content Include="Images\**\*.*" />
  </ItemGroup>

  <Import Project="$(WapProjPath)\Microsoft.DesktopBridge.targets" />
</Project>
```

#### `Package\Package.appxmanifest`
Core MSIX manifest declaring app identity, capabilities, and visual elements.

**Critical configurations:**
- **Identity Name:** `RuntimeRascal.GhostDraw` (reserve in Partner Center first)
- **Publisher:** Placeholder initially, replaced by Partner Center publisher ID
- **Version:** `1.0.17.0` (must be 4-part, synced from package.json)
- **Capabilities:** `<rescap:Capability Name="runFullTrust" />` (CRITICAL for keyboard hooks)
- **TargetDeviceFamily:** Windows.Desktop, MinVersion 10.0.17763.0

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity
    Name="RuntimeRascal.GhostDraw"
    Publisher="CN=PLACEHOLDER-WILL-BE-REPLACED-BY-STORE"
    Version="1.0.17.0" />

  <Properties>
    <DisplayName>GhostDraw</DisplayName>
    <PublisherDisplayName>RuntimeRascal</PublisherDisplayName>
    <Logo>Images\StoreLogo.png</Logo>
    <Description>Draw on your screen anywhere, anytime. Perfect for presentations, tutorials, and collaboration.</Description>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="x-generate"/>
  </Resources>

  <Applications>
    <Application Id="GhostDraw"
      Executable="$targetnametoken$.exe"
      EntryPoint="$targetentrypoint$">
      <uap:VisualElements
        DisplayName="GhostDraw"
        Description="Draw directly on your screen with customizable tools and colors"
        BackgroundColor="transparent"
        Square150x150Logo="Images\Square150x150Logo.png"
        Square44x44Logo="Images\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Images\Wide310x150Logo.png" Square310x310Logo="Images\LargeTile.png" Square71x71Logo="Images\SmallTile.png">
          <uap:ShowNameOnTiles>
            <uap:ShowOn Tile="square150x150Logo"/>
            <uap:ShowOn Tile="wide310x150Logo"/>
            <uap:ShowOn Tile="square310x310Logo"/>
          </uap:ShowNameOnTiles>
        </uap:DefaultTile>
        <uap:SplashScreen Image="Images\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <!-- Standard capabilities for desktop apps -->
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
```

### 2.2 Update Solution File ✅

**Modify:** `GhostDraw.sln` ✅

Add new packaging project reference with Debug|x64 and Release|x64 configurations. ✅

```
Project("{C7167F0D-BC9F-4E6E-AFE1-012C56B48DB5}") = "GhostDraw.Package", "Package\GhostDraw.Package.wapproj", "{F7D8E1A2-3B4C-5D6E-7F8A-9B0C1D2E3F4A}"
EndProject
```

---

## Phase 3: Visual Assets Creation (2-3 hours)

### 3.1 Required Asset Sizes

**Create directory:** `Package\Images\` ✅

**Required images (all PNG format):**
1. **Square44x44Logo.png** (44×44px) - App list icon
2. **Square150x150Logo.png** (150×150px) - Medium tile
3. **Square71x71Logo.png** (71×71px) - Small tile
4. **Wide310x150Logo.png** (310×150px) - Wide tile
5. **LargeTile.png** (310×310px) - Large tile
6. **StoreLogo.png** (50×50px) - Store listing
7. **SplashScreen.png** (620×300px) - Launch splash

**Recommended additional sizes:**
- Square44x44Logo.targetsize-16.png through targetsize-256.png (unplated icons for taskbar/Start)

### 3.2 Asset Creation Strategy

**Source material:**
- Base icon: `Src\GhostDraw\Assets\favicon.ico`
- PNG version: `Src\GhostDraw\Assets\ghost-draw-icon.png`

**Design guidelines:**
- Use transparent backgrounds
- Ensure visibility on light and dark backgrounds
- Follow Microsoft Fluent Design principles
- Consider cyberpunk theme consistency

**Tools:**
- Figma (free)
- Inkscape (free, open-source)
- Visual Studio Image Editor
- Online: https://www.appicon.co/

---

## Phase 4: Build Configuration (2 hours) ✅

### 4.1 Version Synchronization Script ✅

**Create:** `Scripts\Sync-Version.ps1` ✅

PowerShell script to sync version from `package.json` (source of truth) to:
- `Src\GhostDraw\GhostDraw.csproj` (3-part: 1.0.17)
- `Package\Package.appxmanifest` (4-part: 1.0.17.0)
- `Installer\GhostDraw.Installer.wixproj` (3-part: 1.0.17)

```powershell
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
```

**Usage:**
```powershell
.\Scripts\Sync-Version.ps1          # Sync all versions
.\Scripts\Sync-Version.ps1 -Verify  # Check only
```

### 4.2 MSIX Build Script ✅

**Create:** `Package\build-msix.ps1` ✅

Build script with parameters:
- `$Configuration` (Debug/Release)
- `$Version` (e.g., "1.0.17")
- `-CreateUploadPackage` (builds .msixupload for Store submission)

```powershell
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
Write-Host "[3/4] Building MSIX package..." -ForegroundColor Yellow
msbuild GhostDraw.Package.wapproj `
    /p:Configuration=$Configuration `
    /p:Platform=x64 `
    /p:AppxBundle=Never `
    /p:UapAppxPackageBuildMode=SideloadOnly `
    /p:GenerateAppInstallerFile=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build MSIX package" -ForegroundColor Red
    exit 1
}
Write-Host "MSIX package built successfully" -ForegroundColor Green
Write-Host ""

# Create Store upload package if requested
if ($CreateUploadPackage) {
    Write-Host "[4/4] Creating Store upload package..." -ForegroundColor Yellow
    msbuild GhostDraw.Package.wapproj `
        /p:Configuration=$Configuration `
        /p:Platform=x64 `
        /p:AppxBundle=Always `
        /p:UapAppxPackageBuildMode=StoreUpload `
        /p:GenerateAppInstallerFile=false

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to create Store upload package" -ForegroundColor Red
        exit 1
    }
    Write-Host "Store upload package created" -ForegroundColor Green
} else {
    Write-Host "[4/4] Skipping Store upload package (use -CreateUploadPackage to build)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Build Complete! " -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan

# Show output location
$outputPath = "AppPackages\GhostDraw.Package_${Version}.0_Test"
if (Test-Path $outputPath) {
    Write-Host "Output: $outputPath" -ForegroundColor White
    Get-ChildItem $outputPath -Filter "*.msix" | ForEach-Object {
        $size = [math]::Round($_.Length / 1MB, 2)
        Write-Host "  - $($_.Name) ($size MB)" -ForegroundColor Gray
    }
}
```

### 4.3 Test Certificate Creation ✅

**Create:** `Package\create-test-cert.ps1` ✅

Generates self-signed certificate for local MSIX testing:

```powershell
$certPassword = ConvertTo-SecureString -String "GhostDrawTest123!" -Force -AsPlainText

$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject "CN=RuntimeRascal Test Certificate" `
    -KeyUsage DigitalSignature `
    -FriendlyName "GhostDraw Test Certificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

$thumbprint = $cert.Thumbprint

# Export certificate
Export-PfxCertificate `
    -Cert "Cert:\CurrentUser\My\$thumbprint" `
    -FilePath "GhostDraw_TestCert.pfx" `
    -Password $certPassword

Export-Certificate `
    -Cert "Cert:\CurrentUser\My\$thumbprint" `
    -FilePath "GhostDraw_TestCert.cer"

Write-Host "Test certificate created:" -ForegroundColor Green
Write-Host "  PFX: GhostDraw_TestCert.pfx (password: GhostDrawTest123!)" -ForegroundColor Gray
Write-Host "  CER: GhostDraw_TestCert.cer" -ForegroundColor Gray
Write-Host ""
Write-Host "Thumbprint: $thumbprint" -ForegroundColor Yellow
Write-Host ""
Write-Host "To install certificate for testing:" -ForegroundColor Cyan
Write-Host "  1. Double-click GhostDraw_TestCert.cer" -ForegroundColor White
Write-Host "  2. Click 'Install Certificate...'" -ForegroundColor White
Write-Host "  3. Select 'Local Machine' -> Next" -ForegroundColor White
Write-Host "  4. Select 'Place all certificates in the following store'" -ForegroundColor White
Write-Host "  5. Browse -> Select 'Trusted Root Certification Authorities'" -ForegroundColor White
Write-Host "  6. Finish" -ForegroundColor White
```

**IMPORTANT:**
- Add `*.pfx` and `*.cer` to `.gitignore`
- Only for local testing, NOT for Store submission
- Microsoft Store re-signs packages with their certificate

---

## Phase 5: Testing & Validation (3-4 hours) ✅

### 5.1 Local Build & Install Testing ✅

**Build steps:**
```powershell
cd Package
.\create-test-cert.ps1                    # One-time
.\build-msix.ps1 -Version "1.0.17"        # Build MSIX
```

**Install certificate (as Administrator, one-time):**
```powershell
Import-Certificate -FilePath "GhostDraw_TestCert.cer" -CertStoreLocation "Cert:\LocalMachine\Root"
```

**Install MSIX:**
```powershell
Add-AppxPackage -Path "AppPackages\GhostDraw.Package_1.0.17.0_x64_Test\*.msix"
```

**Uninstall for testing:**
```powershell
Get-AppxPackage -Name "*GhostDraw*" | Remove-AppxPackage
```

### 5.2 Critical Functionality Tests ✅

**MUST verify:**
- ✅ Global keyboard hook (Ctrl+Alt+X) works across all applications
- ✅ Drawing tools function identically to MSI version
- ✅ Settings persist at: `%LocalAppData%\Packages\RuntimeRascal.GhostDraw_*\LocalCache\Local\GhostDraw\settings.json`
- ✅ Logging works in virtualized path
- ✅ Single-instance mutex works
- ✅ System tray icon and menu function correctly
- ✅ All tool shortcuts (L, E, U, C, T, A) work
- ✅ Screenshot (Ctrl+S), Undo (Ctrl+Z), Clear (Delete) work

### 5.3 Windows App Certification Kit (WACK) (Optional for now)

**Required for Store submission:**
```powershell
# Launch WACK GUI from Start Menu: "Windows App Cert Kit"
# Select MSIX package path
# Run all tests
# Save report
```

**Expected result:** PASS with `runFullTrust` capability declared

**Common issues:**
- Performance test: App must launch within 5 seconds
- Supported API test: Should pass with `runFullTrust`
- Security test: No malware detection

### 5.4 Settings Migration Consideration

**Known issue:** Users upgrading from MSI to MSIX will lose settings due to different virtualized paths:
- MSI: `%LocalAppData%\GhostDraw\`
- MSIX: `%LocalAppData%\Packages\RuntimeRascal.GhostDraw_*\LocalCache\Local\GhostDraw\`

**Optional enhancement:** Create `Services\MsixMigrationService.cs` to detect and copy old settings on first MSIX launch.

---

## Phase 6: CI/CD Integration (2-3 hours) ✅

### 6.1 Update GitHub Actions Workflow ✅

**Modify:** `.github\workflows\ci.yml` ✅

**MSIX build is now integrated with an easy toggle!** Set `BUILD_MSIX: 'true'` in the workflow env to enable.

**Implementation:**

```yaml
- name: Build MSIX package
  shell: pwsh
  working-directory: Package
  run: |
    # Sync version first
    ..\Scripts\Sync-Version.ps1

    # Build MSIX for sideloading (testing)
    msbuild GhostDraw.Package.wapproj `
      /p:Configuration=Release `
      /p:Platform=x64 `
      /p:AppxBundle=Never `
      /p:UapAppxPackageBuildMode=SideloadOnly `
      /p:AppxPackageSigningEnabled=false

- name: Verify MSIX build output
  shell: pwsh
  run: |
    $msixPath = "Package\AppPackages\GhostDraw.Package_${{ env.VERSION }}.0_Test\GhostDraw.Package_${{ env.VERSION }}.0_x64.msix"

    if (Test-Path $msixPath) {
      $size = (Get-Item $msixPath).Length / 1MB
      Write-Host "✓ MSIX package built successfully" -ForegroundColor Green
      Write-Host "  Size: $([math]::Round($size, 2)) MB" -ForegroundColor Gray
    } else {
      Write-Error "MSIX package not found at $msixPath"
      exit 1
    }

- name: Upload MSIX artifact
  uses: actions/upload-artifact@v4
  with:
    name: GhostDraw-${{ env.VERSION }}-MSIX
    path: Package/AppPackages/GhostDraw.Package_${{ env.VERSION }}.0_Test/*.msix
    retention-days: 30
```

**Note:** CI builds unsigned MSIX for testing. Store submission uses different build mode (see Phase 7.3).

**Toggle MSIX builds:**
```yaml
env:
    VERSION: ''
    BUILD_MSIX: 'false' # Change to 'true' to enable MSIX builds
```

When enabled:
- ✅ Builds unsigned MSIX package (Microsoft Store will sign it)
- ✅ Uploads MSIX as build artifact
- ✅ Includes MSIX in draft releases

**Documentation:** See [Package/README.md](../Package/README.md) for detailed usage instructions.

### 6.2 Version Increment Workflow ✅

**For new releases:**
1. Update `package.json` version (e.g., 1.0.17 → 1.0.18)
2. Run `Scripts\Sync-Version.ps1`
3. Commit changes
4. Push to trigger CI/CD
5. (Optional) Enable MSIX build by setting `BUILD_MSIX: 'true'`
6. Build Store package manually for Partner Center submission when ready

---

## Phase 7: Microsoft Store Submission (4-6 hours + 1-3 days review)

### 7.1 Privacy Policy (MANDATORY)

**Create:** `docs\PRIVACY-POLICY.md`

Must include:
- What data is collected (none for GhostDraw)
- Local data storage locations (settings, logs)
- No internet connectivity or telemetry
- Contact information

**Sample Privacy Policy:**

```markdown
# Privacy Policy for GhostDraw

**Last Updated**: [Current Date]

## Overview
GhostDraw ("we", "our", or "us") is committed to protecting your privacy. This Privacy Policy explains how our desktop application handles your information.

## Information We Collect
GhostDraw is a desktop application that runs entirely on your local computer. We do NOT collect, transmit, or store any personal information.

### Local Data Storage
GhostDraw stores the following data locally on your device:
- **Application Settings**: Drawing preferences (colors, brush size, hotkeys)
  - Location: `%LocalAppData%\GhostDraw\settings.json`
- **Application Logs**: Diagnostic logs for troubleshooting
  - Location: `%LocalAppData%\GhostDraw\logs\`

### Data We DO NOT Collect
- Personal identification information
- Usage analytics or telemetry
- Drawing content or screenshots
- Network activity or browsing history

## Data Transmission
GhostDraw does NOT:
- Connect to the internet
- Send data to external servers
- Track user behavior
- Include third-party analytics

## Third-Party Services
GhostDraw does not integrate with or share data with any third-party services.

## Data Security
All data is stored locally on your device and is protected by your Windows user account permissions. No data is transmitted over the internet.

## Children's Privacy
GhostDraw does not knowingly collect any information from users of any age.

## Changes to This Policy
We may update this Privacy Policy from time to time. Updates will be posted on our GitHub repository and the Microsoft Store listing.

## Contact Us
If you have questions about this Privacy Policy, please contact us:
- GitHub Issues: https://github.com/RuntimeRascal/ghost-draw/issues
- Email: [your-email@example.com]

## Your Rights
Since GhostDraw does not collect any personal data, there is no data to access, modify, or delete. You can delete all local data by uninstalling the application.
```

**Publish:**
- Add to GitHub repository
- Enable GitHub Pages OR host on own domain
- URL format: `https://runtimerascal.github.io/ghost-draw/PRIVACY-POLICY.html`

### 7.2 Reserve App Name in Partner Center

**Steps:**
1. Login to https://partner.microsoft.com/dashboard
2. Navigate: Apps and games → New product → MSIX or PWA app
3. Reserve name: "GhostDraw" (check availability)
4. Save (you have 3 months to submit or name expires)

**Capture identity information:**
- Package Identity Name: e.g., `12345RuntimeRascal.GhostDraw`
- Publisher ID: e.g., `CN=A1B2C3D4-E5F6-7890-ABCD-EF1234567890`

**Update `Package\Package.appxmanifest` with reserved identity.**

### 7.3 Build Store Submission Package

```powershell
cd Package
msbuild GhostDraw.Package.wapproj `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:AppxBundle=Always `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:AppxPackageSigningEnabled=false
```

**Output:** `AppPackages\GhostDraw.Package_1.0.17.0_x64\GhostDraw.Package_1.0.17.0_x64.msixupload`

Microsoft Store re-signs with their certificate, so we don't sign it ourselves.

### 7.4 Complete Store Listing

**Navigate to:** Partner Center → Your App → Start your submission

#### Pricing and Availability
- Visibility: Public
- Markets: Select All
- Pricing: Free

#### Properties
- Category: Productivity > Creative > Drawing & Illustration
- Privacy policy URL: (from 7.1)
- Website: https://github.com/RuntimeRascal/ghost-draw
- Support contact email

#### Age Ratings
- Fill questionnaire (suitable for all ages)
- Expected: EVERYONE

#### Packages
- Upload: `GhostDraw.Package_1.0.17.0_x64.msixupload`
- Device family: Desktop
- Min OS: 10.0.17763.0 (Windows 10 version 1809)

#### Store Listings (English - United States)

**Description** (highlight key features):

```
Draw on Your Screen, Anywhere, Anytime

GhostDraw is a lightweight, cyberpunk-themed Windows desktop application that lets you draw directly on your screen with a simple keyboard hotkey. Perfect for presentations, tutorials, collaboration, or just having fun!

🎨 FEATURES

Drawing Tools:
• Multiple tools - Pen, Line, Rectangle, Circle, Eraser, Arrow, and Text
• Perfect shapes - Hold Shift for perfect circles
• Customizable color palette - Create your own collection
• Variable brush thickness - 1-100px with configurable ranges
• Smooth, high-performance rendering
• Mouse wheel thickness control

Hotkey System:
• Global hotkey activation from any application
• Customizable key combinations
• Toggle mode (press once to start/stop)
• Hold mode (draw while holding hotkey)

User Experience:
• Transparent overlay - draw over any app
• System tray integration
• Emergency exit (ESC key)
• Right-click color cycling
• Help overlay (F1 key)

⌨️ DEFAULT SHORTCUTS
• Ctrl+Alt+X - Activate drawing mode
• P - Pen tool
• L - Line tool
• E - Eraser tool
• U - Rectangle tool
• C - Circle tool
• A - Arrow tool
• T - Text tool
• Right-click - Cycle colors
• Mouse wheel - Adjust brush size
• ESC - Exit drawing mode
• F1 - Help overlay
• Ctrl+S - Screenshot all screens
• Ctrl+Z - Undo last stroke
• Delete - Clear all drawings

⚙️ SYSTEM REQUIREMENTS
• Windows 10 version 1809 (Build 17763) or later
• .NET 8 Runtime (included)
• 50 MB disk space

🔒 PRIVACY
GhostDraw runs entirely on your local computer and does NOT:
• Collect personal information
• Send data to external servers
• Track usage or behavior
• Require internet connection

All settings and logs are stored locally in %LocalAppData%\GhostDraw\

📖 OPEN SOURCE
GhostDraw is open source (MIT License)!
GitHub: https://github.com/RuntimeRascal/ghost-draw

💜 Perfect for:
• Presentations and remote meetings
• Educational tutorials and demonstrations
• Collaborative design sessions
• Annotating screenshots and videos
• Creative expression and note-taking

Download GhostDraw today and start drawing on your screen with style! 👻✨
```

**Screenshots** (minimum 1, maximum 10):
- Sizes: 1366×768, 1920×1080, or 2560×1440
- Recommendations:
  1. Drawing mode with colorful artwork
  2. Settings window
  3. Multiple tool examples
  4. System tray menu
  5. Help overlay (F1)

**Release notes:**
```
Initial Microsoft Store release! 🎉

What's included:
✓ Multiple drawing tools (Pen, Line, Rectangle, Circle, Eraser, Arrow, Text)
✓ Customizable hotkeys and color palette
✓ Screenshot functionality (Ctrl+S)
✓ Undo/Redo support (Ctrl+Z)
✓ Help overlay (F1)
✓ System tray integration
✓ Persistent settings
✓ Cyberpunk-themed UI

This is the same fully-featured version available on GitHub, now with automatic updates through the Microsoft Store!
```

#### Submission Options - CRITICAL

**Certification notes for reviewers:**
```
IMPORTANT NOTES FOR CERTIFICATION:

1. RESTRICTED CAPABILITY - runFullTrust:
   Justification: GhostDraw is a desktop WPF application requiring full Win32 API access for:
   - Global keyboard hooks (SetWindowsHookEx with WH_KEYBOARD_LL) for hotkey detection
   - Drawing overlay windows on top of other applications
   - Screenshot functionality using GDI+ APIs

   This is a legitimate Desktop Bridge/WPF application that cannot function without runFullTrust.

2. TESTING INSTRUCTIONS:
   - Launch application (minimizes to system tray)
   - Right-click tray icon → Settings (verify settings window opens)
  - Press Ctrl+Alt+X to activate drawing mode
   - Draw on screen with left mouse button
   - Press ESC to exit drawing mode
   - Verify hotkey works from any application (e.g., activate from Notepad)

3. KEYBOARD HOOKS:
   The app uses low-level keyboard hooks (WH_KEYBOARD_LL) which do NOT inject DLLs
   into other processes. This is a safe, message-based hook fully compatible with MSIX.

4. PRIVACY:
   Application is fully offline. No network connections, no telemetry, no data collection.
   Privacy policy: [YOUR_URL]

5. OPEN SOURCE:
   Source code: https://github.com/RuntimeRascal/ghost-draw
   Licensed under MIT License.
```

### 7.5 Submit for Certification

**Click:** Submit to the Store

**Timeline:**
- Automated testing: 1-4 hours
- Manual review (due to `runFullTrust`): 1-3 business days
- **Total: 1-3 days typically**

**Possible outcomes:**
1. ✓ Approved - App published
2. ⚠ Approved with suggestions - Published but with recommendations
3. ✗ Failed - Fix issues and resubmit

**Common rejection reasons:**
- WACK test failures
- Insufficient `runFullTrust` justification
- Privacy policy issues
- Crashes during manual testing

---

## Phase 8: Dual Distribution Strategy (2 hours)

### 8.1 Maintain Both MSI and MSIX

**Distribution channels:**
1. **GitHub Releases:** MSI + Portable ZIP (existing)
2. **Microsoft Store:** MSIX (new)

**Version synchronization:**
- Use SAME version number for both packages
- Example: GhostDraw v1.0.17 available as:
  - MSI: `GhostDrawSetup-1.0.17.msi`
  - Store: `GhostDraw 1.0.17`
  - Portable: `GhostDraw-1.0.17-portable.zip`

### 8.2 Documentation Updates

**Modify:** `README.md`
- Add Microsoft Store badge and download link
- Explain benefits of Store version (auto-updates, trusted signature)
- Keep direct download option for advanced users

**Create:** `docs\MICROSOFT-STORE.md`
- Store listing URL
- Benefits comparison (MSI vs MSIX)
- Settings location differences
- Migration notes for users switching from MSI to Store

**Update:** `docs\TODO.md`
- Mark MSIX packaging tasks as complete

---

## Phase 9: Ongoing Maintenance

### Update Process for New Versions

1. Update `package.json` version (e.g., 1.0.17 → 1.0.18)
2. Run `Scripts\Sync-Version.ps1`
3. Commit and push changes
4. Build both packages:
   - MSI: `Installer\build.ps1 -Version "1.0.18"`
   - MSIX: `Package\build-msix.ps1 -Version "1.0.18" -CreateUploadPackage`
5. Create GitHub Release (MSI + ZIP)
6. Submit MSIX to Microsoft Store via Partner Center
7. Certification: 1-3 days (faster for updates than initial)
8. Users receive automatic update within 24-48 hours

### Monitor Store Analytics

**Partner Center provides:**
- Acquisitions (downloads)
- Usage (active users)
- Ratings and reviews
- Health (crashes, errors)
- Demographics

---

## Critical Files Summary

### Files to Create

**Packaging Infrastructure:**
- `Package\GhostDraw.Package.wapproj` - MSIX packaging project
- `Package\Package.appxmanifest` - App manifest with identity, capabilities, visual elements
- `Package\build-msix.ps1` - Build automation script
- `Package\create-test-cert.ps1` - Test certificate generation
- `Package\Images\*.png` - 7+ required visual assets

**Automation & Tooling:**
- `Scripts\Sync-Version.ps1` - Version synchronization across all project files

**Documentation:**
- `docs\PRIVACY-POLICY.md` - MANDATORY for Store submission
- `docs\MICROSOFT-STORE.md` - Store-specific documentation

### Files to Modify

- `GhostDraw.sln` - Add packaging project reference
- `.github\workflows\ci.yml` - Add MSIX build steps
- `README.md` - Add Microsoft Store badge and download option
- `docs\TODO.md` - Mark MSIX tasks complete
- `.gitignore` - Add `*.pfx`, `*.cer`, `Package\AppPackages\`

---

## Key Compatibility Notes

### Global Keyboard Hooks Work in MSIX ✓

**Verified compatibility:** WH_KEYBOARD_LL hooks are MSIX-compatible because they:
- Use message-based callbacks (not DLL injection)
- Run in-process on the installing thread
- Require `runFullTrust` capability (which GhostDraw declares)

**Sources:**
- [Understanding MSIX Limitations: A Guide to Enterprise Application Compatibility](https://www.turbo.net/blog/posts/2025-06-16-understanding-msix-limitations-enterprise-application-compatibility)
- [Prepare to package a desktop application (MSIX)](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-prepare)

### Settings Virtualization

**Automatic redirection:**
- `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` works correctly
- MSIX transparently redirects to: `%LocalAppData%\Packages\RuntimeRascal.GhostDraw_*\LocalCache\Local\`
- No code changes needed in GhostDraw

### Microsoft Store Publisher Fees (2025 Update)

**Individual developers:** FREE (announced September 2025)
**Company accounts:** $99 USD one-time fee

**Sources:**
- [Free developer registration for individual developers on Microsoft Store](https://blogs.windows.com/windowsdeveloper/2025/09/10/free-developer-registration-for-individual-developers-on-microsoft-store/)
- [Publish Windows apps and games to Microsoft Store](https://learn.microsoft.com/en-us/windows/apps/publish/)

---

## Success Criteria

**Phase completion checklist:**
- [ ] MSIX package builds successfully locally
- [ ] WACK tests pass
- [ ] All keyboard hooks function correctly in MSIX
- [ ] Settings persist across app restarts
- [ ] Privacy policy published and accessible
- [ ] App name reserved in Partner Center
- [ ] Store submission package created (.msixupload)
- [ ] Store listing completed with screenshots
- [ ] Certification notes provided for `runFullTrust`
- [ ] Submission passes Microsoft certification
- [ ] App published and discoverable in Microsoft Store
- [ ] CI/CD builds both MSI and MSIX
- [ ] Documentation updated with Store links

---

## Total Estimated Effort

**Hands-on time:** 16-24 hours
**Calendar time:** 4-7 days (including Store certification review)

**Breakdown:**
- Prerequisites & setup: 1 hour
- Packaging project creation: 2 hours
- Visual assets creation: 2-3 hours
- Build configuration: 2 hours
- Testing & validation: 3-4 hours
- CI/CD integration: 2-3 hours
- Store submission: 4-6 hours
- Dual distribution setup: 2 hours
- **Microsoft certification review:** 1-3 business days

---

## Conclusion

This plan provides a complete roadmap to publish GhostDraw to the Microsoft Store with MSIX packaging, eliminating the need for an expensive code-signing certificate while providing automatic updates and improved user trust.

The critical finding is that GhostDraw's global keyboard hooks are fully compatible with MSIX packaging when using the `runFullTrust` restricted capability, which is justified for this Desktop Bridge WPF application.

The dual distribution strategy ensures maximum flexibility: enterprise/advanced users can continue using the MSI installer, while mainstream users benefit from the Store version's automatic updates and trusted Microsoft signature.
