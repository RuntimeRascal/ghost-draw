# Text Tool (Lock Mode) — Feature Plan

## Goal
Add a **Text** drawing tool that lets users place multi-line text on the overlay canvas.

## Non‑Negotiable Requirements (from product decisions)
- **Lock mode required**: Text tool is only usable when `LockDrawingMode` is enabled.
- **Multi-line input**: Enter inserts a newline.
- **Commit rule**: Text is committed by a **second left-click outside the active text area**.
- **Post-commit immutability**: Once committed, text is **not editable**.
- **Removal**: Committed text must be removable via:
  - eraser tool
  - undo
  - clear canvas
- **During an active text session** (caret visible / typing):
  - GhostDraw must **not listen to most keys** (avoid tool switching, clear, undo, screenshot, etc.)
  - **Help (`F1`) must still work**
  - **Draw-mode activation hotkey must still work** (to exit overlays)
  - **ESC must remain an emergency escape**

## UX / Behavior
### Entering text mode
1. User enables drawing mode (must be **lock mode**).
2. User presses `T` to select **Text** tool.
3. User left-clicks anywhere on the canvas → begins a **text session** at that point.

### While typing (text session active)
- A WPF `TextBox` (or equivalent) is shown on the overlay.
- Typing behaves like a normal editor (letters, arrows, backspace, delete, ctrl shortcuts, etc.).
- Text **color and size reflect current settings**, updating in real-time when the user changes them.
  - Color: `AppSettings.ActiveBrush`
  - Size: map from `BrushThickness` (simple mapping, documented below)

### Committing text
- Clicking **outside** the text box commits:
  - Replace the live editor with a committed canvas element (e.g., `TextBlock` or `Path`)
  - Record it in history for undo
  - Keep Text tool selected (next click starts another text session)

### Clicking inside the text area
- Clicking inside does **not** commit; it just places the caret.

### Cancel / emergency exit
- `ESC` always exits drawing mode as today.
- If `ESC` is pressed during a text session, the overlay exits drawing mode; any in-progress editor is removed.

## Multi-monitor expectations
- Each overlay window manages its own text session.
- Committed text should be owned by the overlay where it was created and recorded in history with that overlay’s `OverlayId`.
- Global undo should remove the last committed element on the correct overlay (already handled via `DrawingHistory` + `MultiOverlayWindowOrchestrator`).

## Technical Design
### New/updated states
- `IsTextSessionActive` (per overlay): true while a live `TextBox` editor exists.

### Global keyboard hook gating
When text session is active, the global hook must:
- continue tracking activation hotkey state (so toggling drawing mode still works)
- still emit `F1` help
- still emit `ESC`
- **not** emit tool events for letters
- **not** suppress:
  - Delete (clear)
  - Ctrl+Z (undo)
  - Ctrl+S (screenshot)

Implementation approach:
- Add `GlobalKeyboardHook.SetTextSessionActive(bool)`
- Overlay toggles this on text session start/stop

### Tool architecture
- Add `DrawTool.Text`
- Add `TextTool` implementing `IDrawingTool`
  - `OnMouseDown`: start text session OR commit depending on click target
  - `OnMouseMove/Up`: minimal/no-op (text is click-to-place)
  - `Cancel`: removes in-progress editor
  - `OnColorChanged/OnThicknessChanged`: update live editor styling

### Overlay implementation
- Add a `TextBox` created dynamically and added to `DrawingCanvas`.
- Ensure the `TextBox`:
  - receives focus
  - uses a transparent background and no border (match overlay style)
  - supports multi-line (`AcceptsReturn=true`)
  - does not let the overlay interpret its key input
- Click handling:
  - If active session exists:
    - click inside editor → do nothing (caret only)
    - click outside editor → commit

### History integration
- On commit, call `_drawingHistory.RecordAction(OverlayId, committedElement)`.
- Ensure committed element is a `FrameworkElement` so `Tag` can store GUID.

### Eraser integration
- Eraser currently supports `Polyline`, `Line`, `Rectangle`, `Ellipse`, `Path`.
- Decide committed text representation:
  - Option A (preferred for easy erasing): commit as `Path` geometry (hit-bounds already supported)
  - Option B: commit as `TextBlock` and update `EraserTool` to handle it (bounds intersection)

## Settings mapping (Text size)
- Map brush thickness to font size with a simple linear mapping:
  - `FontSize = clamp(8, 72, BrushThickness * K)`
  - Choose `K` after quick UX test (start with `K=6`)

## Files to change
- `Src/GhostDraw/Core/DrawTool.cs` (add `Text`)
- `Src/GhostDraw/Core/GlobalKeyboardHook.cs` (add `T` handling + text-session gating)
- `Src/GhostDraw/App.xaml.cs` (wire `TextToolPressed`)
- `Src/GhostDraw/Managers/DrawingManager.cs` (add `SetTextTool()`)
- `Src/GhostDraw/Core/ServiceConfiguration.cs` (DI register `TextTool`)
- `Src/GhostDraw/Views/OverlayWindow.xaml.cs` (tool switching + text session visuals)
- `Src/GhostDraw/Tools/TextTool.cs` (new)
- `Src/GhostDraw/Tools/EraserTool.cs` (if needed for committed text element type)
- `docs/KEY_LEGEND.md` (document `T` and text-session key gating)

## Testing plan
### Unit tests (required)
- Add tests in `GhostDraw.Tests` for hook gating helpers:
  - Delete suppression disabled during text session
  - Ctrl+Z suppression disabled during text session
  - Ctrl+S suppression disabled during text session

### Manual safety checks
- Verify ESC always hides overlays even while typing.
- Verify activation hotkey still toggles overlays during typing.
- Verify typing does not switch tools or trigger clear/undo/screenshot.
- Verify multi-monitor: create text on monitor A, undo removes on A.

## Rollout / Incremental Implementation Steps
1. Add `DrawTool.Text` and `T` hotkey event.
2. Add hook text-session gating (no regressions to CallNextHookEx behavior).
3. Add `TextTool` and overlay text session UI.
4. Commit element + history integration.
5. Ensure eraser removes committed text.
6. Add/update documentation and unit tests; run `dotnet test`.

## Open questions (need a quick decision)
- Commit representation: `Path` (geometry) vs `TextBlock`.
- Exact font family (default system) vs a fixed font.
- Whether Ctrl+Enter commits (not required; default is click outside).
