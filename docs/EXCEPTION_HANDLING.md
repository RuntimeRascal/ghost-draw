# GhostDraw Exception Handling & System Safety

## Overview

GhostDraw implements a comprehensive global exception handling system to prevent crashes and ensure the user's system remains usable even when errors occur. This is **critical** for an application that intercepts global keyboard/mouse input, as failures could lock the user out of their system.

## The Problem

When GhostDraw crashes while:
- Drawing mode is active (mouse captured)
- Keyboard hooks are installed
- Overlay window is visible

The user could experience:
- ? Inability to use mouse
- ? Keyboard input not working properly
- ? Screen blocked by transparent overlay
- ? No way to regain control without forced restart

## The Solution

### Global Exception Handler

The `GlobalExceptionHandler` service provides:

1. **Automatic Exception Capture**
   - UI thread exceptions (Dispatcher)
   - Background thread exceptions
   - Unobserved task exceptions

2. **Emergency State Reset**
   - Disables drawing mode
   - Releases all keyboard hooks
   - Hides overlay window
   - Resets lock mode
   - Ensures system remains usable

3. **User Notification**
   - Friendly error messages
   - Clear explanation of what happened
   - Information about recovery actions taken

## Architecture

### Exception Handler Registration

```csharp
// In App.xaml.cs OnStartup()
_exceptionHandler = _serviceProvider.GetRequiredService<GlobalExceptionHandler>();
_exceptionHandler.RegisterHandlers();
```

### Handlers Registered

```csharp
// UI Thread
Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

// Background Threads  
AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

// Task Exceptions
TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
```

### Emergency State Reset Process

1. **Disable Drawing Mode**
   ```csharp
   if (_drawingManager.IsDrawingMode)
   {
       _drawingManager.DisableDrawingMode();
   }
   ```

2. **Release Keyboard Hooks**
   ```csharp
   _keyboardHook.Dispose(); // Unhooks immediately
   ```

3. **Reset Lock Mode**
   ```csharp
   if (currentSettings.LockDrawingMode)
   {
       _settingsService.SetLockDrawingMode(false);
   }
   ```

4. **Hide Overlay**
   ```csharp
   foreach (Window window in Application.Current.Windows)
   {
       if (window is OverlayWindow overlay)
       {
           overlay.Hide();
       }
   }
   ```

## Protected Critical Paths

### 1. Keyboard Hook Callbacks

**Location**: `GlobalKeyboardHook.cs` - `HookCallback()`

**Protection**:
```csharp
private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
    try
    {
        // Hook logic here
    }
    catch (Exception ex)
    {
        // CRITICAL: Never throw from hook callback
        _logger.LogError(ex, "Exception in keyboard hook callback");
    }
    
    // MUST ALWAYS call CallNextHookEx
    return CallNextHookEx(_hookID, nCode, wParam, lParam);
}
```

**Why**: Hook callbacks **must never throw** - exceptions would crash the hook chain and potentially the entire system.

### 2. Drawing Manager Methods

**Location**: `DrawingManager.cs` - All public methods

**Protection**:
```csharp
public void EnableDrawing()
{
    try
    {
        // Enable logic
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to enable drawing mode");
        // Cleanup attempt
        _overlayWindow.DisableDrawing();
        _overlayWindow.Hide();
        throw; // Re-throw for GlobalExceptionHandler
    }
}
```

**Why**: Drawing mode failures need cleanup AND global handling.

### 3. App Event Handlers

**Location**: `App.xaml.cs` - Hotkey events

**Protection**:
```csharp
private void OnHotkeyPressed(object? sender, EventArgs e)
{
    try
    {
        _drawingManager?.EnableDrawing();
    }
    catch (Exception ex)
    {
        _exceptionHandler?.HandleException(ex, "Hotkey pressed handler");
    }
}
```

**Why**: User-triggered events need consistent error handling.

## Exception Severity Levels

### Critical - Emergency Reset Required

Triggers full emergency state reset:

- **Context**: Hook callbacks, overlay, input capture
- **Exception Types**: `OutOfMemoryException`, `ExternalException`
- **Action**: Full emergency reset + user notification

```csharp
if (IsSystemSafetyCritical(exception, context))
{
    EmergencyStateReset($"Critical exception in {context}");
    ShowErrorNotification(exception);
}
```

### Error - Log and Continue

Logged but doesn't trigger reset:

- **Context**: Settings, UI operations
- **Action**: Log error, continue execution

```csharp
_logger.LogError(ex, "Non-critical error in {Context}", context);
```

### Warning - Expected Failures

Known issues that don't require intervention:

- **Context**: Network timeouts, file not found
- **Action**: Log warning only

## User Experience

### Error Notification

When an exception occurs:

```
??????????????????????????????????????????????
?      GhostDraw - Error Recovery            ?
??????????????????????????????????????????????
? GhostDraw encountered an unexpected error  ?
? and has reset to a safe state.             ?
?                                            ?
? • Drawing mode has been disabled           ?
? • All keyboard hooks have been released    ?
? • Your system should be fully responsive   ?
?                                            ?
? Error: [Exception Message]                 ?
?                                            ?
? You can continue using GhostDraw, or       ?
? restart the application for a fresh start. ?
??????????????????????????????????????????????
            [ OK ]
```

### What the User Should See

