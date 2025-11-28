# GhostDraw Installer Implementation Plan

## ?? Overview

This document outlines the implementation strategy for creating a professional Windows installer for GhostDraw using **WiX Toolset v4** (Windows Installer XML). WiX is the industry-standard, open-source toolset for creating MSI installers.

---

## ?? Requirements

### **Functional Requirements**
1. ? **Optional Start Menu entry** - User can choose during installation
2. ? **Optional Desktop shortcut** - User can choose during installation
3. ? **Optional startup entry** - User can choose to run app on Windows login
4. ? **Clean uninstall** - Remove all files, shortcuts, and registry entries
5. ? **Update support** - Detect existing installation and upgrade gracefully
6. ? **Version management** - Prevent downgrade, allow same-version repair

### **Technical Requirements**
- **Installer Type**: MSI (Microsoft Installer)
- **Tool**: WiX Toolset v4.x
- **Target**: .NET 8 Desktop (Windows 10/11)
- **Architecture**: x64 only (modern Windows)
- **UAC**: Require administrator elevation for installation

---

## ??? Architecture

### **Directory Structure**
```
ghost-draw/
??? src/
?   ??? GhostDraw.csproj          # Main application
??? installer/
?   ??? GhostDraw.Installer.wixproj    # WiX project
?   ??? Product.wxs                    # Main installer definition
?   ??? Features.wxs                   # Optional features (shortcuts, startup)
?   ??? UI.wxs                         # Custom installer UI
?   ??? Resources/
?   ?   ??? banner.bmp                 # Top banner (493x58)
?   ?   ??? dialog.bmp                 # Background (493x312)
?   ?   ??? ghost-draw-icon.ico        # Installer icon
?   ??? Scripts/
?       ??? harvest-files.ps1          # Auto-generate file list
??? .github/
?   ??? workflows/
?       ??? build-installer.yml        # CI/CD for building installer
??? docs/
    ??? installer-implementation-plan.md  # This file
```

---

## ?? Implementation Steps

### **Phase 1: Setup WiX Toolset**

#### **1.1 Install WiX Tools**

**Option A: Using .NET Tool (Recommended)**
```powershell
# Install WiX CLI globally
dotnet tool install --global wix --version 4.0.5

# Verify installation
wix --version
```

**Option B: Using Installer**
- Download from: https://github.com/wixtoolset/wix/releases
- Install WiX v4.x MSI

#### **1.2 Create WiX Project**

```powershell
# Create installer directory
mkdir installer
cd installer

# Create new WiX MSI project
wix project new -o GhostDraw.Installer.wixproj -type msi
```

---

### **Phase 2: Define Product Configuration**

#### **2.1 Product.wxs - Main Installer Definition**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Product Id="*"
           Name="GhostDraw"
           Language="1033"
           Version="!(bind.FileVersion.GhostDrawExe)"
           Manufacturer="RuntimeRascal"
           UpgradeCode="12345678-1234-1234-1234-123456789ABC">
    
    <Package InstallerVersion="500"
             Compressed="yes"
             InstallScope="perMachine"
             Description="GhostDraw - On-Screen Drawing Tool"
             Comments="Draw directly on your screen with customizable hotkeys"
             Manufacturer="RuntimeRascal" />

    <!-- Upgrade strategy: Major upgrade -->
    <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed."
                  AllowSameVersionUpgrades="yes"
                  Schedule="afterInstallInitialize" />

    <!-- Embedded CAB -->
    <MediaTemplate EmbedCab="yes" />

    <!-- Icon for Add/Remove Programs -->
    <Icon Id="AppIcon" SourceFile="..\src\Assets\ghost-draw-icon.ico" />
    <Property Id="ARPPRODUCTICON" Value="AppIcon" />
    
    <!-- Program Files -->
    <Property Id="ARPURLINFOABOUT" Value="https://github.com/RuntimeRascal/ghost-draw" />
    <Property Id="ARPNOREPAIR" Value="yes" Secure="yes" />
    <Property Id="ARPNOMODIFY" Value="yes" Secure="yes" />

    <!-- Install location -->
    <StandardDirectory Id="ProgramFiles6432Folder">
      <Directory Id="CompanyFolder" Name="RuntimeRascal">
        <Directory Id="INSTALLFOLDER" Name="GhostDraw" />
      </Directory>
    </StandardDirectory>

    <!-- Start Menu folder -->
    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ApplicationProgramsFolder" Name="GhostDraw"/>
    </StandardDirectory>

    <!-- Desktop folder -->
    <StandardDirectory Id="DesktopFolder" />

    <!-- Startup folder -->
    <StandardDirectory Id="StartupFolder" />
  </Product>
