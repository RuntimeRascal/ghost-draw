# GhostDraw Installer - Quick Start Guide

## ?? What We're Building

A professional Windows MSI installer using **WiX Toolset v4** that:
- ? Installs GhostDraw to Program Files
- ? Offers optional Start Menu shortcut
- ? Offers optional Desktop shortcut  
- ? Offers optional "Run at Startup" option
- ? Cleanly uninstalls (removes files, shortcuts, registry)
- ? Supports in-place upgrades (preserves user settings)
- ? Prevents downgrades
- ? Builds automatically via GitHub Actions

---

## ?? Quick Implementation Steps

### **1. Install WiX Toolset**
```powershell
dotnet tool install --global wix --version 4.0.5
wix --version  # Verify installation
```

### **2. Create Installer Project**
```powershell
# From repo root
mkdir installer
cd installer
wix project new -o GhostDraw.Installer.wixproj -type msi
```

### **3. Key Files to Create**

| File | Purpose |
|------|---------|
| `Product.wxs` | Main installer definition (product info, files) |
| `Features.wxs` | Optional features (shortcuts, startup) |
| `UI.wxs` | Custom installer wizard dialogs |
| `Resources/banner.bmp` | Top banner image (493×58) |
| `Resources/dialog.bmp` | Background image (493×312) |
| `Resources/License.rtf` | License agreement text |

### **4. Build Installer**
```powershell
# Build main app (self-contained)
dotnet publish src/GhostDraw.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true

# Build installer
dotnet build installer/GhostDraw.Installer.wixproj -c Release

# Output: installer/bin/Release/GhostDrawSetup.msi
```

### **5. Test Installer**
```powershell
# Install
msiexec /i GhostDrawSetup.msi /l*v install.log

# Uninstall
msiexec /x GhostDrawSetup.msi /l*v uninstall.log

# Upgrade (install newer version over old)
msiexec /i GhostDrawSetup-v1.1.0.msi /l*v upgrade.log
```

---

## ?? Critical Concepts

### **UpgradeCode (NEVER CHANGE)**
```xml
<Product UpgradeCode="12345678-1234-1234-1234-123456789ABC">
```
?? This GUID must remain constant forever. It's how Windows knows "GhostDraw v1.1.0" is an upgrade to "GhostDraw v1.0.0".

### **Feature Levels**
```xml
<Feature Level="1">    <!-- Install by default (checked) -->
<Feature Level="1000"> <!-- Don't install by default (unchecked) -->
```

### **Component Rules**
- One Component = One registry KeyPath OR one file KeyPath
- Each Component needs unique GUID (use `Guid="*"` for auto-generation)
- Components track what's installed for clean uninstall

---

## ?? Testing Checklist

### **Before Each Release**
- [ ] Fresh install works
- [ ] Upgrade from previous version works
- [ ] Settings are preserved during upgrade
- [ ] Uninstall removes all files
- [ ] Optional features work (Start Menu, Desktop, Startup)
- [ ] App launches after "Launch GhostDraw" checkbox

### **Edge Cases**
- [ ] Install while app is running (should fail gracefully)
- [ ] Attempt downgrade (should be blocked)
- [ ] Repair installation (reinstall same version)
- [ ] Custom install directory works

---

## ?? Branding Assets Needed

### **Installer Icon**
- **File**: `Resources/ghost-draw-icon.ico`
- **Source**: Copy from `src/Assets/ghost-draw-icon.ico`
- **Used for**: Add/Remove Programs icon

### **Banner Image**
- **File**: `Resources/banner.bmp`
- **Size**: 493 × 58 pixels
- **Format**: 24-bit BMP
- **Content**: GhostDraw logo + "Setup Wizard"

### **Dialog Background**
- **File**: `Resources/dialog.bmp`
- **Size**: 493 × 312 pixels
- **Format**: 24-bit BMP
- **Content**: Cyberpunk-themed background with ghost/draw imagery

### **License Agreement**
- **File**: `Resources/License.rtf`
- **Format**: Rich Text Format (RTF)
- **Content**: MIT License or your chosen license

---

## ?? Release Workflow

### **Manual Release**
```powershell
# 1. Update version in GhostDraw.csproj
<Version>1.1.0</Version>

# 2. Build installer
dotnet build installer/GhostDraw.Installer.wixproj -c Release

# 3. Test locally
msiexec /i installer/bin/Release/GhostDrawSetup.msi

# 4. Create Git tag
git tag v1.1.0
git push origin v1.1.0

# 5. Upload MSI to GitHub Releases
```

### **Automated Release (GitHub Actions)**
```bash
# Just push a version tag
git tag v1.1.0
git push origin v1.1.0

# GitHub Actions will:
# - Build the application
# - Build the installer
# - Create a GitHub Release
# - Upload the MSI
```

---

## ?? Troubleshooting

### **Build Errors**

**"Error: Could not find file 'GhostDraw.exe'"**
- **Solution**: Build the main app first: `dotnet publish src/GhostDraw.csproj`

**"Error: UpgradeCode cannot be changed"**
- **Solution**: Revert `UpgradeCode` to original value in `Product.wxs`

**"Error: Component X has duplicate GUID"**
- **Solution**: Use `Guid="*"` for auto-generation or ensure unique GUIDs

### **Installation Issues**

**"Installation failed with error 1603"**
- **Check logs**: `msiexec /i Setup.msi /l*v install.log`
- **Common causes**: App is running, insufficient permissions, corrupted MSI

**"Settings were lost after upgrade"**
- **Check**: Settings folder should be `%LOCALAPPDATA%\GhostDraw`
- **Fix**: Add `RemoveFolder` condition: `NOT UPGRADINGPRODUCTCODE`

**"Shortcuts not created"**
- **Check**: Feature was selected during install
- **Fix**: Modify installation via Control Panel ? Add/Remove Features

---

## ?? Next Steps

1. **Read full plan**: `docs/installer-implementation-plan.md`
2. **Install WiX**: `dotnet tool install --global wix`
3. **Create project structure**: Follow Phase 1 of implementation plan
4. **Start with Product.wxs**: Define basic product metadata
5. **Test early, test often**: Build and test installer frequently

---

## ?? Need Help?

- **WiX Tutorial**: https://www.firegiant.com/wix/tutorial/
- **WiX Documentation**: https://wixtoolset.org/docs/
- **GitHub Discussions**: https://github.com/wixtoolset/issues/discussions
- **Stack Overflow**: https://stackoverflow.com/questions/tagged/wix

---

**Ready to start? Begin with Phase 1 of the implementation plan!** ??
