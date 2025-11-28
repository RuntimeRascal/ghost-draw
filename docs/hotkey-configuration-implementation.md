# Hotkey Configuration Implementation Plan

## ?? Overview

This document outlines the implementation strategy for making the hotkey combination configurable in GhostDraw. The approach uses a **key recorder** with Windows API for localized key names.

---

## ?? Goals

- ? Allow users to customize the hotkey combination (currently hardcoded as `Ctrl+Alt+D`)
- ? Support 1-3 modifiers + 1 regular key
- ? Persist configuration across app restarts
- ? Provide intuitive UI for recording key combinations
- ? Validate combinations to prevent conflicts with OS hotkeys
- ? Apply changes immediately without requiring app restart

---

## ??? Architecture

### **Data Flow**

```
User clicks "RECORD NEW HOTKEY"
    ?
Create temporary GlobalKeyboardHook (recorder mode)
    ?
Listen for KeyPressed events, collect VK codes
    ?
User releases all keys
    ?
Validate combination (has modifiers, not reserved)
    ?
Display preview using GetKeyNameText API
    ?
User clicks "APPLY"
    ?
Save List<int> to AppSettings (settings.json)
    ?
Main GlobalKeyboardHook.Configure(vkList)
    ?
Hook now detects new combination ?
```

### **Components**

#### **1. VirtualKeyHelper (New Utility Class)**
- Uses Windows `GetKeyNameText` P/Invoke for localized key names
- Converts VK codes to user-friendly display strings
- Validates modifier keys
- Handles special cases and extended keys

#### **2. GlobalKeyboardHook (Refactored)**
- Add `KeyPressed` and `KeyReleased` events that expose raw VK codes
- Replace hardcoded VK constants with configurable `List<int>`
- Add `Configure(List<int> virtualKeys)` method
- Use dictionary for key state tracking instead of individual booleans

#### **3. AppSettings (Updated)**
- Replace string-based properties with `List<int> HotkeyVirtualKeys`
- Add computed property `HotkeyDisplayName` for UI display
- Remove `HotkeyModifier1`, `HotkeyModifier2`, `HotkeyKey` (breaking change)

#### **4. AppSettingsService (Updated)**
- Add `SetHotkey(List<int> virtualKeys)` method
- Add `HotkeyChanged` event
- Implement validation logic

#### **5. SettingsWindow (New UI)**
- Add recorder UI with recording/preview/apply flow
- Temporary hook for capturing key combinations
- Real-time preview during recording
- Validation and warning messages

---

## ?? Implementation Steps

### **Phase 1: Foundation (VirtualKeyHelper)**

**File**: `VirtualKeyHelper.cs` (new)

**Tasks**:
1. Create static utility class in root namespace
2. Add P/Invoke declarations for `GetKeyNameText` and `MapVirtualKey`
3. Implement `GetFriendlyName(int vkCode)` method
4. Implement `GetCombinationDisplayName(List<int> virtualKeys)` method
5. Implement `IsModifierKey(int vkCode)` helper
6. Handle special cases and extended keys
7. Add unit tests

**Dependencies**: None

---

### **Phase 2: Data Model Updates**

#### **2.1 Update AppSettings**

**File**: `AppSettings.cs`

**Changes**:
```csharp
// REMOVE these properties
public string HotkeyModifier1 { get; set; } = "Control";
public string HotkeyModifier2 { get; set; } = "Alt";
public string HotkeyKey { get; set; } = "D";

// ADD this property
[JsonPropertyName("hotkeyVirtualKeys")]
public List<int> HotkeyVirtualKeys { get; set; } = new() { 0xA2, 0xA4, 0x44 };

// ADD computed property
[JsonIgnore]
public string HotkeyDisplayName => VirtualKeyHelper.GetCombinationDisplayName(HotkeyVirtualKeys);
```

**Migration Strategy**:
- Add migration code in `AppSettingsService.LoadSettings()` to convert old format to new
- If old properties exist, convert to VK codes: "Control" ? 0xA2, "Alt" ? 0xA4, "D" ? 0x44
- Save in new format

#### **2.2 Update AppSettingsService**

**File**: `Services/AppSettingsService.cs`

