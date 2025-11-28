# GhostDraw Installer Implementation Status

**Last Updated:** 2025-11-28
**Current Phase:** Phase 2 - Core Installer (In Progress)
**Reference:** [installer-implementation-plan.md](./installer-implementation-plan.md)

---

## 📋 Implementation Phases Overview

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1: Setup WiX Toolset | ✅ Complete | WiX 4.0.5 installed, project created |
| Phase 2: Core Installer | ✅ Complete | Installs app and all dependencies correctly |
| Phase 3: Optional Features | 🔄 In Progress | Features defined, need testing verification |
| Phase 4: Custom UI | 🔄 In Progress | Basic UI, needs custom dialogs and branding |
| Phase 5: Build Process | ✅ Working | Builds and installs successfully |
| Phase 6: Upgrade Logic | ❌ Not Started | - |
| Phase 7: CI/CD | ❌ Not Started | - |

---

## 🎯 Current Implementation Status

### Phase 1: Setup WiX Toolset ✅
- [x] WiX Toolset v4.0.5 installed
- [x] Installer project created at `installer/GhostDraw.Installer.wixproj`
- [x] Basic directory structure created

**Files Created:**
- `installer/GhostDraw.Installer.wixproj`
- `installer/Product.wxs`
- `installer/Resources/License.rtf`
- `installer/Resources/README.md`

---

### Phase 2: Core Installer 🔄

#### ✅ Completed Items:
- [x] Created `Product.wxs` with basic structure
- [x] Added WiX v4 `<Package>` element (not old `<Product>`)
- [x] Configured `MajorUpgrade` for version management
- [x] Set UpgradeCode: `A8B9C0D1-2E3F-4A5B-6C7D-8E9F0A1B2C3D`
- [x] Defined install directories (Program Files, Start Menu, Desktop, Startup)
- [x] Added all Microsoft.Extensions.* DLL dependencies
- [x] Added all Serilog.* DLL dependencies
- [x] Fixed `EnableDefaultCompileItems` issue (WiX was auto-including files)

#### ❌ Outstanding Issues:
1. **Critical: Installer not actually installing files**
   - Build succeeds (820KB MSI created)
   - Installation completes but files not in `C:\Program Files\RuntimeRascal\GhostDraw\`
   - App doesn't launch from shortcuts
   - Exit code 103 when running installer

2. **WiX v4 Syntax Issues:**
   - Plan uses old WiX v3 `<Product>` syntax, we're using WiX v4 `<Package>`
   - Plan shows `Guid="*"` for auto-generation, but v4 may handle differently
   - Wildcard file includes (`*.dll`, `*.json`) not working - need explicit file lists

3. **Missing Components per Plan:**
   - [ ] Assets folder not being copied
   - [ ] Need to verify all runtime files are included

#### 📝 Current File Structure:
```
installer/
├── GhostDraw.Installer.wixproj
│   - Platform: x64
│   - EnableDefaultCompileItems: false (CRITICAL FIX)
│   - Includes WixToolset.UI.wixext package
│   - ProjectReference to main GhostDraw.csproj
├── Product.wxs
│   - Package element (WiX v4 syntax)
│   - 4 Features: MainApplication, StartMenuShortcut, DesktopShortcut, StartupShortcut
│   - ComponentGroups: ProductComponents, SettingsComponents
│   - All DLLs explicitly listed
│   - UI fragment with WixUI_FeatureTree
└── Resources/
    └── License.rtf
```

---

### Phase 3: Optional Features 🔄

#### ✅ Completed Items:
- [x] Created 4 Features in Product.wxs:
  - MainApplication (required)
  - StartMenuShortcut (default ON)
  - DesktopShortcut (default OFF)
  - StartupShortcut (default OFF)
- [x] Added shortcut components for each feature
- [x] Configured registry-based KeyPath for shortcuts

#### ⚠️ Known Warnings:
- ICE69: Mismatched component references (shortcuts reference GhostDrawExe in different feature)
  - Worked around by adding `ComponentRef Id="GhostDrawExe"` to shortcut features
  - Still generates warnings but builds successfully

#### ❌ Outstanding Items:
- [ ] Shortcuts not tested (can't test until installer actually installs files)
- [ ] Need to verify startup shortcut `--minimized` argument works with app

---

### Phase 4: Custom UI 🔄

#### ✅ Completed Items:
- [x] Fixed WiX v4 UI syntax (ui:WixUI)
- [x] Added UI namespace to root Wix element
- [x] Configured WixUI_FeatureTree (shows feature selection)
- [x] Added License.rtf reference
- [x] Added WixUI_ErrorProgressText for error handling

#### 🧪 Pending Testing:
- [ ] Verify installer shows welcome dialog
- [ ] Verify license agreement dialog
- [ ] Verify feature selection tree (Custom Setup)
- [ ] Verify installation progress dialog
- [ ] Verify completion dialog

#### ❌ Outstanding Items per Plan:
- [ ] Custom branding images:
  - [ ] banner.bmp (493x58)
  - [ ] dialog.bmp (493x312)
- [ ] Launch app after install checkbox
- [ ] Custom welcome message (optional enhancement)

---

### Phase 5: Build Process ⚠️

#### ✅ Working:
- [x] Build command succeeds
- [x] Creates 820KB MSI file
- [x] Main app publishes successfully to `src/bin/Release/net8.0-windows/win-x64/publish/`

#### ❌ Not Working:
- [ ] **CRITICAL: MSI doesn't actually install files to Program Files**
- [ ] Need to diagnose why installation "succeeds" but files aren't copied

#### Current Build Commands:
```powershell
# Publish app (works)
dotnet publish src/GhostDraw.csproj -c Release -r win-x64 --self-contained false

