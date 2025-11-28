# Settings Architecture

## Overview

GhostDraw uses a robust settings system that persists user preferences to disk and synchronizes them in real-time across the application. This document explains the architecture, data flow, and critical implementation patterns.

## Components

### 1. **AppSettings** (`src/AppSettings.cs`)
- **Purpose**: Data model for application settings
- **Location**: Serialized to `%LOCALAPPDATA%\GhostDraw\settings.json`
- **Properties**:
  - `BrushColor`: Hex color (e.g., "#FF0000")
  - `BrushThickness`: Current brush thickness (double)
  - `MinBrushThickness`: Minimum allowed thickness (double, default: 1.0)
  - `MaxBrushThickness`: Maximum allowed thickness (double, default: 20.0)
  - `HotkeyModifier1/2/Key`: Hotkey combination
  - `LockDrawingMode`: Toggle vs. hold mode (bool)
  - `LogLevel`: Logging verbosity (string)
  - `ColorPalette`: Available colors (List<string>)

### 2. **AppSettingsService** (`src/AppSettingsService.cs`)
- **Purpose**: Manages loading, saving, and updating settings
- **Pattern**: Singleton service registered in DI container
- **Key Features**:
  - Automatic file creation if missing
  - Default settings fallback
  - Real-time persistence (every change saves to disk)
  - Event-based notifications for UI updates

**Events:**
```csharp
public event EventHandler<string>? BrushColorChanged;
public event EventHandler<double>? BrushThicknessChanged;
public event EventHandler<bool>? LockDrawingModeChanged;
public event EventHandler<(double min, double max)>? BrushThicknessRangeChanged;
```

**Methods:**
- `CurrentSettings`: Returns a clone (read-only copy) of current settings
- `SetBrushColor(string)`: Updates color, saves to disk, raises event
- `SetBrushThickness(double)`: Updates thickness (clamped to min/max), saves, raises event
- `SetBrushThicknessRange(double, double)`: Updates min/max, auto-adjusts current if needed
- `SetLockDrawingMode(bool)`: Updates mode, saves, raises event
- `GetNextColor()`: Cycles to next color in palette

### 3. **SettingsWindow** (`src/SettingsWindow.xaml.cs`)
- **Purpose**: UI for configuring settings
- **Pattern**: Event-driven with nesting level protection (see below)
- **Responsibilities**:
  - Display current settings
  - Handle user input
  - Update service when user changes settings
  - Update UI when service notifies of external changes (e.g., mouse wheel while drawing)

---

## Critical Pattern: Nesting Level Protection

### The Problem

Settings updates can trigger cascading events that lead to infinite recursion or race conditions:

```
User types in MinThicknessTextBox
  ↓
TextChanged event fires
  ↓
Calls SetBrushThicknessRange()
  ↓
Service saves to disk and raises BrushThicknessRangeChanged event
  ↓
Event handler updates MinThicknessTextBox.Text
  ↓
TextChanged event fires AGAIN! ❌ RECURSION
```

### The Solution: Nesting Level Counter

Instead of a boolean flag (which gets overwritten), we use an **integer counter** to track nesting depth:

```csharp
private int _updateNestingLevel = 0;  // Track nesting depth of updates
```

**Pattern:**
```csharp
_updateNestingLevel++;
try
{
    // Safe to update UI and call services
    _appSettings.SetBrushThicknessRange(minValue, maxValue);
    ThicknessSlider.Minimum = minValue;
    ThicknessSlider.Maximum = maxValue;
}
finally
{
    _updateNestingLevel--;  // Always decrement, even on exception
}
```

**Checking if user-initiated:**
```csharp
if (_updateNestingLevel > 0)
{
    // Programmatic update - skip to prevent recursion
    return;
}

// Only execute if user directly triggered this event
_appSettings.SetBrushThickness(value);
```

### Why Nesting Level Instead of Boolean?

**Boolean flag (BROKEN):**
```csharp
_isUpdatingFromEvent = true;
SetBrushThicknessRange();  // Raises event
  // Event handler sets _isUpdatingFromEvent = false
// ❌ Flag is now false even though we're still in the original call!
ThicknessSlider.Minimum = min;  // NOT PROTECTED!
```

