# GhostDraw Key Legend

This document serves as the **single source of truth** for all keyboard and mouse input mappings in GhostDraw. Before implementing new features that use keyboard shortcuts or mouse actions, consult this document to avoid conflicts.

---

## 🎯 Purpose

- **Prevent Key Conflicts**: Ensure new features don't reuse existing keybindings
- **Quick Reference**: Single location to check all input mappings
- **Maintainability**: Keep track of all user-facing controls as the app evolves

---
## ⌨️ Keyboard Shortcuts

### Global Hotkey (Configurable)

| Key Combination   | Default      | Action                     | Status     | Notes                                                                                                                   |
| ----------------- | ------------ | -------------------------- | ---------- | ----------------------------------------------------------------------------------------------------------------------- |
| Activation Hotkey | `Ctrl+Alt+D` | Toggle drawing mode on/off | **IN USE** | User-configurable via Settings → Hotkey. Can use multiple modifier keys (Ctrl, Alt, Shift, Win) + any letter/number key |

### Drawing Mode - Tool Selection

| Key | Virtual Key Code | Action                             | Status     | Implementation                    |
| --- | ---------------- | ---------------------------------- | ---------- | --------------------------------- |
| `P` | `0x50` (80)      | Select Pen Tool (freehand drawing) | **IN USE** | `VK_P` in `GlobalKeyboardHook.cs` |
| `L` | `0x4C` (76)      | Select Line Tool (straight lines)  | **IN USE** | `VK_L` in `GlobalKeyboardHook.cs` |
| `A` | `0x41` (65)      | Select Arrow Tool (line + arrow)   | **IN USE** | `VK_A` in `GlobalKeyboardHook.cs` |
| `E` | `0x45` (69)      | Select Eraser Tool                 | **IN USE** | `VK_E` in `GlobalKeyboardHook.cs` |
| `U` | `0x55` (85)      | Select Rectangle Tool              | **IN USE** | `VK_U` in `GlobalKeyboardHook.cs` |
| `C` | `0x43` (67)      | Select Circle Tool                 | **IN USE** | `VK_C` in `GlobalKeyboardHook.cs` |

### Drawing Mode - Actions

| Key      | Virtual Key Code | Action                                     | Status     | Implementation                         |
| -------- | ---------------- | ------------------------------------------ | ---------- | -------------------------------------- |
| `Delete` | `0x2E` (46)      | Clear entire canvas (with confirmation)    | **IN USE** | `VK_DELETE` in `GlobalKeyboardHook.cs` |
| `ESC`    | `0x1B` (27)      | Close modal/help or exit from drawing mode | **IN USE** | `VK_ESCAPE` in `GlobalKeyboardHook.cs` |
| `F1`     | `0x70` (112)     | Show keyboard shortcuts help overlay       | **IN USE** | `VK_F1` in `GlobalKeyboardHook.cs`     |

### Reserved Keys (Do Not Use)

These keys are **reserved** for future functionality or should be avoided to prevent conflicts:

| Key                          | Reason                                                            |
| ---------------------------- | ----------------------------------------------------------------- |
| `F2` - `F12`                 | May be used for future tool shortcuts or system functions         |
| `Ctrl+C`, `Ctrl+V`, `Ctrl+X` | Standard clipboard operations (may implement copy/paste drawings) |
| `Ctrl+Y`                     | Reserved for potential Redo functionality                         |
| `Backspace`                  | Reserved for potential Undo Last Stroke                           |
| `Space`                      | Reserved for potential Pan/Drag Canvas functionality              |
| Arrow Keys                   | Reserved for potential Canvas Navigation                          |
| `Tab`                        | System window switching - do not override                         |
| `Alt+F4`                     | System close window - do not override                             |

---

## 🖱️ Mouse Actions

### Drawing Mode - Mouse Buttons