**Changes**:
```csharp
// ADD event
public event EventHandler<List<int>>? HotkeyChanged;

// ADD method
public void SetHotkey(List<int> virtualKeys)
{
    _logger.LogInformation("Setting hotkey to VKs: {VKs}", string.Join(", ", virtualKeys));
    _currentSettings.HotkeyVirtualKeys = new List<int>(virtualKeys);
    SaveSettings(_currentSettings);
    HotkeyChanged?.Invoke(this, virtualKeys);
}

// UPDATE LoadSettings() to handle migration
private AppSettings LoadSettings()
{
    // ... existing load logic
    
    // Migration: Convert old string-based hotkeys to VK codes
    if (settings.HotkeyModifier1 != null && settings.HotkeyVirtualKeys.Count == 0)
    {
        settings.HotkeyVirtualKeys = ConvertLegacyHotkey(
            settings.HotkeyModifier1,
            settings.HotkeyModifier2,
            settings.HotkeyKey
        );
        SaveSettings(settings); // Persist new format
    }
    
    return settings;
}
```

---

### **Phase 3: Refactor GlobalKeyboardHook**

**File**: `GlobalKeyboardHook.cs`

**Changes**:

#### **3.1 Add New Events**
```csharp
// ADD: Raw key events for recorder
public event EventHandler<KeyEventArgs>? KeyPressed;
public event EventHandler<KeyEventArgs>? KeyReleased;

// NEW: Event args class
public class KeyEventArgs : EventArgs
{
    public int VirtualKeyCode { get; }
    public KeyEventArgs(int vkCode) => VirtualKeyCode = vkCode;
}
```

#### **3.2 Replace Hardcoded VK Constants**
```csharp
// REMOVE these constants
private const int VK_LCONTROL = 0xA2;
private const int VK_RCONTROL = 0xA3;
private const int VK_LMENU = 0xA4;
private const int VK_RMENU = 0xA5;
private const int VK_D = 0x44;

// KEEP this (emergency exit)
private const int VK_ESCAPE = 0x1B;
```

#### **3.3 Add Configuration Support**
```csharp
// ADD: Configurable hotkey VKs
private List<int> _hotkeyVKs = new() { 0xA2, 0xA4, 0x44 };  // Default: Ctrl+Alt+D
private Dictionary<int, bool> _keyStates = new();

// ADD: Configuration method
public void Configure(List<int> virtualKeys)
{
    _hotkeyVKs = new List<int>(virtualKeys);
    _keyStates.Clear();
    
    foreach (var vk in virtualKeys)
        _keyStates[vk] = false;
    
    _logger.LogInformation("Hotkey reconfigured to: {DisplayName}", 
        VirtualKeyHelper.GetCombinationDisplayName(virtualKeys));
}
```

#### **3.4 Update HookCallback Logic**
```csharp
private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
    try
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN;
            
            // Fire raw key events (for recorder)
            if (isKeyDown)
                KeyPressed?.Invoke(this, new KeyEventArgs(vkCode));
            else
                KeyReleased?.Invoke(this, new KeyEventArgs(vkCode));
            
            // Emergency ESC exit
            if (vkCode == VK_ESCAPE && isKeyDown)
            {
                _logger.LogInformation("?? ESC pressed - emergency exit");
                EscapePressed?.Invoke(this, EventArgs.Empty);
            }
            
            // Track hotkey state
            if (_hotkeyVKs.Contains(vkCode))
            {
                _keyStates[vkCode] = isKeyDown;
                
                // Check if ALL hotkey keys are pressed
                bool allPressed = _hotkeyVKs.All(vk => _keyStates[vk]);
                
                if (allPressed && !_wasHotkeyActive)
                {
                    _logger.LogInformation("?? HOTKEY PRESSED");
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                else if (!allPressed && _wasHotkeyActive)
                {
                    _logger.LogInformation("?? HOTKEY RELEASED");
                    HotkeyReleased?.Invoke(this, EventArgs.Empty);
                }
                
                _wasHotkeyActive = allPressed;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Exception in keyboard hook callback");
    }
    
    return CallNextHookEx(_hookID, nCode, wParam, lParam);
}
```