</Wix>
```

**Key Elements:**
- `Product/@Id="*"` - Auto-generate GUID for each build (allows side-by-side installs during dev)
- `Product/@UpgradeCode` - **MUST remain constant** across all versions (enables upgrades)
- `MajorUpgrade` - Automatically removes old version before installing new
- `InstallScope="perMachine"` - Install for all users (requires admin)

---

#### **2.2 Component Groups - Application Files**

```xml
<ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
  <!-- Main executable -->
  <Component Id="GhostDrawExe" Guid="*">
    <File Id="GhostDrawExe"
          Source="..\src\bin\Release\net8.0-windows\win-x64\publish\GhostDraw.exe"
          KeyPath="yes">
      <!-- Register for "Open With" (optional) -->
      <ProgId Id="GhostDraw.Document" Description="GhostDraw Session">
        <Extension Id="gdraw" ContentType="application/x-ghostdraw">
          <Verb Id="open" Command="Open" Argument="&quot;%1&quot;" />
        </Extension>
      </ProgId>
    </File>
  </Component>

  <!-- DLLs -->
  <Component Id="RuntimeDlls" Guid="*">
    <File Source="..\src\bin\Release\net8.0-windows\win-x64\publish\*.dll" />
  </Component>

  <!-- Config files -->
  <Component Id="ConfigFiles" Guid="*">
    <File Source="..\src\bin\Release\net8.0-windows\win-x64\publish\*.json" />
  </Component>

  <!-- Assets -->
  <Component Id="Assets" Guid="*">
    <File Source="..\src\bin\Release\net8.0-windows\win-x64\publish\Assets\*.*" />
  </Component>

  <!-- Settings folder (in %LOCALAPPDATA%) -->
  <Component Id="SettingsFolderRef" Guid="*" Directory="LocalAppDataFolder">
    <CreateFolder Directory="GhostDrawSettingsFolder" />
    <RemoveFolder Id="CleanupSettings" Directory="GhostDrawSettingsFolder" On="uninstall" />
    <RegistryValue Root="HKCU" Key="Software\RuntimeRascal\GhostDraw" 
                   Name="SettingsPath" Value="[LocalAppDataFolder]GhostDraw" 
                   Type="string" KeyPath="yes" />
  </Component>
</ComponentGroup>

<StandardDirectory Id="LocalAppDataFolder">
  <Directory Id="GhostDrawSettingsFolder" Name="GhostDraw" />
</StandardDirectory>
```

---

### **Phase 3: Optional Features (Shortcuts & Startup)**

#### **3.1 Features.wxs - User-Selectable Options**

```xml
<Feature Id="MainApplication"
         Title="GhostDraw Application"
         Level="1"
         ConfigurableDirectory="INSTALLFOLDER"
         Absent="disallow"
         AllowAdvertise="no"
         Description="Core application files (required)">
  <ComponentGroupRef Id="ProductComponents" />
</Feature>

<Feature Id="StartMenuShortcut"
         Title="Start Menu Shortcut"
         Level="1"
         AllowAdvertise="no"
         Description="Add a shortcut to the Start Menu">
  <Component Id="StartMenuShortcutComponent" 
             Guid="*" 
             Directory="ApplicationProgramsFolder">
    <Shortcut Id="StartMenuShortcut"
              Name="GhostDraw"
              Description="Draw on your screen"
              Target="[#GhostDrawExe]"
              WorkingDirectory="INSTALLFOLDER"
              Icon="AppIcon" />
    <RemoveFolder Id="CleanupStartMenu" On="uninstall" />
    <RegistryValue Root="HKCU" Key="Software\RuntimeRascal\GhostDraw" 
                   Name="StartMenuShortcut" Value="1" Type="integer" KeyPath="yes" />
  </Component>
</Feature>

<Feature Id="DesktopShortcut"
         Title="Desktop Shortcut"
         Level="1000"
         AllowAdvertise="no"
         Description="Add a shortcut to the Desktop">
  <Component Id="DesktopShortcutComponent" 
             Guid="*" 
             Directory="DesktopFolder">
    <Shortcut Id="DesktopShortcut"
              Name="GhostDraw"
              Description="Draw on your screen"
              Target="[#GhostDrawExe]"
              WorkingDirectory="INSTALLFOLDER"
              Icon="AppIcon" />
    <RegistryValue Root="HKCU" Key="Software\RuntimeRascal\GhostDraw" 
                   Name="DesktopShortcut" Value="1" Type="integer" KeyPath="yes" />
  </Component>