| Action                | When Active          | Effect                                                       | Status        | Notes                                                       |
| --------------------- | -------------------- | ------------------------------------------------------------ | ------------- | ----------------------------------------------------------- |
| **Left Click**        | Drawing Mode enabled | Begin drawing stroke (Pen/Eraser) or place point (Line tool) | **IN USE**    | Primary drawing action                                      |
| **Left Click + Drag** | Drawing Mode enabled | Draw continuous stroke (Pen) or erase (Eraser)               | **IN USE**    | Main drawing interaction                                    |
| **Right Click**       | Drawing Mode enabled | Cycle to next color in palette                               | **IN USE**    | Implemented in `OverlayWindow.MouseRightButtonDown`         |
| **Middle Click**      | N/A                  | Not currently used                                           | **AVAILABLE** | Could be used for future features (e.g., pan, color picker) |

### Drawing Mode - Mouse Wheel

| Action          | When Active          | Effect                          | Status     | Implementation                   |
| --------------- | -------------------- | ------------------------------- | ---------- | -------------------------------- |
| **Scroll Up**   | Drawing Mode enabled | Increase brush thickness by 1px | **IN USE** | `OverlayWindow.MouseWheel` event |
| **Scroll Down** | Drawing Mode enabled | Decrease brush thickness by 1px | **IN USE** | `OverlayWindow.MouseWheel` event |

**Thickness Constraints:**
- Minimum: Configurable (default: 1px)
- Maximum: Configurable (default: 100px)
- Current adjustment: ±1px per wheel notch

---

## 📋 Input Context Matrix

This table shows which inputs are active in different application states:

| Input                             | System Tray    | Settings Window  | Drawing Mode Inactive | Drawing Mode Active                |
| --------------------------------- | -------------- | ---------------- | --------------------- | ---------------------------------- |
| Activation Hotkey                 | ✓ Triggers     | ✗ Disabled       | ✓ Triggers            | ✓ Toggles Off                      |
| `P`, `L`, `A`, `E`, `U`, `C` keys | ✗              | ✗                | ✗                     | ✓ Tool Selection                   |
| `Delete` key                      | ✗              | ✗                | ✗                     | ✓ Clear Canvas                     |
| `ESC` key                         | ✗              | ✓ Closes Window  | ✗                     | ✓ Close Modal/Help or Exit Drawing |
| `F1` key                          | ✗              | ✗                | ✗                     | ✓ Show Help                        |
| Left Click                        | ✗              | ✓ UI Interaction | ✗                     | ✓ Draw                             |
| Right Click                       | ✓ Context Menu | ✓ UI Interaction | ✗                     | ✓ Cycle Color                      |
| Mouse Wheel                       | ✗              | ✓ Scroll         | ✗                     | ✓ Adjust Thickness                 |

---

## 🔧 Implementation Reference

### Key Constants Location

All virtual key codes are defined in:
```
Src\GhostDraw\Core\GlobalKeyboardHook.cs
```

```csharp
private const int VK_ESCAPE = 0x1B;    // 27
private const int VK_DELETE = 0x2E;    // 46 - 'Delete' key for clear canvas
private const int VK_A = 0x41;         // 65 - 'A' key for arrow tool
private const int VK_L = 0x4C;         // 76 - 'L' key for line tool
private const int VK_P = 0x50;         // 80 - 'P' key for pen tool
private const int VK_E = 0x45;         // 69 - 'E' key for eraser tool
private const int VK_U = 0x55;         // 85 - 'U' key for rectangle tool
private const int VK_C = 0x43;         // 67 - 'C' key for circle tool
private const int VK_F1 = 0x70;        // 112 - 'F1' key for help
```

### Event Handlers

