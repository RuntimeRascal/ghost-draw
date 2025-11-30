# GitHub Copilot Instructions for GhostDraw

## Project Overview

GhostDraw is a Windows desktop application that allows users to draw directly on the screen using a keyboard hotkey to activate and mouse input. The application uses:

-   **WPF** for UI and overlay rendering
-   **Global Windows Hooks** for keyboard/mouse capture
-   **Dependency Injection** with Microsoft.Extensions.DependencyInjection
-   **Structured Logging** with Serilog + Microsoft.Extensions.Logging
-   **.NET 8** target framework

## Critical Safety Requirements

### PRIORITY #1: User Safety & System Stability

This application intercepts global keyboard and mouse input and displays a fullscreen transparent overlay. **If the application crashes or hangs, the user could be locked out of their system.** All code must prioritize robustness and graceful failure.

### PRIORITY #2: Ensure all new code is tested via unit tests or integration tests where applicable.

Ensure The GhostDraw.Tests project has adequate coverage for any new features or changes and that all tests pass.

#### Mandatory Safety Practices

1. **Always Use Try-Catch in Critical Paths**
2. **Hook Callbacks MUST Always Call CallNextHookEx**
    - Never block the hook chain
    - Always return from hook callbacks quickly (< 5ms)
    - Never throw unhandled exceptions from hooks
3. **Overlay Window Safety**
    - Overlay must NEVER prevent access to underlying windows when hidden
    - Always hide overlay on unhandled exceptions
    - Provide emergency escape mechanisms (ESC key should always hide overlay)
    - Window must properly release input focus when hidden
4. **Cleanup on Exit**
5. **Emergency Bailout**
    - Log all critical errors before application termination

### Logging Standards

Use structured logging with proper log levels:

```csharp
// Information - Normal operational events
_logger.LogInformation("Drawing mode enabled");

// Debug - Detailed diagnostic info
_logger.LogDebug("Overlay dimensions: {Width}x{Height}", width, height);

// Warning - Unexpected but recoverable situations
_logger.LogWarning("Hook callback took {Ms}ms (slow)", elapsed);

// Error - Errors that don't crash the app
_logger.LogError(ex, "Failed to save settings");

// Critical - Serious errors that might crash the app
_logger.LogCritical(ex, "Keyboard hook installation failed");
```

**Never log on every mouse move** - use `LogTrace` and ensure it's filtered by default.

## Testing Considerations

When adding new features:

1. **Test crash scenarios** - Kill the process and verify system is usable
2. **Test modal dialogs** - Ensure overlay doesn't block system dialogs
3. **Test Alt+Tab** - Verify overlay doesn't interfere with window switching
4. **Test multi-monitor** - Ensure overlay spans all screens correctly
5. **Test rapid hotkey toggles** - No race conditions
6. **Test high DPI scaling** - Drawing should work on all DPI settings

## Feature Development Workflow

When adding new features:

1. **Plan for failure** - What happens if this feature crashes?
2. **Add logging** - Log state changes at Info level
3. **Add error handling** - Catch and log all exceptions
4. **Test crash recovery** - Verify app remains safe if feature fails
5. **Update settings UI** - Add configuration options when appropriate
6. **Document in code** - Add XML comments for public APIs

## Resources

-   [Low-Level Keyboard Hook Documentation](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc)
-   [WPF Transparent Windows Best Practices](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/)
-   [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Writing-Log-Events)
-   [Microsoft.Extensions.Logging Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
-   [Settings Architecture Guide](../docs/settings-architecture.md) - Internal architecture documentation

---

## Summary: The Golden Rules

1. **Safety First** - User must never be locked out of their system
2. **Fast Hooks** - Hook callbacks must be < 5ms
3. **Always Cleanup** - Unhook and release resources on exit
4. **Fail Gracefully** - Catch exceptions, log them, continue running
5. **Log Judiciously** - Info for events, Debug for diagnostics, Error for problems
6. **Test Edge Cases** - Crashes, multi-monitor, high DPI, rapid input
7. **Document Risks** - Comment any potentially dangerous code paths

Remember: This application has elevated privileges and intercepts user input. With great power comes great responsibility!