---

### **Phase 4: Settings Window UI**

**File**: `SettingsWindow.xaml`

**Changes**:

#### **4.1 Update HOTKEY GroupBox**
```xaml
<GroupBox Header="// HOTKEY">
    <StackPanel>
        <!-- Current Hotkey Display -->
        <Border Background="#1A1A2E"
                BorderBrush="#00FFFF"
                BorderThickness="2"
                Padding="16,12"
                Margin="0,0,0,16">
            <Border.Effect>
                <DropShadowEffect Color="#00FFFF" 
                                Opacity="0.5" 
                                BlurRadius="12" 
                                ShadowDepth="0"/>
            </Border.Effect>
            <StackPanel>
                <TextBlock Text="CURRENT HOTKEY:"
                          Foreground="#808080"
                          FontSize="10"
                          FontFamily="Consolas"
                          Margin="0,0,0,4"/>
                <TextBlock x:Name="CurrentHotkeyText"
                          Text="Control + Alt + D"
                          Foreground="#00FFFF"
                          FontSize="16"
                          FontWeight="Bold"
                          FontFamily="Consolas"/>
            </StackPanel>
        </Border>
        
        <!-- Recorder Box (hidden by default) -->
        <Border x:Name="RecorderBox"
                Background="#0A0E27"
                BorderBrush="#FF0080"
                BorderThickness="2"
                Padding="20"
                Margin="0,0,0,16"
                Visibility="Collapsed">
            <Border.Effect>
                <DropShadowEffect Color="#FF0080" 
                                Opacity="0.8" 
                                BlurRadius="20" 
                                ShadowDepth="0"/>
            </Border.Effect>
            <StackPanel>
                <TextBlock x:Name="RecorderStatusText"
                          Text="?? RECORDING... Press your hotkey combination"
                          Foreground="#FF0080"
                          FontSize="14"
                          FontWeight="Bold"
                          FontFamily="Consolas"
                          TextAlignment="Center"
                          Margin="0,0,0,12"/>
                <TextBlock x:Name="RecorderPreviewText"
                          Text="Waiting for keys..."
                          Foreground="#00FFFF"
                          FontSize="18"
                          FontWeight="Bold"
                          FontFamily="Consolas"
                          TextAlignment="Center"
                          MinHeight="30"/>
                <TextBlock Text="// Press ESC to cancel"
                          Foreground="#606060"
                          FontSize="10"
                          FontFamily="Consolas"
                          TextAlignment="Center"
                          Margin="0,8,0,0"/>
            </StackPanel>
        </Border>
        
        <!-- Buttons -->
        <StackPanel Orientation="Horizontal" 
                   HorizontalAlignment="Center">
            <Button x:Name="RecordButton"
                   Content="?? RECORD NEW HOTKEY"
                   Style="{StaticResource ModernButton}"
                   Click="RecordButton_Click"
                   Margin="0,0,12,0"/>
            <Button x:Name="CancelRecordButton"
                   Content="CANCEL"
                   Visibility="Collapsed"
                   Click="CancelRecord_Click"
                   Background="#1A1A2E"
                   Foreground="#808080"
                   BorderBrush="#404040"
                   BorderThickness="1"
                   Padding="20,12"
                   FontSize="12"
                   FontWeight="Bold"
                   FontFamily="Consolas"
                   Cursor="Hand"
                   Margin="0,0,12,0"/>
            <Button x:Name="ApplyHotkeyButton"
                   Content="? APPLY"
                   Visibility="Collapsed"
                   Click="ApplyHotkey_Click"
                   Style="{StaticResource ModernButton}"/>
        </StackPanel>
    </StackPanel>
</GroupBox>
```

---

### **Phase 5: Settings Window Code-Behind**

**File**: `SettingsWindow.xaml.cs`

**Changes**:

#### **5.1 Add Recording State**
```csharp
private bool _isRecording = false;
private HashSet<int> _recordedKeys = new();
private GlobalKeyboardHook? _recorderHook;
```