# Build installer (builds but doesn't install correctly)
dotnet build installer/GhostDraw.Installer.wixproj -c Release
```

---

### Phase 6: Upgrade Logic ❌

**Status:** Not started (blocked by Phase 2 issues)

**Required per Plan:**
- [ ] Implement UpgradeVersion detection
- [ ] Add PreventDowngrade custom action
- [ ] Configure RemoveExistingProducts sequence
- [ ] Add settings preservation logic
- [ ] Test upgrade scenarios

---

### Phase 7: CI/CD ❌

**Status:** Not started

**Required per Plan:**
- [ ] Create `.github/workflows/build-installer.yml`
- [ ] Configure version extraction from git tags
- [ ] Add code signing step (optional)
- [ ] Configure GitHub release creation

---

## 🔍 Key Technical Details

### WiX v4 vs v3 Differences (CRITICAL)
The plan was written for WiX v3, but we're using WiX v4. Key differences:

**1. Main Element:**
- **v3:** `<Product>` element
- **v4:** `<Package>` element

**2. File Wildcards:**
- **v3:** Wildcards work in File Source (`*.dll`)
- **v4:** May require explicit file listing or heat.exe harvesting

**3. UI Built-in Sets (CRITICAL - Main Blocker):**
- **v3:** `<UIRef Id="WixUI_FeatureTree" />`
- **v4:** `<ui:WixUI Id="WixUI_FeatureTree" />` with namespace `xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui"`

**4. Namespace Requirements:**
- **v4 requires** the UI namespace to be declared in the root `<Wix>` element
- Without proper namespace and syntax, UI silently fails to load

### Current UpgradeCode
**CRITICAL - DO NOT CHANGE:**
```
UpgradeCode="A8B9C0D1-2E3F-4A5B-6C7D-8E9F0A1B2C3D"
```

### Component GUIDs (Fixed)
All components use explicit GUIDs to avoid conflicts:
- GhostDrawExe: `B1C2D3E4-5F6A-7B8C-9D0E-1F2A3B4C5D6E`
- GhostDrawDll: `C2D3E4F5-6A7B-8C9D-0E1F-2A3B4C5D6E7F`
- MicrosoftExtensionsDlls: `D3E4F5A6-7B8C-9D0E-1F2A-3B4C5D6E7F8A`
- SerilogDlls: `E4F5A6B7-8C9D-0E1F-2A3B-4C5D6E7F8A9B`
- ConfigFiles: `F5A6B7C8-9D0E-1F2A-3B4C-5D6E7F8A9B0C`
- SettingsFolderRef: `A6B7C8D9-0E1F-2A3B-4C5D-6E7F8A9B0C1D`
- StartMenuShortcutComponent: `A1B2C3D4-5E6F-7A8B-9C0D-1E2F3A4B5C6D`
- DesktopShortcutComponent: `B2C3D4E5-6F7A-8B9C-0D1E-2F3A4B5C6D7E`
- StartupShortcutComponent: `C3D4E5F6-7A8B-9C0D-1E2F-3A4B5C6D7E8F`

### Application Files Required
Based on `src/bin/Release/net8.0-windows/win-x64/publish/`:
- GhostDraw.exe
- GhostDraw.dll
- GhostDraw.runtimeconfig.json
- GhostDraw.deps.json
- Microsoft.Extensions.*.dll (9 files)
- Serilog.*.dll (5 files)

---

## 🚨 Current Status

### ✅ **Basic Installation Works!**
- Installer builds successfully (820KB MSI)
- Installs application to Program Files
- Includes all dependencies (exe, dlls, config files)
- App launches correctly
- Icon appears in Settings → Apps ✅ (fixed with favicon.ico)

### ✅ **UI Dialogs Fixed! (2025-11-28)**
**Problem Solved:** WiX v4 uses different syntax than v3 for built-in UI sets

**Root Cause:** The implementation plan was written for WiX v3, but we're using WiX v4.

**Solution:**
```xml
<!-- WiX v3 (WRONG for v4) -->
<UIRef Id="WixUI_FeatureTree" />

<!-- WiX v4 (CORRECT) -->
<ui:WixUI Id="WixUI_FeatureTree" />
```

**Key Changes Made:**
1. Added UI namespace: `xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui"`
2. Changed from `<UIRef>` to `<ui:WixUI>` for built-in UI sets
3. Kept `<UIRef Id="WixUI_ErrorProgressText" />` for error text