</Feature>

<Feature Id="StartupShortcut"
         Title="Run at Windows Startup"
         Level="1000"
         AllowAdvertise="no"
         Description="Automatically start GhostDraw when you log in to Windows">
  <Component Id="StartupShortcutComponent" 
             Guid="*" 
             Directory="StartupFolder">
    <Shortcut Id="StartupShortcut"
              Name="GhostDraw"
              Description="Draw on your screen"
              Target="[#GhostDrawExe]"
              Arguments="--minimized"
              WorkingDirectory="INSTALLFOLDER" />
    <RegistryValue Root="HKCU" Key="Software\RuntimeRascal\GhostDraw" 
                   Name="StartupShortcut" Value="1" Type="integer" KeyPath="yes" />
  </Component>
</Feature>
```

**Feature Levels:**
- `Level="1"` - Install by default (checkbox checked)
- `Level="1000"` - Don't install by default (checkbox unchecked)

---

### **Phase 4: Custom Installer UI**

#### **4.1 UI.wxs - Installer Wizard Dialogs**

```xml
<UI Id="CustomUI">
  <!-- Use WixUI_FeatureTree (allows feature selection) -->
  <UIRef Id="WixUI_FeatureTree" />
  <UIRef Id="WixUI_ErrorProgressText" />

  <!-- Custom dialogs -->
  <DialogRef Id="WelcomeDlg" />
  <DialogRef Id="LicenseAgreementDlg" />
  <DialogRef Id="CustomizeDlg" />
  <DialogRef Id="VerifyReadyDlg" />
  <DialogRef Id="ProgressDlg" />
  <DialogRef Id="ExitDialog" />

  <!-- Custom welcome message -->
  <Publish Dialog="WelcomeDlg" Control="Next" Event="NewDialog" Value="LicenseAgreementDlg">1</Publish>
  
  <!-- Show feature tree for customization -->
  <Publish Dialog="LicenseAgreementDlg" Control="Next" Event="NewDialog" Value="CustomizeDlg">LicenseAccepted = "1"</Publish>

  <!-- Branding -->
  <Property Id="WixUIBannerBmp" Value="Resources\banner.bmp" />
  <Property Id="WixUIDialogBmp" Value="Resources\dialog.bmp" />
  <Property Id="WixUILicenseRtf" Value="Resources\License.rtf" />
</UI>
```

#### **4.2 Custom Messages**

```xml
<WixVariable Id="WixUILicenseRtf" Value="Resources\License.rtf" />
<WixVariable Id="WixUIBannerBmp" Value="Resources\banner.bmp" />
<WixVariable Id="WixUIDialogBmp" Value="Resources\dialog.bmp" />

<!-- Launch application after install -->
<Property Id="WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT" 
          Value="Launch GhostDraw" />
<Property Id="WIXUI_EXITDIALOGOPTIONALCHECKBOX" Value="1" />
<Property Id="WixShellExecTarget" Value="[#GhostDrawExe]" />

<CustomAction Id="LaunchApplication"
              BinaryKey="WixCA"
              DllEntry="WixShellExec"
              Impersonate="yes" />

<InstallExecuteSequence>
  <Custom Action="LaunchApplication" After="InstallFinalize">
    WIXUI_EXITDIALOGOPTIONALCHECKBOX = 1 and NOT Installed
  </Custom>
</InstallExecuteSequence>
```

---

### **Phase 5: Build Process**

#### **5.1 GhostDraw.Installer.wixproj**

```xml
<Project Sdk="WixToolset.Sdk/4.0.5">
  <PropertyGroup>
    <OutputName>GhostDrawSetup</OutputName>
    <OutputType>Package</OutputType>
    <WixTargetsPath>$(MSBuildExtensionsPath)\WiX Toolset\v4\wix.targets</WixTargetsPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Product.wxs" />
    <Compile Include="Features.wxs" />
    <Compile Include="UI.wxs" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="Resources\**" />
  </ItemGroup>

  <!-- Ensure main app is built first -->
  <ItemGroup>
    <ProjectReference Include="..\src\GhostDraw.csproj">
      <Name>GhostDraw</Name>
      <Project>{GUID-OF-MAIN-PROJECT}</Project>
    </ProjectReference>
  </ItemGroup>