#### **5.2 Add Recording Logic**
```csharp
private void RecordButton_Click(object sender, RoutedEventArgs e)
{
    StartRecording();
}

private void StartRecording()
{
    _isRecording = true;
    _recordedKeys.Clear();
    
    // Show recorder UI
    RecorderBox.Visibility = Visibility.Visible;
    RecordButton.Visibility = Visibility.Collapsed;
    CancelRecordButton.Visibility = Visibility.Visible;
    RecorderStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0080"));
    RecorderStatusText.Text = "?? RECORDING... Press your hotkey combination";
    RecorderPreviewText.Text = "Waiting for keys...";
    
    // Create temporary hook for recording
    _recorderHook = new GlobalKeyboardHook(_logger);
    _recorderHook.KeyPressed += OnRecorderKeyPressed;
    _recorderHook.KeyReleased += OnRecorderKeyReleased;
    _recorderHook.EscapePressed += OnRecorderEscape;
    _recorderHook.Start();
    
    _logger.LogInformation("Started hotkey recording");
}

private void OnRecorderKeyPressed(object? sender, GlobalKeyboardHook.KeyEventArgs e)
{
    _recordedKeys.Add(e.VirtualKeyCode);
    RecorderPreviewText.Text = VirtualKeyHelper.GetCombinationDisplayName(_recordedKeys.ToList());
    _logger.LogDebug("Recorded key: VK {VK}", e.VirtualKeyCode);
}

private void OnRecorderKeyReleased(object? sender, GlobalKeyboardHook.KeyEventArgs e)
{
    // When user releases ALL keys, we have the complete combination
    if (_recordedKeys.Count > 0 && !IsAnyKeyPressed())
    {
        StopRecording(accepted: true);
    }
}

private void OnRecorderEscape(object? sender, EventArgs e)
{
    StopRecording(accepted: false);
}

private void StopRecording(bool accepted)
{
    _isRecording = false;
    
    // Cleanup recorder hook
    _recorderHook?.Stop();
    _recorderHook?.Dispose();
    _recorderHook = null;
    
    if (accepted && ValidateRecordedKeys())
    {
        // Show Apply button
        ApplyHotkeyButton.Visibility = Visibility.Visible;
        RecordButton.Content = "?? RECORD AGAIN";
        RecorderStatusText.Text = "? Hotkey captured! Click APPLY to save";
        RecorderStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
    }
    else
    {
        // Cancel - hide recorder
        RecorderBox.Visibility = Visibility.Collapsed;
        RecordButton.Visibility = Visibility.Visible;
        RecordButton.Content = "?? RECORD NEW HOTKEY";
        ApplyHotkeyButton.Visibility = Visibility.Collapsed;
    }
    
    CancelRecordButton.Visibility = Visibility.Collapsed;
}

private bool ValidateRecordedKeys()
{
    // Must have at least 2 keys
    if (_recordedKeys.Count < 2)
    {
        MessageBox.Show(
            "Hotkey must include at least one modifier (Ctrl, Alt, Shift, Win) and one regular key.",
            "Invalid Hotkey", 
            MessageBoxButton.OK, 
            MessageBoxImage.Warning);
        return false;
    }
    
    // Must have at least one modifier
    if (!_recordedKeys.Any(vk => VirtualKeyHelper.IsModifierKey(vk)))
    {
        MessageBox.Show(
            "Hotkey must include at least one modifier key (Ctrl, Alt, Shift, Win).",
            "Invalid Hotkey", 
            MessageBoxButton.OK, 
            MessageBoxImage.Warning);
        return false;
    }
    
    // Check for reserved combinations
    if (IsReservedCombo(_recordedKeys.ToList()))
    {
        var result = MessageBox.Show(
            $"?? {VirtualKeyHelper.GetCombinationDisplayName(_recordedKeys.ToList())} is a system hotkey that may not work reliably.\n\nDo you want to use it anyway?",
            "System Hotkey Warning", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Warning);
        
        return result == MessageBoxResult.Yes;
    }
    
    return true;
}

private bool IsReservedCombo(List<int> vks)
{
    // Check common reserved combinations
    var hasCtrl = vks.Any(vk => vk is 0xA2 or 0xA3);
    var hasAlt = vks.Any(vk => vk is 0xA4 or 0xA5);
    var hasWin = vks.Any(vk => vk is 0x5B or 0x5C);
    
    // Ctrl+Alt+Delete
    if (hasCtrl && hasAlt && vks.Contains(0x2E))
        return true;
    
    // Win+L
    if (hasWin && vks.Contains(0x4C))
        return true;
    
    // Alt+F4
    if (hasAlt && vks.Contains(0x73))
        return true;
    
    // Alt+Tab
    if (hasAlt && vks.Contains(0x09))
        return true;
    
    return false;
}

private bool IsAnyKeyPressed()
{
    foreach (var vk in _recordedKeys)
    {
        if ((GetAsyncKeyState(vk) & 0x8000) != 0)
            return true;
    }
    return false;
}

[DllImport("user32.dll")]
private static extern short GetAsyncKeyState(int vKey);

private void ApplyHotkey_Click(object sender, RoutedEventArgs e)
{
    // Save to settings
    _appSettings.SetHotkey(_recordedKeys.ToList());
    
    // Update display
    CurrentHotkeyText.Text = RecorderPreviewText.Text;
    
    // Reset UI
    RecorderBox.Visibility = Visibility.Collapsed;
    RecordButton.Visibility = Visibility.Visible;
    RecordButton.Content = "?? RECORD NEW HOTKEY";
    ApplyHotkeyButton.Visibility = Visibility.Collapsed;
    
    MessageBox.Show(
        "? Hotkey updated successfully!\n\nChanges take effect immediately.",
        "Success", 
        MessageBoxButton.OK, 
        MessageBoxImage.Information);
}

private void CancelRecord_Click(object sender, RoutedEventArgs e)
{
    StopRecording(accepted: false);
}
```

