# Changelog

All notable changes to GhostDraw will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
  - Enhanced tool interface consistency

### Fixed
- LineTool's `OnDeactivated` method now properly resets state without calling non-existent method

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