</Project>
```

#### **5.2 Build Commands**

```powershell
# Build main application (self-contained)
dotnet publish src/GhostDraw.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -p:IncludeNativeLibrariesForSelfExtract=true

# Build installer
dotnet build installer/GhostDraw.Installer.wixproj `
  -c Release

# Output: installer/bin/Release/GhostDrawSetup.msi
```

---

### **Phase 6: Upgrade Logic**

#### **6.1 Version Detection**

```xml
<!-- Detect previous versions -->
<Upgrade Id="12345678-1234-1234-1234-123456789ABC">
  <!-- Upgrade from any older version -->
  <UpgradeVersion OnlyDetect="no"
                  Property="PREVIOUSVERSIONSINSTALLED"
                  Minimum="1.0.0" 
                  Maximum="!(bind.FileVersion.GhostDrawExe)"
                  IncludeMinimum="yes"
                  IncludeMaximum="no" />
  
  <!-- Prevent downgrade -->
  <UpgradeVersion OnlyDetect="yes"
                  Property="NEWERPRODUCTFOUND"
                  Minimum="!(bind.FileVersion.GhostDrawExe)"
                  IncludeMinimum="no" />
</Upgrade>

<CustomAction Id="PreventDowngrade"
              Error="A newer version of [ProductName] is already installed." />

<InstallExecuteSequence>
  <Custom Action="PreventDowngrade" After="FindRelatedProducts">
    NEWERPRODUCTFOUND
  </Custom>
  <RemoveExistingProducts After="InstallInitialize" />
</InstallExecuteSequence>
```

#### **6.2 Settings Preservation**

```xml
<!-- Preserve user settings during upgrade -->
<Component Id="SettingsPreservation" Guid="*" Directory="INSTALLFOLDER">
  <RegistryValue Root="HKCU" Key="Software\RuntimeRascal\GhostDraw" 
                 Name="PreserveSettings" Value="1" Type="integer" KeyPath="yes" />
  
  <!-- Don't remove %LOCALAPPDATA%\GhostDraw during upgrade -->
  <RemoveFolder Id="CleanupSettingsOnUninstall" Directory="GhostDrawSettingsFolder" 
                On="uninstall">
    <![CDATA[NOT UPGRADINGPRODUCTCODE]]>
  </RemoveFolder>
</Component>
```

---

### **Phase 7: GitHub Actions CI/CD**

#### **7.1 .github/workflows/build-installer.yml**

```yaml
name: Build Installer

on:
  push:
    tags:
      - 'v*.*.*'  # Trigger on version tags (e.g., v1.0.0)
  workflow_dispatch:

env:
  DOTNET_VERSION: '8.0.x'
  WIX_VERSION: '4.0.5'

jobs:
  build-installer:
    runs-on: windows-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for version info

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Install WiX Toolset
        run: dotnet tool install --global wix --version ${{ env.WIX_VERSION }}

      - name: Extract version from tag
        id: version
        run: |
          $version = "${{ github.ref }}" -replace 'refs/tags/v', ''
          echo "VERSION=$version" >> $env:GITHUB_OUTPUT
        shell: pwsh

      - name: Build application
        run: |
          dotnet publish src/GhostDraw.csproj `
            -c Release `
            -r win-x64 `
            --self-contained true `
            -p:PublishSingleFile=false `
            -p:Version=${{ steps.version.outputs.VERSION }}

      - name: Build installer
        run: |
          dotnet build installer/GhostDraw.Installer.wixproj `
            -c Release `
            -p:Version=${{ steps.version.outputs.VERSION }}

      - name: Sign installer (optional)
        if: false  # Enable when you have code signing certificate
        run: |
          signtool sign /f certificate.pfx `
            /p ${{ secrets.SIGNING_PASSWORD }} `
            /tr http://timestamp.digicert.com `
            /td sha256 `
            /fd sha256 `
            installer/bin/Release/GhostDrawSetup.msi

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            installer/bin/Release/GhostDrawSetup.msi
          body: |
            ## GhostDraw v${{ steps.version.outputs.VERSION }}
            
            ### Installation
            1. Download `GhostDrawSetup.msi`
            2. Run the installer
            3. Choose optional features (Start Menu, Desktop, Startup)
            4. Click Install
            
            ### What's New
            See [CHANGELOG.md](CHANGELOG.md) for details.
            
            ### System Requirements
            - Windows 10/11 (x64)
            - .NET 8 Runtime (included in installer)
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: GhostDrawSetup-v${{ steps.version.outputs.VERSION }}
          path: installer/bin/Release/GhostDrawSetup.msi