#### **5.3 Update LoadSettings**
```csharp
private void LoadSettings()
{
    _isUpdatingFromEvent = true;
    try
    {
        var settings = _appSettings.CurrentSettings;
        
        // ... existing code ...
        
        // Load hotkey display
        CurrentHotkeyText.Text = settings.HotkeyDisplayName;
        
        // ... rest of loading code ...
    }
    finally
    {
        _isUpdatingFromEvent = false;
    }
}
```

---

### **Phase 6: Wire Up in ServiceConfiguration**

**File**: `ServiceConfiguration.cs`

**Changes**:
```csharp
public static ServiceProvider ConfigureServices()
{
    // ... existing setup ...
    
    _serviceProvider = services.BuildServiceProvider();
    
    // Load saved hotkey configuration
    var appSettings = _serviceProvider.GetRequiredService<AppSettingsService>();
    var keyboardHook = _serviceProvider.GetRequiredService<GlobalKeyboardHook>();
    
    // Configure hotkey from settings
    keyboardHook.Configure(appSettings.CurrentSettings.HotkeyVirtualKeys);
    
    // Subscribe to hotkey changes
    appSettings.HotkeyChanged += (sender, vks) =>
    {
        _configLogger?.LogInformation("Hotkey configuration changed, reconfiguring hook");
        keyboardHook.Configure(vks);
    };
    
    // ... rest of setup ...
}
```

---

## ?? Testing Strategy

### **Unit Tests**

1. **VirtualKeyHelper**
   - Test `GetFriendlyName()` for common keys
   - Test `GetCombinationDisplayName()` with various combinations
   - Test `IsModifierKey()` for all modifier types
   - Test edge cases (invalid VK codes, empty lists)

2. **GlobalKeyboardHook**
   - Test `Configure()` updates internal state
   - Test hotkey detection with different combinations
   - Test that ESC still works as emergency exit
   - Test that KeyPressed/KeyReleased events fire correctly

3. **AppSettingsService**
   - Test `SetHotkey()` persists to JSON
   - Test `HotkeyChanged` event fires
   - Test migration from old format to new format

### **Integration Tests**

1. Test full recording flow:
   - Start recording
   - Press keys
   - Release keys
   - Validate
   - Apply
   - Verify persistence

2. Test hotkey detection after reconfiguration:
   - Change hotkey
   - Verify old hotkey no longer works
   - Verify new hotkey triggers drawing mode