**Nesting level (CORRECT):**
```csharp
_updateNestingLevel++;  // Now = 1
SetBrushThicknessRange();  // Raises event
  _updateNestingLevel++;  // Now = 2
  // Update UI
  _updateNestingLevel--;  // Now = 1
// ✅ Still = 1, so ThicknessSlider update is still protected
ThicknessSlider.Minimum = min;  // PROTECTED
_updateNestingLevel--;  // Now = 0
```

---

## Data Flow Diagrams

### Startup Flow

```
Application Start
    ↓
AppSettingsService constructor
    ↓
Check if settings.json exists
    ↓
┌─────────────────────────┬─────────────────────────┐
│ File exists             │ File missing            │
├─────────────────────────┼─────────────────────────┤
│ Load from disk          │ Create default settings │
│ Deserialize JSON        │ Save to disk            │
└─────────────────────────┴─────────────────────────┘
    ↓
Settings available in CurrentSettings
    ↓
SettingsWindow opens (if user requests)
    ↓
LoadSettings() called
    ↓
_updateNestingLevel++ (blocks event handlers)
    ↓
Populate all UI controls from CurrentSettings
    ↓
_updateNestingLevel-- (enables event handlers)
    ↓
Subscribe to service events
```

### User Changes Setting (UI → Service → Disk)

```
User types "5" in MinThicknessTextBox
    ↓
MinThicknessTextBox_TextChanged fires
    ↓
Check: _updateNestingLevel == 0? (YES - user initiated)
    ↓
_updateNestingLevel++
    ↓
Parse value (5.0)
    ↓
Call _appSettings.SetBrushThicknessRange(5.0, 20.0)
    ↓
    ┌──────────────────────────────────┐
    │ AppSettingsService                │
    │ ─────────────────────────────────│
    │ Update _currentSettings.Min = 5.0│
    │ Clamp current thickness if needed│
    │ Serialize to JSON                │
    │ Write to settings.json           │
    │ Raise BrushThicknessRangeChanged │
    └──────────────────────────────────┘
    ↓
Event fires → OnBrushThicknessRangeChanged
    ↓
_updateNestingLevel++ (now = 2)
    ↓
Update MinThicknessTextBox.Text = "5" (no TextChanged because value unchanged)
Update MaxThicknessTextBox.Text = "20"
Update ThicknessSlider.Minimum = 5.0
Update ThicknessSlider.Maximum = 20.0
    ↓
_updateNestingLevel-- (now = 1)
    ↓
Back to MinThicknessTextBox_TextChanged
    ↓
_updateNestingLevel-- (now = 0)
    ↓
✅ Complete - setting saved to disk and UI updated
```

### External Change (Service → UI)

Example: User scrolls mouse wheel while drawing to change thickness

```
OverlayWindow detects mouse wheel
    ↓
Calls _appSettings.SetBrushThickness(5.0)
    ↓
    ┌──────────────────────────────────┐
    │ AppSettingsService                │
    │ ─────────────────────────────────│
    │ Update _currentSettings.Thickness│
    │ Serialize to JSON                │
    │ Write to settings.json           │
    │ Raise BrushThicknessChanged      │
    └──────────────────────────────────┘
    ↓
Event fires → OnBrushThicknessChanged (in SettingsWindow, if open)
    ↓
_updateNestingLevel++
    ↓
Update ThicknessSlider.Value = 5.0
    ↓
ThicknessSlider_ValueChanged fires
    ↓
Check: _updateNestingLevel > 0? (YES - skip)
    ↓
Return early (don't call SetBrushThickness again)
    ↓
_updateNestingLevel--
    ↓
✅ UI updated, no recursion
```

---

## Event Handler Responsibilities

All event handlers in `SettingsWindow` follow this pattern:

### Service Event Handlers
**Purpose**: Update UI when service changes settings (external change or event from own update)

```csharp
private void OnBrushThicknessChanged(object? sender, double thickness)
{
    Dispatcher.Invoke(() =>
    {
        _updateNestingLevel++;
        try
        {
            if (ThicknessSlider != null && ThicknessValueText != null)
            {
                ThicknessSlider.Value = thickness;
                ThicknessValueText.Text = $"{thickness:F0} px";
            }
        }
        finally
        {
            _updateNestingLevel--;
        }
    });
}
```

**Key points:**
- Always wrapped in `Dispatcher.Invoke()` (events may fire from any thread)
- Always increment/decrement nesting level
- Always use try/finally to ensure decrement happens
- Always null-check controls

### UI Control Event Handlers
**Purpose**: Update service when user changes UI controls

