# Changelog

All notable changes to GhostDraw will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## v1.0.15

### Added
- **Arrow Tool**
  - Press `A` while drawing mode is active to select the Arrow tool
  - Click two points to draw a line with an arrowhead
  - Works with undo (`Ctrl+Z`) and can be erased with the Eraser tool


## v1.0.14

### Added
- **Clear Canvas Confirmation Modal**
  - Press `Delete` while drawing mode is active to open a Yes/No confirmation before clearing the canvas
  - While the modal is open, drawing and tool switching are disabled; closing the modal restores the previous tool

### Changed
- **Clear Canvas Hotkey**
  - Clear canvas hotkey changed from `R` to `Delete`
  - `Delete` is suppressed while drawing mode is active to prevent deleting content in underlying apps
- **UI + Docs**
  - Updated overlay hint text and Help shortcuts to reference `Delete`
  - Updated `docs/KEY_LEGEND.md`

### Fixed
- Updated `ClearCanvasFeatureTests` to reflect the new `Delete` hotkey + confirmation flow


## v1.0.13

### Added
- **Undo (Ctrl+Z) with Permanent Eraser Semantics**
  - Press `Ctrl+Z` while drawing mode is active to undo the most recent **completed** action (pen stroke, line, rectangle, circle)
  - Erased items are permanently deleted and are never restored by undo
  - New `DrawingHistory` service tracks completed actions using stable element IDs
  - Added unit tests covering undo behavior and eraser permanence

### Changed
- **Tool Completion Events**
  - Tools report completed actions (pen on mouse-up; shapes on second click) so undo is per-action instead of per-mouse-move
  - Eraser reports erased elements so history entries are marked removed
- **Keyboard Hook Handling for Ctrl+Z**
  - Detects `Ctrl+Z` only when drawing mode is active
  - Suppresses `Ctrl+Z` during drawing mode to prevent pass-through to underlying apps
- **History Reset Behavior**
  - Undo history is cleared when clearing the canvas and when exiting drawing mode


## v1.0.12

### Added
- **F1 Help Toggle Behavior**
  - `F1` now toggles the keyboard shortcuts help overlay on and off instead of auto-hiding after a timeout
  - Help overlay stays visible until explicitly closed with `F1` or `ESC`
  - New tests cover help toggle and ESC behavior to prevent regressions

### Changed
- **ESC Key Behavior with Help Open**
  - Pressing `ESC` while help is visible now only closes the help overlay and **does not** exit drawing mode
  - Pressing `ESC` when help is hidden still performs the normal emergency exit from drawing mode
  - Centralized ESC handling in `OverlayWindow` so drawing manager respects whether it should actually exit
- **Docs**
  - Updated the project README
  - Added CONTRIBUTING document


## v1.0.11

### Added
- **Rectangle Tool: Shift Modifier for Perfect Squares**
  - Hold Shift while drawing rectangles to constrain to perfect squares
  - Uses `Math.Min(width, height)` to fit square within dragged bounds
  - Works during both preview and finalization
  - Consistent with Circle tool's Shift modifier behavior

### Fixed
- **Eraser Tool Now Erases All Shape Types**
  - Eraser now properly removes Rectangle shapes
  - Eraser now properly removes Circle/Ellipse shapes
  - Added bounds intersection detection using `IntersectsWith()` for shape types
  - Added NaN guards for `Canvas.GetLeft/GetTop` to handle uninitialized attached properties
  - Previously only removed Polylines (pen strokes) and Lines

## v1.0.10

### Added
- **Version Display in Settings Window** - Shows current application version in footer
  - Version number appears in bottom-left corner of Settings window
  - Formatted as "Version X.Y.Z"
  - Retrieved from assembly information for accuracy
  - Helps users verify which version they're running

### Changed
- Settings window footer now includes version information for better user awareness

## v1.0.9

### Added
- **Circle/Ellipse Tool** - Draw circles and ellipses by defining a bounding box
  - Press `C` to activate Circle tool
  - Uses bounding box approach: first click sets one corner, second click defines the opposite corner
  - **Hold Shift** while drawing to create perfect circles (equal width and height)
  - Live preview updates as you move the mouse
  - Respects current color and thickness settings
  - Custom circle cursor with corner markers
  - Supports right-click color cycling and mouse wheel thickness adjustment
  - Works seamlessly with Eraser tool
  - Consistent with Rectangle tool behavior for intuitive shape creation

## v1.0.8

### Added
- **Rectangle Tool** - Draw rectangles by clicking two points
  - Press `U` to activate Rectangle tool
  - First click sets one corner, second click finalizes the rectangle
  - Live preview updates as you move the mouse
  - Respects current color and thickness settings
  - Custom rectangle cursor with corner markers
  - Supports right-click color cycling and mouse wheel thickness adjustment
  - Works seamlessly with Eraser tool

## v1.0.7

### Added
- **Screenshot Capture** - Capture your drawings as images
  - Press `Ctrl+S` to capture full screen with drawings (saved to Pictures\GhostDraw)
  - Key suppression prevents Windows from intercepting Ctrl+S during drawing mode
  - Optional: Copy to clipboard, open folder, play shutter sound (configurable in settings)
- **Screenshot Settings Panel** - New UI section in Settings window
  - Toggle clipboard copy, folder opening, and sound effects
  - Configurable save location