```

---

## ?? Testing Strategy

### **Manual Testing Checklist**

#### **Install Scenarios**
- [ ] Fresh install on clean machine
- [ ] Install with all features selected
- [ ] Install with no optional features
- [ ] Install to custom directory
- [ ] Install while app is running (should fail gracefully)

#### **Upgrade Scenarios**
- [ ] Upgrade from v1.0.0 to v1.1.0
- [ ] Upgrade preserves user settings (`%LOCALAPPDATA%\GhostDraw\settings.json`)
- [ ] Upgrade preserves selected features (shortcuts)
- [ ] Attempt downgrade (should be blocked)
- [ ] Reinstall same version (repair)

#### **Uninstall Scenarios**
- [ ] Uninstall from Control Panel ? Programs and Features
- [ ] Verify all files removed from `Program Files`
- [ ] Verify shortcuts removed (Start Menu, Desktop, Startup)
- [ ] Verify registry keys removed
- [ ] Verify settings folder remains (user data)

#### **Feature Testing**
- [ ] Start Menu shortcut launches app
- [ ] Desktop shortcut launches app
- [ ] Startup shortcut runs on login
- [ ] Uncheck features during install ? not created
- [ ] Modify installation to add/remove features

---

## ?? Version Strategy

### **Semantic Versioning**
```
MAJOR.MINOR.PATCH

Examples:
1.0.0 - Initial release
1.1.0 - Add eraser feature
1.1.1 - Fix eraser cursor bug
2.0.0 - Breaking: Redesigned settings format
```

### **UpgradeCode Management**
```xml
<!-- NEVER CHANGE THIS GUID -->
<Product UpgradeCode="12345678-1234-1234-1234-123456789ABC">
```

?? **Critical**: The `UpgradeCode` must remain constant across all versions. Changing it breaks upgrade detection.

---

## ?? Security Considerations

### **Code Signing (Recommended for Production)**

```powershell
# Sign the MSI with your certificate
signtool sign /f YourCertificate.pfx `
  /p YourPassword `
  /tr http://timestamp.digicert.com `
  /td sha256 `
  /fd sha256 `
  GhostDrawSetup.msi
```

**Benefits:**
- Removes "Unknown Publisher" warning
- Builds user trust
- Required for some enterprise environments

**Cost**: $100-$400/year for code signing certificate

---

## ?? Resources

### **WiX Documentation**
- Official Docs: https://wixtoolset.org/docs/
- Tutorial: https://www.firegiant.com/wix/tutorial/
- Schema Reference: https://wixtoolset.org/docs/schema/

### **Tools**
- WiX Toolset: https://github.com/wixtoolset/wix
- Orca (MSI Editor): https://learn.microsoft.com/en-us/windows/win32/msi/orca-exe
- Dark (Decompile MSI): Included with WiX

### **Community**
- WiX GitHub Discussions: https://github.com/wixtoolset/issues/discussions
- Stack Overflow: https://stackoverflow.com/questions/tagged/wix

---

## ? Success Criteria

- [ ] MSI builds successfully in CI/CD
- [ ] Installer size < 50 MB
- [ ] Fresh install completes in < 30 seconds
- [ ] Upgrade preserves user settings
- [ ] Uninstall removes all files except user data
- [ ] All optional features work correctly
- [ ] No UAC prompts after installation (for normal app use)
- [ ] Installer works on Windows 10 and 11
- [ ] Release workflow publishes MSI to GitHub Releases

---

## ?? Implementation Timeline

**Phase 1**: Setup (1 day)
- Install WiX Toolset
- Create installer project structure
- Define Product.wxs skeleton

**Phase 2**: Core Installer (2 days)
- Define all components and files
- Implement feature selection
- Test fresh install

**Phase 3**: UI Customization (1 day)
- Create custom dialogs
- Add branding (banner, dialog images)
- Add license agreement

**Phase 4**: Upgrade Logic (1 day)
- Implement MajorUpgrade
- Test upgrade scenarios
- Verify settings preservation

**Phase 5**: CI/CD (1 day)
- Create GitHub Actions workflow
- Test build automation
- Publish to releases

**Phase 6**: Testing & Polish (2 days)
- Run full test matrix
- Fix any issues
- Document installation process

---

**Total Estimated Time**: 8 days (1-2 weeks with testing)

---

*Last Updated: 2025-01-28*
