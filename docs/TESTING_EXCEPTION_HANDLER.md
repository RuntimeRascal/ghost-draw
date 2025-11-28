# Testing the Global Exception Handler

## Quick Test Guide

### Test 1: Normal Operation (Baseline)

**Steps**:
1. Run GhostDraw
2. Press `Ctrl+Alt+D`
3. Draw on screen
4. Release keys
5. Verify drawing clears

**Expected**: ? Normal operation, no errors

---

### Test 2: Force Exception in Drawing Mode

**Steps**:
1. Open `DrawingManager.cs`
2. Add to `EnableDrawing()` method:
   ```csharp
   public void EnableDrawing()
   {
       throw new InvalidOperationException("TEST EXCEPTION - Drawing Mode");
       // ... rest of code
   }
   ```
3. Run GhostDraw
4. Press `Ctrl+Alt+D`

**Expected**:
- ? Error notification appears
- ? Message says "reset to safe state"
- ? Overlay not visible
- ? Keyboard/mouse work normally
- ? Log shows exception and emergency reset

**Cleanup**: Remove the `throw` statement

---

### Test 3: Force Exception in Hook Callback

**Steps**:
1. Open `GlobalKeyboardHook.cs`
2. Add to `HookCallback()` method:
   ```csharp
   private IntPtr HookCallback(...)
   {
       throw new Exception("TEST EXCEPTION - Hook");
       // ... rest of code
   }
   ```
3. Run GhostDraw
4. Press any key

**Expected**:
- ? Exception logged to file
- ? Hook continues working
- ? No user notification (handled internally)
- ? Can still use hotkeys

**Cleanup**: Remove the `throw` statement

---

### Test 4: Verify Emergency Reset

**Steps**:
1. Open `DrawingManager.cs`
2. Add exception that triggers emergency reset:
   ```csharp
   public void EnableDrawing()
   {
       _overlayWindow.Show();
       throw new OutOfMemoryException("TEST OOM");
   }
   ```
3. Run GhostDraw
4. Press `Ctrl+Alt+D`

**Expected**:
- ? Emergency reset triggered
- ? Overlay hidden
- ? Hooks released
- ? Lock mode reset
- ? Log shows "EMERGENCY STATE RESET"

**Cleanup**: Remove the `throw` statement

---

### Test 5: Verify Logging

**Steps**:
1. Run any of the tests above
2. Open log folder:
   - Windows Key + R
   - Type: `%LOCALAPPDATA%\GhostDraw`
   - Open latest `ghostdraw-[date].log`

**Expected to See**:
```
[HH:mm:ss.fff] [CRT] [GlobalExceptionHandler] Unhandled exception on UI thread
System.InvalidOperationException: TEST EXCEPTION
   at GhostDraw.DrawingManager.EnableDrawing()
   
[HH:mm:ss.fff] [WRN] [GlobalExceptionHandler] EMERGENCY STATE RESET initiated. Reason: UI thread exception
[HH:mm:ss.fff] [WRN] [GlobalExceptionHandler] Emergency state reset completed. Actions taken: Drawing mode disabled, Keyboard hooks released
```

---

### Test 6: Verify System Remains Usable

**After any exception test**:

1. ? Open Notepad - type text (keyboard works)
2. ? Click and drag window (mouse works)
3. ? Alt+Tab works (no overlay blocking)
4. ? Right-click system tray icon (app still running)
5. ? Exit and restart GhostDraw (clean restart)

**All should work perfectly!**

---

### Test 7: ESC Key Emergency Exit

**Steps**:
1. Run GhostDraw
2. Press `Ctrl+Alt+D` (drawing mode active)
3. Press `ESC`

**Expected**:
- ? Drawing mode disabled immediately
- ? Overlay hidden
- ? Log shows "ESC pressed - emergency exit"

---

## Automated Testing (Future)

Create `GlobalExceptionHandlerTests.cs`:

```csharp
[Fact]
public void EmergencyStateReset_ShouldDisableDrawingMode()
{
    // Arrange
    var handler = CreateHandler();
    _drawingManager.EnableDrawing();
    
    // Act
    handler.EmergencyStateReset("Test");
    
    // Assert
    Assert.False(_drawingManager.IsDrawingMode);
}

[Fact]
public void HandleException_CriticalContext_TriggersReset()
{
    // Arrange
    var handler = CreateHandler();
    var ex = new Exception("Test");
    
    // Act
    handler.HandleException(ex, "hook callback");
    
    // Assert
    // Verify emergency reset was called
}
```

---

## Success Criteria

### ? All Tests Pass When:

1. **Exceptions are caught** - No unhandled crashes
2. **Emergency reset works** - System remains usable
3. **Logging is comprehensive** - Can diagnose issues
4. **User is notified** - Clear error messages
5. **Recovery is automatic** - No manual intervention needed

### ? Tests Fail If:

1. Application crashes completely
2. Keyboard/mouse stop working
3. Overlay remains visible after error
4. No error notification shown
5. Logs don't show exception details

---

## Regression Testing

After any code changes, verify:

1. Normal drawing still works
2. Hotkeys still work
3. Settings still save
4. Logs still created
5. Exception handler still active

Run these 5 quick tests:
```
1. Draw ? Clear (normal operation)
2. Open Settings ? Change color ? Save
3. Check logs folder exists
4. Force exception ? Verify recovery
5. Restart app ? Verify clean start
```

---

## Production Readiness Checklist

Before release:

- [ ] Remove all `throw new Exception("TEST")` statements
- [ ] Test on clean Windows install
- [ ] Test on different Windows versions (10, 11)
- [ ] Test with multiple monitors
- [ ] Test with high DPI displays
- [ ] Verify logs don't contain sensitive data
- [ ] Check log file size limits working
- [ ] Test rapid hotkey presses
- [ ] Test while system is under load
- [ ] Verify no memory leaks after exceptions

---

## Troubleshooting

### "Exception handler not registered"

**Check**: `App.xaml.cs` - verify handler registration in `OnStartup()`

### "Emergency reset not triggered"

**Check**: `IsSystemSafetyCritical()` - verify context detection

### "Logs not created"

**Check**: `%LOCALAPPDATA%\GhostDraw` folder permissions

### "Notification not shown"

**Check**: Dispatcher access in `ShowErrorNotification()`

---

## Tips

### Quick Log Check

```powershell
# Open latest log
notepad "$env:LOCALAPPDATA\GhostDraw\ghostdraw-$(Get-Date -Format 'yyyyMMdd').log"
```

### Force Garbage Collection (Memory Test)

```csharp
// In test code
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
```

### Monitor Hook State

```csharp
// In GlobalKeyboardHook
_logger.LogInformation("Hook ID = {HookId}, Is Valid = {IsValid}", 
    _hookID, _hookID != IntPtr.Zero);
```

---

## Next Steps After Testing

1. **Remove test exceptions** from code
2. **Document any issues** found
3. **Add automated tests** for regressions
4. **Deploy to production** with confidence! ??