**Keyboard Events:**
- `GlobalKeyboardHook.HotkeyPressed` - Activation hotkey down
- `GlobalKeyboardHook.HotkeyReleased` - Activation hotkey up
- `GlobalKeyboardHook.EscapePressed` - ESC key
- `GlobalKeyboardHook.ClearCanvasPressed` - Delete key
- `GlobalKeyboardHook.PenToolPressed` - P key
- `GlobalKeyboardHook.LineToolPressed` - L key
- `GlobalKeyboardHook.EraserToolPressed` - E key
- `GlobalKeyboardHook.RectangleToolPressed` - U key
- `GlobalKeyboardHook.CircleToolPressed` - C key
- `GlobalKeyboardHook.HelpPressed` - F1 key

**Mouse Events:**
- `OverlayWindow.MouseLeftButtonDown` - Start drawing/place point
- `OverlayWindow.MouseMove` - Continue drawing stroke
- `OverlayWindow.MouseLeftButtonUp` - End drawing stroke
- `OverlayWindow.MouseRightButtonDown` - Cycle color
- `OverlayWindow.MouseWheel` - Adjust thickness

---

## ➕ Adding New Keybindings

Before adding a new keyboard shortcut:

1. **Check this document** - Ensure the key is not already in use
2. **Update this document** - Add the new keybinding to the appropriate section
3. **Add VK constant** - Define in `GlobalKeyboardHook.cs`
4. **Add event** - Create event handler in `GlobalKeyboardHook.cs`
5. **Wire up UI** - Connect to `OverlayWindow` or other components
6. **Update help** - Add to F1 help overlay in `OverlayWindow.xaml`
7. **Update README** - Document user-facing feature in `README.md`
8. **Update CHANGELOG** - Document in release notes

---

## 🔮 Future Considerations

### Potential Features Requiring New Keys

| Feature             | Suggested Key       | Priority | Status                         |
| ------------------- | ------------------- | -------- | ------------------------------ |
| Undo Last Stroke    | `Ctrl+Z`            | High     | ✅ **Implemented**              |
| Redo Stroke         | `Ctrl+Y`            | High     | Not Implemented                |
| Save Drawing        | `Ctrl+S`            | Medium   | ✅ **Implemented** (Screenshot) |
| Load Drawing        | `Ctrl+O`            | Medium   | Not Implemented                |
| Circle/Ellipse Tool | `C`                 | Low      | ✅ **Implemented**              |
| Text Tool           | `T`                 | Low      | Not Implemented                |
| Color Picker        | Middle Click or `I` | Low      | Not Implemented                |
| Toggle Grid/Snap    | `G`                 | Low      | Not Implemented                |
| Zoom In/Out         | `Ctrl+Mouse Wheel`  | Low      | Not Implemented                |

### Keys to Keep Available

Reserve these keys for high-priority future features:
- **`Ctrl+Y`** - Redo (highest priority)
- **`Space`** - Pan/temporary tool toggle

---

## 📜 Version History

| Version | Date | Changes                                                       |
| ------- | ---- | ------------------------------------------------------------- |
| v1.0.9  | 2024 | Added Circle tool (`C` key) with Shift for perfect circles    |
| v1.0.7  | 2024 | Added Rectangle tool (`U` key)                                |
| v1.0.6  | 2024 | Initial key legend. Added Eraser tool (`E` key)               |
| v1.0.5  | 2024 | Added Line tool (`L` key), Pen tool (`P` key), F1 help        |
| v1.0.0  | 2024 | Initial release with activation hotkey, `R` clear, `ESC` exit |

---

## ⚠️ Important Notes

### For Developers

1. **Never reuse keys** without checking this document first
2. **Always update this document** when adding new shortcuts
3. **Test for conflicts** with common application shortcuts
4. **Respect system shortcuts** (Alt+F4, Alt+Tab, Ctrl+Alt+Del)
5. **Document unavailability** if a key can't be captured (e.g., Win key restrictions)

### For Users

- All key mappings (except activation hotkey) are **hard-coded**
- The activation hotkey can be customized in **Settings ? Hotkey**
- Press **F1** while in drawing mode to see current shortcuts
- Some system shortcuts take precedence over GhostDraw shortcuts

---

*Last Updated: v1.0.9*
*Maintained by: GhostDraw Development Team*