? **Before Exception Handler**:
- Application crashes
- Overlay might remain visible
- Hooks might remain active
- System potentially unusable

? **After Exception Handler**:
- Error notification shown
- Drawing mode disabled
- Hooks released
- System fully responsive
- Application continues running (or exits cleanly)

## Testing Exception Handling

### Manual Testing

1. **Force Exception in Drawing Mode**
   ```csharp
   // In DrawingManager.EnableDrawing()
   throw new InvalidOperationException("Test exception");
   ```
   
   Expected: Emergency reset, notification shown, system usable

2. **Force Exception in Hook Callback**
   ```csharp
   // In GlobalKeyboardHook.HookCallback()
   throw new Exception("Test hook exception");
   ```
   
   Expected: Exception logged, hook continues working

3. **Out of Memory Simulation**
   ```csharp
   // Allocate large arrays to trigger OOM
   ```
   
   Expected: Emergency reset, critical error logged

### Automated Testing

See `GlobalExceptionHandlerTests.cs` for:
- Emergency reset functionality
- Handler registration/unregistration
- Critical exception detection
- Notification system

## Logging

### Exception Logging Format

```
[HH:mm:ss.fff] [CRT] [GlobalExceptionHandler] Unhandled exception on UI thread
System.InvalidOperationException: Test error
   at GhostDraw.DrawingManager.EnableDrawing()
   
[HH:mm:ss.fff] [WRN] [GlobalExceptionHandler] EMERGENCY STATE RESET initiated. Reason: UI thread exception

[HH:mm:ss.fff] [WRN] [GlobalExceptionHandler] Emergency state reset completed. Actions taken: Drawing mode disabled, Keyboard hooks released, Overlay window hidden
```

### Log Levels

- **Critical**: Unhandled exceptions, emergency reset failures
- **Error**: Exceptions in non-critical paths
- **Warning**: Emergency reset actions, state changes
- **Information**: Normal state transitions
- **Debug**: Detailed execution flow
- **Trace**: Hook callback details

## Best Practices

### DO ?

1. **Always wrap critical code in try-catch**
   ```csharp
   try
   {
       // Critical operation
   }
   catch (Exception ex)
   {
       _exceptionHandler.HandleException(ex, "operation name");
   }
   ```

2. **Never throw from hook callbacks**
   ```csharp
   private IntPtr HookCallback(...)
   {
       try { /* ... */ }
       catch { /* Log and continue */ }
       return CallNextHookEx(...); // MUST always call
   }
   ```

3. **Log all exceptions with context**
   ```csharp
   _logger.LogError(ex, "Failed to {Operation} in {Context}", operation, context);
   ```

4. **Test exception paths**
   - Force exceptions in development
   - Verify emergency reset works
   - Check system remains usable

### DON'T ?

1. **Don't ignore exceptions**
   ```csharp
   catch { } // BAD - hides problems
   ```

2. **Don't throw from Dispose/Cleanup**
   ```csharp
   public void Dispose()
   {
       try { /* cleanup */ }
       catch { /* Log but don't throw */ }
   }
   ```

3. **Don't block in exception handlers**
   ```csharp
   catch (Exception ex)
   {
       Thread.Sleep(1000); // BAD
       _logger.LogError(ex);
   }
   ```

4. **Don't assume cleanup succeeded**
   ```csharp
   try { Cleanup(); }
   catch { /* Cleanup might fail! */ }
   ```

## Emergency Recovery

### For Users

If GhostDraw becomes unresponsive:

1. **Press ESC** - Emergency exit from drawing mode
2. **Wait 2 seconds** - Allow exception handler to work
3. **Check notification** - Read recovery message
4. **Restart if needed** - Exit from system tray if unstable

### For Developers

If testing exception handling:

1. **Check logs** - `%LOCALAPPDATA%\GhostDraw\ghostdraw-[date].log`
2. **Verify state** - Hooks released, overlay hidden
3. **Test mouse/keyboard** - Should work normally
4. **Read exception details** - Stack trace in logs

## Windows Event Log Integration

For critical failures that prevent normal logging:

```csharp
// Last resort logging
System.Diagnostics.EventLog.WriteEntry("GhostDraw", 
    $"CRITICAL FAILURE: {ex.Message}", 
    EventLogEntryType.Error);
```

View in Windows Event Viewer:
- Event Viewer ? Windows Logs ? Application
- Source: "GhostDraw"
- Level: Error

## Future Improvements

1. **Crash Reporting**
   - Automatic error reports (with user consent)
   - Anonymous crash statistics
   - Pattern detection

2. **Recovery Options**
   - "Safe Mode" with minimal features
   - Automatic restart after crash
   - State persistence across crashes

3. **Enhanced Diagnostics**
   - Memory usage monitoring
   - Performance metrics
   - Health checks

4. **User Control**
   - Option to disable exception recovery
   - Custom recovery actions
   - Error report viewing

## Conclusion

The global exception handler is a **critical safety system** that:

? **Prevents system lockout**
? **Provides clear user feedback**
? **Enables crash recovery**
? **Maintains application stability**
? **Logs comprehensive diagnostics**

This ensures GhostDraw is safe and reliable even when unexpected errors occur.

---

**Safety First**: When in doubt, trigger emergency reset. It's better to interrupt drawing than to lock out the user's system. ???