```csharp
private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
{
    // Only update settings if this change was NOT triggered by programmatic update
    if (ThicknessValueText != null && _updateNestingLevel == 0)
    {
        double value = e.NewValue;
        ThicknessValueText.Text = $"{value:F0} px";
        _appSettings.SetBrushThickness(value);
    }
}
```

**Key points:**
- Check `_updateNestingLevel == 0` to ensure user-initiated
- Don't increment nesting level (let service event handler do that)
- Null-check controls before accessing

---

## Common Pitfalls & Solutions

### ❌ Pitfall 1: Forgetting to Check Nesting Level
```csharp
// BAD - always updates, causes recursion
private void MinThicknessTextBox_TextChanged(...)
{
    _appSettings.SetBrushThicknessRange(min, max);  // ❌ Infinite loop!
}
```

```csharp
// GOOD - only update if user-initiated
private void MinThicknessTextBox_TextChanged(...)
{
    if (_updateNestingLevel > 0)
        return;  // ✅ Skip programmatic changes

    _appSettings.SetBrushThicknessRange(min, max);
}
```

### ❌ Pitfall 2: Using Boolean Instead of Counter
```csharp
// BAD - nested calls corrupt the flag
private bool _isUpdating = false;

private void Update()
{
    _isUpdating = true;
    CallService();  // Raises event
        // Event handler sets _isUpdating = false ❌
    // Flag is now false even though we're still updating!
    _isUpdating = false;
}
```

```csharp
// GOOD - nesting level handles nested calls correctly
private int _updateNestingLevel = 0;

private void Update()
{
    _updateNestingLevel++;
    CallService();  // Raises event
        _updateNestingLevel++;
        _updateNestingLevel--;  // Still = 1 ✅
    // Still protected
    _updateNestingLevel--;  // Now = 0
}
```

### ❌ Pitfall 3: Missing Try/Finally
```csharp
// BAD - exception leaves nesting level corrupted
_updateNestingLevel++;
UpdateUI();  // Throws exception
_updateNestingLevel--;  // ❌ Never executes!
```

```csharp
// GOOD - always decrements, even on exception
_updateNestingLevel++;
try
{
    UpdateUI();
}
finally
{
    _updateNestingLevel--;  // ✅ Always executes
}
```

### ❌ Pitfall 4: Precision Loss
```csharp
// BAD - casting to int loses decimal precision
MinThicknessTextBox.Text = ((int)settings.MinBrushThickness).ToString();
// If MinBrushThickness = 1.5, displays "1" ❌
```

```csharp
// GOOD - preserves double precision while displaying as integer
MinThicknessTextBox.Text = settings.MinBrushThickness.ToString("F0");
// If MinBrushThickness = 1.5, displays "2" ✅ (rounded)
// If MinBrushThickness = 1.0, displays "1" ✅
```

---

## Testing Considerations

When writing tests for settings-related code:

1. **Test default creation**: Verify defaults are created if settings.json is missing
2. **Test persistence**: Verify changes are saved to disk immediately
3. **Test clamping**: Verify BrushThickness is clamped to min/max range
4. **Test auto-adjust**: Verify BrushThickness adjusts when min/max changes put it out of range
5. **Test events**: Verify all events fire correctly
6. **Test concurrency**: Verify settings are thread-safe (service uses file I/O)

Example test:
```csharp
[Fact]
public void SetBrushThicknessRange_ShouldAdjustCurrentThicknessIfBelowMin()
{
    var service = new AppSettingsService(logger);
    service.SetBrushThickness(3.0);

    // Change range so current thickness (3.0) is below new min (5.0)
    service.SetBrushThicknessRange(5.0, 20.0);

    // Verify it auto-adjusted
    Assert.Equal(5.0, service.CurrentSettings.BrushThickness);
}
```

---

## Adding New Settings

To add a new setting:

### 1. Update `AppSettings.cs`
```csharp
[JsonPropertyName("myNewSetting")]
public string MyNewSetting { get; set; } = "default value";
```

Update `Clone()` method:
```csharp
public AppSettings Clone()
{
    return new AppSettings
    {
        // ... existing properties ...
        MyNewSetting = this.MyNewSetting
    };
}
```

### 2. Update `AppSettingsService.cs`
Add event:
```csharp
public event EventHandler<string>? MyNewSettingChanged;
```