- **Key Legend Documentation** - Comprehensive keyboard shortcut reference (`docs/KEY-LEGEND.md`)

### Fixed
- Screenshot hotkey (`Ctrl+S`) now correctly detects Control key by tracking both left (VK_LCONTROL) and right (VK_RCONTROL) control keys instead of generic VK_CONTROL
- Thread safety improvements with volatile field for update nesting level

### Changed
- Snipping tool (`S` key) now properly exits drawing mode to allow user interaction
- User must manually reactivate drawing mode after using snipping tool (press hotkey)

## v1.0.6

### Added
- **Eraser Tool** - Remove drawing objects underneath the cursor
  - Press `E` to activate Eraser tool
  - Click and drag to erase drawings interactively
  - Eraser size adjusts with mouse wheel (same as brush thickness)
  - Intelligent intersection detection for precise erasing
  - Works with both polylines (pen strokes) and lines (straight line tool)
  - Custom eraser cursor with visual feedback
- **Improved Code Quality**
  - Added explicit type aliases to resolve WPF/WinForms namespace conflicts
  - Fixed `Point`, `Brush`, `Color`, `ColorConverter`, and `Brushes` type ambiguities
  - Enhanced tool interface consistency

### Fixed
- Ambiguous reference errors caused by both WPF (`System.Windows`) and WinForms (`System.Drawing`) being enabled
- LineTool's `OnDeactivated` method now properly resets state without calling non-existent method
- Build errors related to namespace conflicts in drawing tool implementations

## v1.0.5

### Added
- **Line Tool** - Draw straight lines by clicking two points
  - Press `L` to activate Line tool
  - First click sets start point, second click finalizes the line
  - Real-time preview follows cursor
  - Line thickness and color adjust dynamically with mouse wheel and right-click
- **Explicit Tool Selection** - Direct keyboard shortcuts for tool switching
  - Press `P` to select Pen tool (freehand drawing)
  - Press `L` to select Line tool (straight lines)
- **Keyboard Shortcuts Help** - F1 help popup showing all available shortcuts
  - Press `F1` while in drawing mode to display help overlay
  - Shows current activation hotkey combination dynamically
  - Displays all drawing tools, actions, and system shortcuts
  - Auto-fades after 4 seconds with smooth animations
- **Settings Architecture Improvements**
  - Abstracted settings persistence with `ISettingsStore` interface
  - `FileSettingsStore` for production file-based persistence
  - `InMemorySettingsStore` for isolated unit testing
  - Eliminated file system dependencies from unit tests

### Changed
- Tool selection now uses explicit methods instead of toggle-only behavior
- Test suite refactored to use in-memory storage, eliminating test pollution
- Settings service now uses dependency injection for storage layer

### Fixed
- Line tool cursor hotspot now correctly positioned at line start point (left circle)
- Test isolation issues resolved - no more shared file system state

## v1.0.0-v1.0.4 - Initial Releases

### Added
- **Drawing Mode Activation**
  - Configurable hotkey combination (default: Ctrl+Alt+D)
  - Toggle or hold mode options
- **Pen Tool** - Freehand drawing with mouse
  - Click and drag to draw smooth polylines
  - Adjustable brush thickness (1-20 pixels)
  - 10-color palette with cycling
- **Brush Customization**
  - Right-click to cycle through color palette
  - Mouse wheel to adjust brush thickness
  - Real-time thickness indicator overlay
- **Canvas Management**
  - Press `R` to clear canvas with visual feedback
  - Press `ESC` for emergency exit from drawing mode
  - Canvas spans all monitors in multi-monitor setups
- **System Tray Integration**
  - Always-running background application
  - Context menu for quick access to settings
  - Log level adjustment from tray
- **Settings Window**
  - Cyberpunk-themed UI
  - Drawing settings (brush thickness, color palette)
  - Hotkey configuration with visual recorder
  - Mode settings (toggle vs hold)
  - Logging level configuration
- **Robust Error Handling**
  - Global exception handler prevents system lockout
  - Comprehensive logging with Serilog
  - Safe hook cleanup on exit
- **Custom Cursors**
  - Colored pencil cursor shows active brush color
  - Line tool cursor with dual circles and connecting line

### Technical Features
- WPF transparent overlay across all screens
- Global Windows keyboard/mouse hooks
- Dependency injection with Microsoft.Extensions.DependencyInjection
- Structured logging with Serilog
- Settings persistence in LocalApplicationData
- .NET 8 target framework

---

## How to Update This Changelog

When adding new features or fixes:

1. Add entries under the `[Unreleased]` section
2. Use these categories:
   - `Added` - New features
   - `Changed` - Changes to existing functionality
   - `Deprecated` - Soon-to-be removed features
   - `Removed` - Removed features
   - `Fixed` - Bug fixes
   - `Security` - Security fixes

3. When releasing a new version:
   - Change `[Unreleased]` to `[X.Y.Z] - YYYY-MM-DD`
   - Create a new `[Unreleased]` section at the top
   - Update version numbers according to [Semantic Versioning](https://semver.org/):
     - MAJOR version for incompatible API changes
     - MINOR version for backwards-compatible functionality additions
     - PATCH version for backwards-compatible bug fixes

### Example Entry Format

```markdown
## [Unreleased]

### Added
- **Feature Name** - Brief description
  - Detail about feature
  - Another detail

### Fixed
- Issue description and what was fixed