**Status:** ✅ **FIXED** - Installer now shows full UI wizard (pending user test)

**Resources Used:**
- [WiX v4 UI Example](https://github.com/iswix-llc/iswix-tutorials/blob/master/WiX-v4-HeatWaveBuildTools/desktop-application/Installer/DesktopApplication/UI.wxs)
- [Stack Overflow: WiX v4 UI](https://stackoverflow.com/questions/77094493/how-to-add-ui-to-installer-for-wix-v4-heatwave-for-vs2022-firegiant)

### 🎯 **Features To Implement Next**
1. ✅ UI dialogs (FIXED - needs user testing)
2. Test feature selection (Start Menu, Desktop, Startup shortcuts)
3. Add branding assets (banner.bmp, dialog.bmp)
4. Add launch-after-install checkbox
5. Upgrade logic and version management
6. CI/CD pipeline

---

## 📝 Testing Checklist

### Installation Testing (Blocked)
- [ ] Fresh install creates files in Program Files
- [ ] App launches after installation
- [ ] App appears in system tray
- [ ] Start Menu shortcut works
- [ ] Desktop shortcut works (when selected)
- [ ] Startup shortcut works (when selected)

### Uninstall Testing (Blocked)
- [ ] Uninstall removes all files
- [ ] Shortcuts removed
- [ ] Registry entries cleaned up
- [ ] User settings preserved in LocalAppData

---

## 🎯 Next Steps (Incremental Implementation)

### Phase 3: Optional Features Testing & Fixes
1. **Test current feature selection**
   - [ ] Verify Start Menu shortcut can be selected/deselected
   - [ ] Verify Desktop shortcut option works
   - [ ] Verify Startup shortcut option works
   - [ ] Test that unchecking features prevents installation

2. **Fix any feature issues found**
   - Fix shortcut creation if not working
   - Ensure feature tree displays correctly in installer UI

### Phase 4: Custom UI & Branding (Next Priority)
3. **Implement custom dialog flow**
   - [ ] Add proper dialog sequence (Welcome → License → CustomizeDlg → Ready → Install → Exit)
   - [ ] Configure dialog navigation properly

4. **Add branding assets**
   - [ ] Create/add banner.bmp (493x58)
   - [ ] Create/add dialog.bmp (493x312)
   - [ ] Update WixVariable references

5. **Add launch-after-install**
   - [ ] Add checkbox on exit dialog
   - [ ] Implement WixShellExec custom action
   - [ ] Test app launches after install

### Phase 6: Upgrade Logic
6. **Implement upgrade detection**
   - [ ] Add UpgradeVersion detection
   - [ ] Test upgrade scenarios
   - [ ] Verify settings preservation

### Phase 7: CI/CD
7. **Automate builds**
   - [ ] Create GitHub Actions workflow
   - [ ] Configure automatic releases

---

## 📚 Reference Information

### Key Files to Monitor
- `installer/Product.wxs` - Main installer definition
- `installer/GhostDraw.Installer.wixproj` - Project configuration
- `src/GhostDraw.csproj` - Main app (must publish before building installer)

### Build Warnings (Acceptable)
- WIX1076 ICE61: Version comparison warning (acceptable for v1.0.0)
- WIX1076 ICE69: Component reference warnings (worked around, not critical)

### Documentation
- Plan: `docs/installer-implementation-plan.md`
- Quick Start: `docs/installer-quickstart.md`
- WiX v4 Docs: https://wixtoolset.org/docs/

---

## 🔄 Change Log

### 2025-11-28 (Evening) - UI FIX! 🎉
- **MAJOR FIX:** Discovered WiX v4 UI syntax difference from v3
  - Changed from `<UIRef Id="WixUI_FeatureTree" />` to `<ui:WixUI Id="WixUI_FeatureTree" />`
  - Added UI namespace: `xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui"`
  - Found via web search and [WiX v4 examples](https://github.com/iswix-llc/iswix-tutorials)
- Installer now configured with full UI wizard (pending user test)

### 2025-11-28 (Afternoon)
- Fixed icon in Settings → Apps (changed from .png to favicon.ico)
- Attempted multiple approaches to activate UI (all failed due to wrong syntax)
  - Tried UIRef in Fragment (v3 syntax)
  - Tried WixUI_Minimal (v3 syntax)
  - Added WIXUI_INSTALLDIR property
  - All attempts built successfully but UI didn't show
- Documented issue and began deep research

### 2025-11-28 (Morning)
- Fixed critical WiX build error (duplicate entry sections)
  - Added `EnableDefaultCompileItems=false` to project file
  - WiX SDK was auto-including .wxs files causing duplicates
- Added all DLL dependencies explicitly (15 DLL files)
- Updated Component GUIDs to avoid conflicts
- Installer builds successfully (820KB)
- Confirmed installation works correctly

### Earlier
- Created initial installer structure
- Added basic Product.wxs
- Configured features and shortcuts
- Added WixToolset.UI.wixext package reference

---

*This document should be updated after each implementation step and testing cycle.*