3. Test validation:
   - Try single key (should fail)
   - Try no modifiers (should fail)
   - Try reserved combo (should warn)

### **Manual Testing Scenarios**

- [ ] Record simple combo (e.g., Ctrl+K)
- [ ] Record complex combo (e.g., Ctrl+Alt+Shift+F)
- [ ] Record with numpad keys
- [ ] Record with function keys (F1-F12)
- [ ] Test on different keyboard layouts
- [ ] Test persistence across app restarts
- [ ] Test immediate effect (no restart needed)
- [ ] Test ESC cancellation during recording
- [ ] Test validation messages for invalid combos
- [ ] Test reserved combo warnings

---

## ?? Edge Cases & Error Handling

### **Edge Cases**

1. **User presses only modifiers** ? Validation fails
2. **User presses Escape** ? Gets reserved for emergency exit
3. **User holds keys for long time** ? Should still work
4. **User presses left AND right Ctrl** ? Treated as single "Ctrl" modifier
5. **International keyboards** ? GetKeyNameText handles localization
6. **Rapid key presses** ? Use HashSet to collect unique VKs
7. **Recording interrupted** ? Cleanup hook properly

### **Error Handling**

```csharp
// In recording logic
try
{
    _recorderHook.Start();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to start recorder hook");
    MessageBox.Show(
        "Failed to start key recording. Please try again.",
        "Error", 
        MessageBoxButton.OK, 
        MessageBoxImage.Error);
    StopRecording(accepted: false);
}

// In configuration
try
{
    keyboardHook.Configure(virtualKeys);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to configure hotkey");
    // Fallback to default
    keyboardHook.Configure(new List<int> { 0xA2, 0xA4, 0x44 });
}
```

---

## ?? Migration Strategy

### **Old Format (Deprecated)**
```json
{
  "hotkeyModifier1": "Control",
  "hotkeyModifier2": "Alt",
  "hotkeyKey": "D"
}
```

### **New Format**
```json
{
  "hotkeyVirtualKeys": [162, 164, 68]
}
```

### **Migration Code**
```csharp
private List<int> ConvertLegacyHotkey(string mod1, string mod2, string key)
{
    var vks = new List<int>();
    
    // Map modifiers
    if (mod1 == "Control") vks.Add(0xA2);
    if (mod1 == "Alt") vks.Add(0xA4);
    if (mod1 == "Shift") vks.Add(0xA0);
    if (mod1 == "Win") vks.Add(0x5B);
    
    if (mod2 == "Control") vks.Add(0xA2);
    if (mod2 == "Alt") vks.Add(0xA4);
    if (mod2 == "Shift") vks.Add(0xA0);
    if (mod2 == "Win") vks.Add(0x5B);
    
    // Map key
    if (key.Length == 1)
        vks.Add(key[0]);
    
    return vks;
}
```

---

## ? Success Criteria

- [ ] VirtualKeyHelper created with Windows API integration
- [ ] GlobalKeyboardHook refactored to use dynamic configuration
- [ ] AppSettings updated to store VK codes
- [ ] Settings UI shows recording interface
- [ ] Recording captures key combinations correctly
- [ ] Validation prevents invalid combinations
- [ ] Settings persist to JSON
- [ ] Hotkey changes apply immediately
- [ ] All unit tests pass
- [ ] Manual testing scenarios completed
- [ ] TODO.md updated with checkbox marked
- [ ] Documentation updated

---

## ?? References

- [Windows Virtual Key Codes](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes)
- [GetKeyNameText API](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeynametext)
- [MapVirtualKey API](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-mapvirtualkeya)
- [Low-Level Keyboard Hook](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc)

---

## ?? Implementation Timeline

**Estimated Time**: 4-6 hours

1. **Phase 1** - VirtualKeyHelper (30 min)
2. **Phase 2** - Data Model Updates (30 min)
3. **Phase 3** - GlobalKeyboardHook Refactor (1 hour)
4. **Phase 4** - Settings UI (1 hour)
5. **Phase 5** - Code-Behind Logic (1 hour)
6. **Phase 6** - Wiring & Integration (30 min)
7. **Testing & Validation** (1-2 hours)

---

*Last Updated: 2025-01-28*
