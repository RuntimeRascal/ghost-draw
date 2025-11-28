# GitHub Copilot Instructions for GhostDraw

## Project Overview

GhostDraw is a Windows desktop application that allows users to draw directly on the screen using a keyboard hotkey (Ctrl+Alt+D) and mouse input. The application uses:

- **WPF** for UI and overlay rendering
- **Global Windows Hooks** for keyboard/mouse capture
- **Dependency Injection** with Microsoft.Extensions.DependencyInjection
- **Structured Logging** with Serilog + Microsoft.Extensions.Logging
- **.NET 8** target framework

### Directory Structure
From repo root (`C:\code\github\ghost-draw\`):
- `src/` - Main application code. .sln and project files here. **This is the project root / workspace directory.**
- `.github/copilot-instructions.md` - This file.
- `docs/` - Additional documentation, design notes, etc. When adding docs here, update the solution file docs folder to show them in IDEs.
- `tests/` - Unit and integration tests.
- `README.md` - Project overview and setup instructions.

**Important**: When creating or editing files:
- **Repo root** = `C:\code\github\ghost-draw\` (where README.md, .github/, docs/, tests/ live)
- **Project root / Workspace** = `C:\code\github\ghost-draw\src\` (where .csproj and source code live)
- Documentation files (`.md`) should go in the **repo root** or `docs/` directory, NOT in `src/`
- Source code files (`.cs`, `.xaml`, etc.) should go in the **project root** (`src/`) or subdirectories within it

## Critical Safety Requirements

### ?? PRIORITY #1: User Safety & System Stability

This application intercepts global keyboard and mouse input and displays a fullscreen transparent overlay. **If the application crashes or hangs, the user could be locked out of their system.** All code must prioritize robustness and graceful failure.

### ?? PRIORITY #2: Ensure all new code is tested via unit tests or integration tests where applicable.
Ensure The GhostDraw.Tests project has adequate coverage for any new features or changes and that all tests pass.


#### Mandatory Safety Practices

1. **Always Use Try-Catch in Critical Paths**
   ```csharp
   // ? GOOD - Protected hook callback
   private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
   {
       try
       {
           // Hook logic here
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Hook callback failed");
           // MUST allow hook chain to continue
       }
       return CallNextHookEx(_hookID, nCode, wParam, lParam);
   }
   ```

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
   ```csharp
   // Always unhook on dispose/exit
   protected override void OnExit(ExitEventArgs e)
   {
       try
       {
           _keyboardHook?.Dispose(); // Removes hooks
           _notifyIcon?.Dispose();
           ServiceConfiguration.Shutdown();
       }
       catch (Exception ex)
       {
           _logger?.LogCritical(ex, "Failed during cleanup");
       }
       base.OnExit(e);
   }
   ```

5. **Emergency Bailout**
   - Consider adding ESC key as emergency override to hide overlay
   - Add Ctrl+Alt+Shift+X as emergency kill switch
   - Log all critical errors before application termination

### Hook-Related Guidelines

- **Never** perform blocking operations in hook callbacks
- **Never** call MessageBox or show dialogs from hook callbacks
- **Never** do heavy processing in hook callbacks (delegate to background tasks)
- **Always** log hook installation success/failure
- **Always** verify hook is properly uninstalled on exit

### Overlay Window Guidelines

- **Always** set `Topmost = true` but ensure it doesn't block system dialogs
- **Never** set `ShowInTaskbar = true` (prevents Alt+Tab interference)
- **Always** ensure overlay is truly transparent when not drawing
- **Always** hide overlay on application deactivation/suspend
- **Always** test that mouse/keyboard input passes through when inactive

## Architecture Guidelines

### Dependency Injection

All components should use constructor injection:

```csharp
public class MyComponent
{
    private readonly ILogger<MyComponent> _logger;
    private readonly SomeDependency _dependency;
    
    public MyComponent(ILogger<MyComponent> logger, SomeDependency dependency)
    {
        _logger = logger;
        _dependency = dependency;
    }
}
```

Register new services in `ServiceConfiguration.ConfigureServices()`:

```csharp
services.AddSingleton<MyComponent>();
```

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

### Event Handling Pattern

```csharp
// ? GOOD - Safe event invocation
try
{
    HotkeyPressed?.Invoke(this, EventArgs.Empty);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Event handler failed");
    // Continue execution
}

// ? BAD - Unprotected event invocation
HotkeyPressed?.Invoke(this, EventArgs.Empty); // Could crash app
```

## Code Style Guidelines

### Naming Conventions

- Private fields: `_camelCase`
- Constants: `UPPER_SNAKE_CASE` or `PascalCase`
- Public/Internal: `PascalCase`
- Interfaces: `IPascalCase`

### Error Messages

Include context in error messages:

```csharp
_logger.LogError(ex, "Failed to install keyboard hook. HookID={HookId}", _hookID);
```

### Null Safety

Leverage C# 8+ nullable reference types:

```csharp
private ILogger<MyClass>? _logger; // Nullable
private readonly Config _config = null!; // Non-null assertion (set in constructor)
```

## Testing Considerations

When adding new features:

1. **Test crash scenarios** - Kill the process and verify system is usable
2. **Test modal dialogs** - Ensure overlay doesn't block system dialogs
3. **Test Alt+Tab** - Verify overlay doesn't interfere with window switching
4. **Test multi-monitor** - Ensure overlay spans all screens correctly
5. **Test rapid hotkey toggles** - No race conditions
6. **Test high DPI scaling** - Drawing should work on all DPI settings

## Common Pitfalls to Avoid

? **Don't**: Use `Application.Current.Dispatcher.Invoke` in hook callbacks  
? **Do**: Queue work to background thread if needed

? **Don't**: Store large objects in hook callback scope  
? **Do**: Keep hook callbacks lean and fast

? **Don't**: Assume hook callbacks run on UI thread  
? **Do**: Marshal to UI thread only when needed

? **Don't**: Show error messages directly to user from background code  
? **Do**: Log errors and handle them gracefully

? **Don't**: Use `Thread.Sleep` or blocking waits in hook callbacks  
? **Do**: Use async/await patterns when possible (not in hooks though!)

## Windows API (P/Invoke) Guidelines

Always check return values from Windows APIs:

```csharp
_hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
if (_hookID == IntPtr.Zero)
{
    int error = Marshal.GetLastWin32Error();
    _logger.LogError("SetWindowsHookEx failed with error code: {ErrorCode}", error);
    throw new Win32Exception(error);
}
```

## Performance Guidelines

- Hook callbacks should complete in < 5ms
- Don't allocate large objects in hot paths
- Consider object pooling for frequently created objects
- Profile with multiple monitors (more pixels = more work)

## Feature Development Workflow

When adding new features:

1. **Plan for failure** - What happens if this feature crashes?
2. **Add logging** - Log state changes at Info level
3. **Add error handling** - Catch and log all exceptions
4. **Test crash recovery** - Verify app remains safe if feature fails
5. **Update settings UI** - Add configuration options when appropriate
6. **Document in code** - Add XML comments for public APIs

## Future Considerations

When suggesting new features, consider:

- **Brush customization** (color, thickness, opacity)
- **Stroke persistence** (save/load drawings)
- **Undo/redo functionality**
- **Screenshot integration**
- **Multi-user scenarios** (Terminal Server, Fast User Switching)
- **Accessibility** (screen readers, high contrast mode)
- **Localization** (internationalization support)

## Resources

- [Low-Level Keyboard Hook Documentation](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc)
- [WPF Transparent Windows Best Practices](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Writing-Log-Events)
- [Microsoft.Extensions.Logging Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)

---

## Summary: The Golden Rules

1. **Safety First** - User must never be locked out of their system
2. **Fast Hooks** - Hook callbacks must be < 5ms
3. **Always Cleanup** - Unhook and release resources on exit
4. **Fail Gracefully** - Catch exceptions, log them, continue running
5. **Log Judiciously** - Info for events, Debug for diagnostics, Error for problems
6. **Test Edge Cases** - Crashes, multi-monitor, high DPI, rapid input
7. **Document Risks** - Comment any potentially dangerous code paths

Remember: This application has elevated privileges and intercepts user input. With great power comes great responsibility! ???
