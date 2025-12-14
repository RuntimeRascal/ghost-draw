# GhostDraw Installer

This directory contains the WiX Toolset v4 installer project for GhostDraw.

## Prerequisites

- WiX Toolset v4.0.5 installed (`dotnet tool install --global wix --version 4.0.5`)
- .NET 8 SDK
- Windows 10/11 (x64)

## Building the Installer

### 1. Build the main application (self-contained)

```powershell
dotnet publish ..\src\GhostDraw.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false
```

### 2. Build the installer

```powershell
dotnet build GhostDraw.Installer.wixproj -c Release
```

### 3. Output

The MSI installer will be created at:
```
bin\Release\net8.0-windows\win-x64\GhostDrawSetup.msi
```

## Testing the Installer

### Install
```powershell
msiexec /i GhostDrawSetup.msi /l*v install.log
```

### Uninstall
```powershell
msiexec /x GhostDrawSetup.msi /l*v uninstall.log
```

### Upgrade (install newer version)
```powershell
msiexec /i GhostDrawSetup-v1.1.0.msi /l*v upgrade.log
```

## Files

- `GhostDraw.Installer.wixproj` - WiX project file
- `Product.wxs` - Main installer definition (product info, files, directories)
- `Features.wxs` - Optional features (Start Menu, Desktop, Startup shortcuts)
- `UI.wxs` - Custom installer wizard dialogs
- `Resources/` - Branding assets (icons, banners, license)

## Features

The installer supports:
- ✅ Install to Program Files
- ✅ Clean uninstall (removes files, shortcuts, registry)
- ✅ In-place upgrades (preserves user settings)
- ✅ Downgrade prevention
- ✅ Launch app after installation

## Upgrade Code

⚠️ **CRITICAL**: The UpgradeCode GUID in `Product.wxs` must **NEVER** change across versions:

```xml
<?define UpgradeCode="A8B9C0D1-2E3F-4A5B-6C7D-8E9F0A1B2C3D" ?>
```

Changing this GUID will break upgrade detection.

## Troubleshooting

### Build Errors

**"Could not find file 'GhostDraw.exe'"**
- Build the main application first (step 1 above)

**"Component X has duplicate GUID"**
- Each Component needs a unique GUID (already configured with unique GUIDs)

### Installation Issues

**Check logs**:
```powershell
msiexec /i Setup.msi /l*v install.log
```

Common issues:
- App is running during upgrade (must be closed)
- Insufficient permissions (run as administrator)
- Corrupted MSI file (rebuild)

## Resources

- [WiX Documentation](https://wixtoolset.org/docs/)
- [WiX Tutorial](https://www.firegiant.com/wix/tutorial/)
