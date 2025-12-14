# MSIX Package for Microsoft Store

This directory contains the Windows Application Packaging Project for building MSIX packages for Microsoft Store distribution.

## Quick Start

### Local Development Build

```powershell
# 1. Generate test certificate (one-time)
cd Package
.\create-test-cert.ps1

# 2. Install certificate as Administrator (one-time)
.\install-cert.ps1

# 3. Build MSIX package
.\build-msix.ps1 -Version "1.0.17" -Configuration Debug

# 4. Install locally for testing
Add-AppxPackage -Path "AppPackages\GhostDraw.Package_1.0.17.0_x64_Debug_Test\GhostDraw.Package_1.0.17.0_x64_Debug.msix"

# 5. Remove the installed package
Get-AppxPackage -Name "*GhostDraw*" | Remove-AppxPackage
```

## Files

- **GhostDraw.Package.wapproj** - Windows Application Packaging Project
- **Package.appxmanifest** - App manifest with identity, capabilities, and visual elements
- **build-msix.ps1** - Build automation script
- **create-test-cert.ps1** - Generates self-signed certificate for local testing
- **install-cert.ps1** - Installs test certificate to Trusted Root (requires Admin)
- **Generate-Assets.ps1** - Creates visual assets from source icon
- **Images/** - Visual assets (logos, tiles, splash screen)

## Visual Assets

Current assets are auto-generated placeholders from the main app icon. For production:

1. Consider custom designs for:
   - **Wide310x150Logo.png** - Wide tile with app name/branding
   - **SplashScreen.png** - 620x300 with centered logo and tagline
   - **StoreLogo.png** - 50x50 for Store listing

2. Use professional design tools:
   - Figma (free)
   - Inkscape (free, open-source)
   - https://www.appicon.co/

## Microsoft Store Submission

**Submission Steps:**

1. **Build Store upload package**:
   ```powershell
   .\build-msix.ps1 -Version "1.0.17" -Configuration Release -CreateUploadPackage
   ```

2. **Upload to Partner Center**:
   - Navigate to: https://partner.microsoft.com/dashboard
   - Select your "Ghost Draw" app submission
   - Upload the `.msixupload` file from `AppPackages\`
   - Complete Store listing with screenshots and descriptions
   - Submit for certification

## Testing

### Local Installation

After installing the MSIX package locally, verify:

- ✅ Global keyboard hook (Ctrl+Alt+D) works
- ✅ All drawing tools function correctly
- ✅ Settings persist in MSIX virtualized path
- ✅ System tray integration works
- ✅ All keyboard shortcuts work

### Uninstall

```powershell
Get-AppxPackage -Name "*GhostDraw*" | Remove-AppxPackage
```

## Troubleshooting

### Certificate Trust Issues

If you get "certificate not trusted" errors when installing:

1. Run PowerShell as Administrator
2. Execute: `.\install-cert.ps1`
3. Verify certificate is in Trusted Root: `Get-ChildItem Cert:\LocalMachine\Root | Where-Object { $_.Subject -like '*RuntimeRascal*' }`

### Build Failures

- **MSBuild not found**: Ensure Visual Studio 2022 with Windows SDK is installed
- **Wrong SDK version**: Update `TargetPlatformVersion` in `GhostDraw.Package.wapproj` to match installed SDK

## References
- [MSIX Documentation](https://docs.microsoft.com/windows/msix/)
- [Partner Center](https://partner.microsoft.com/dashboard) - Microsoft Store submission portal
