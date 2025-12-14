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
```

### CI/CD Build

The GitHub Actions workflow includes MSIX build support that can be toggled on/off.

**To enable MSIX builds in CI:**

Edit [.github/workflows/ci.yml](../.github/workflows/ci.yml) and change:

```yaml
env:
    VERSION: '' # Will be set from package.json
    BUILD_MSIX: 'false' # Set to 'true' to enable MSIX package build
```

to:

```yaml
env:
    VERSION: '' # Will be set from package.json
    BUILD_MSIX: 'true' # Set to 'true' to enable MSIX package build
```

When enabled, the CI workflow will:
- Build an unsigned MSIX package (signing disabled for CI)
- Upload the MSIX as a build artifact
- Include the MSIX in draft releases

**Note:** CI builds are unsigned. The Microsoft Store will automatically sign your package during submission.

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

**Account Information:**
- Publisher: **RuntimeRascal2**
- App Name: **Ghost Draw** (already reserved)
- Support Email: **runtimerascal@outlook.com**
- Privacy Policy: https://github.com/RuntimeRascal/ghost-draw/blob/main/docs/PRIVACY-POLICY.md

**Submission Steps:**

1. **Update manifest identity with Publisher ID from Partner Center**:
   - Log into https://partner.microsoft.com/dashboard
   - Navigate to your "Ghost Draw" app
   - Copy the Publisher ID from the App identity page
   - Update `Package.appxmanifest`:
     ```xml
     <Identity Name="RuntimeRascal2.GhostDraw"
               Publisher="CN=YourPublisherIDFromPartnerCenter"
               Version="1.0.17.0" />
     ```

2. **Build Store upload package**:
   ```powershell
   .\build-msix.ps1 -Version "1.0.17" -Configuration Release -CreateUploadPackage
   ```

3. **Upload to Partner Center**:
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

- [Windows Store Plan](../docs/WINDOWS-STORE-PLAN.md) - Full implementation guide
- [MSIX Documentation](https://docs.microsoft.com/windows/msix/)
- [Partner Center](https://partner.microsoft.com/dashboard) - Microsoft Store submission portal