Add setter method:
```csharp
public void SetMyNewSetting(string value)
{
    _logger.LogInformation("Setting MyNewSetting to {Value}", value);
    _currentSettings.MyNewSetting = value;
    SaveSettings(_currentSettings);

    // Raise event to notify UI
    MyNewSettingChanged?.Invoke(this, value);
}
```

### 3. Update `SettingsWindow.xaml.cs`
Subscribe to event in constructor:
```csharp
_appSettings.MyNewSettingChanged += OnMyNewSettingChanged;
```

Unsubscribe in `UnsubscribeFromEvents()`:
```csharp
_appSettings.MyNewSettingChanged -= OnMyNewSettingChanged;
```

Add event handler:
```csharp
private void OnMyNewSettingChanged(object? sender, string value)
{
    Dispatcher.Invoke(() =>
    {
        _updateNestingLevel++;
        try
        {
            if (MyTextBox != null)
            {
                MyTextBox.Text = value;
            }
        }
        finally
        {
            _updateNestingLevel--;
        }
    });
}
```

Add UI control handler:
```csharp
private void MyTextBox_TextChanged(object sender, TextChangedEventArgs e)
{
    if (MyTextBox == null || _updateNestingLevel > 0)
        return;

    _appSettings.SetMyNewSetting(MyTextBox.Text);
}
```

Load in `LoadSettings()`:
```csharp
MyTextBox.Text = settings.MyNewSetting;
```

### 4. Add Tests
```csharp
[Fact]
public void SetMyNewSetting_ShouldPersist()
{
    var service = new AppSettingsService(logger);
    service.SetMyNewSetting("new value");

    var reloaded = new AppSettingsService(logger);
    Assert.Equal("new value", reloaded.CurrentSettings.MyNewSetting);
}
```

---

## File Location

Settings are stored at:
```
%LOCALAPPDATA%\GhostDraw\settings.json
```

On most systems, this resolves to:
```
C:\Users\<username>\AppData\Local\GhostDraw\settings.json
```

Example `settings.json`:
```json
{
  "brushColor": "#FF0000",
  "brushThickness": 3.0,
  "minBrushThickness": 1.0,
  "maxBrushThickness": 20.0,
  "hotkeyModifier1": "Control",
  "hotkeyModifier2": "Alt",
  "hotkeyKey": "D",
  "lockDrawingMode": false,
  "logLevel": "Information",
  "colorPalette": [
    "#FF0000",
    "#00FF00",
    "#0000FF",
    "#FFFF00",
    "#FF00FF",
    "#00FFFF",
    "#FFFFFF",
    "#000000",
    "#FFA500",
    "#800080"
  ]
}
```

---

## Best Practices

1. **Always use the nesting level pattern** when updating UI from events
2. **Always use try/finally** when incrementing nesting level
3. **Always null-check controls** before accessing them
4. **Never call SaveSettings() directly** - use AppSettingsService methods
5. **Use `CurrentSettings` (clone)** to read settings - never modify the returned object
6. **Log all settings changes** at Information level for debugging
7. **Test edge cases**: Empty file, corrupted JSON, invalid values, missing fields
8. **Document any new settings** in this file

---

## Troubleshooting

### Settings not loading at startup
- Check if `LoadSettings()` is called before subscribing to events
- Verify nesting level is incremented during load
- Check Debug output for "LoadSettings START/END" messages

### Infinite recursion / stack overflow
- Verify all UI event handlers check `_updateNestingLevel == 0`
- Verify all service event handlers increment/decrement nesting level
- Check for missing `return` after nesting level check

### Settings not persisting to disk
- Verify you're calling `_appSettings.SetXxx()` methods, not modifying `CurrentSettings` directly
- Check file permissions on `%LOCALAPPDATA%\GhostDraw\`
- Look for exceptions in logs

### UI not updating from external changes
- Verify event subscription in constructor
- Verify event handler uses `Dispatcher.Invoke()`
- Check that event handler increments/decrements nesting level

---

## Related Files

- `src/AppSettings.cs` - Settings data model
- `src/AppSettingsService.cs` - Settings management service
- `src/SettingsWindow.xaml` - Settings UI (XAML)
- `src/SettingsWindow.xaml.cs` - Settings UI logic
- `tests/GhostDraw.Tests/AppSettingsTests.cs` - Settings model tests
- `tests/GhostDraw.Tests/AppSettingsServiceTests.cs` - Service tests
