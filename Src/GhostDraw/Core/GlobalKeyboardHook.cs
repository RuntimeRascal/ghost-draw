using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace GhostDraw.Core;

public class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    // Only keep VK_ESCAPE constant (emergency exit)
    private const int VK_ESCAPE = 0x1B;    // 27
    private const int VK_DELETE = 0x2E;    // 46 - 'Delete' key for clear canvas
    private const int VK_L = 0x4C;         // 76 - 'L' key for line tool
    private const int VK_P = 0x50;         // 80 - 'P' key for pen tool
    private const int VK_E = 0x45;         // 69 - 'E' key for eraser tool
    private const int VK_U = 0x55;         // 85 - 'U' key for rectangle tool
    private const int VK_C = 0x43;         // 67 - 'C' key for circle tool
    private const int VK_F1 = 0x70;        // 112 - 'F1' key for help
    private const int VK_S = 0x53;         // 83 - 'S' key for screenshot (Ctrl+S only)
    private const int VK_Z = 0x5A;         // 90 - 'Z' key for undo (Ctrl+Z only)
    private const int VK_LCONTROL = 0xA2;  // 162 - Left Control key
    private const int VK_RCONTROL = 0xA3;  // 163 - Right Control key

    private readonly ILogger<GlobalKeyboardHook> _logger;
    private readonly LowLevelKeyboardProc _proc;
    private nint _hookID = nint.Zero;
    private bool _disposed = false;
    private readonly object _disposeLock = new object();

    // Events
    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;
    public event EventHandler? EscapePressed;
    public event EventHandler? ClearCanvasPressed;
    public event EventHandler? PenToolPressed;
    public event EventHandler? LineToolPressed;
    public event EventHandler? EraserToolPressed;
    public event EventHandler? RectangleToolPressed;
    public event EventHandler? CircleToolPressed;
    public event EventHandler? HelpPressed;
    public event EventHandler? ScreenshotFullPressed;
    public event EventHandler? UndoPressed;

    // NEW: Raw key events for recorder
    public event EventHandler<KeyEventArgs>? KeyPressed;
    public event EventHandler<KeyEventArgs>? KeyReleased;

    // NEW: Configurable hotkey VKs
    private List<int> _hotkeyVKs = new() { 0xA2, 0xA4, 0x44 };  // Default: Ctrl+Alt+D
    private Dictionary<int, bool> _keyStates = new();
    private bool _wasHotkeyActive = false;
    private volatile bool _isControlPressed = false;

    // Drawing mode state - used to determine if we should suppress keys
    private volatile bool _isDrawingModeActive = false;

    public GlobalKeyboardHook(ILogger<GlobalKeyboardHook> logger)
    {
        _logger = logger;
        _logger.LogDebug("GlobalKeyboardHook constructor called");
        _proc = HookCallback;

        // Initialize key states
        foreach (var vk in _hotkeyVKs)
            _keyStates[vk] = false;
    }

    /// <summary>
    /// Configures the hotkey combination
    /// </summary>
    /// <param name="virtualKeys">List of virtual key codes</param>
    public void Configure(List<int> virtualKeys)
    {
        _hotkeyVKs = [.. virtualKeys];
        _keyStates.Clear();

        foreach (var vk in virtualKeys)
            _keyStates[vk] = false;

        _logger.LogInformation("Hotkey reconfigured to: {DisplayName}",
            Helpers.VirtualKeyHelper.GetCombinationDisplayName(virtualKeys));
    }

    /// <summary>
    /// Event args for key events
    /// </summary>
    public class KeyEventArgs : EventArgs
    {
        public int VirtualKeyCode { get; }

        public KeyEventArgs(int vkCode)
        {
            VirtualKeyCode = vkCode;
        }
    }

    public void Start()
    {
        lock (_disposeLock)
        {
            if (_disposed)
            {
                _logger.LogWarning("Cannot start hook - already disposed");
                return;
            }

            _logger.LogDebug("Starting keyboard hook installation");
            _hookID = SetHook(_proc);
            _logger.LogDebug("Hook ID = {HookId}", _hookID);

            if (_hookID == nint.Zero)
            {
                _logger.LogError("Failed to set keyboard hook! Hook ID is zero");
            }
            else
            {
                _logger.LogInformation("Keyboard hook successfully installed");
            }
        }
    }

    public void Stop()
    {
        lock (_disposeLock)
        {
            if (_disposed)
            {
                _logger.LogDebug("Stop called but already disposed - skipping");
                return;
            }

            if (_hookID != nint.Zero)
            {
                _logger.LogDebug("Stopping keyboard hook (ID: {HookId})", _hookID);

                try
                {
                    if (UnhookWindowsHookEx(_hookID))
                    {
                        _logger.LogInformation("Keyboard hook successfully uninstalled");
                    }
                    else
                    {
                        int error = Marshal.GetLastWin32Error();
                        _logger.LogWarning("UnhookWindowsHookEx returned false. Error code: {ErrorCode}", error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception while unhooking keyboard hook");
                }
                finally
                {
                    _hookID = nint.Zero;
                }
            }
            else
            {
                _logger.LogDebug("Stop called but hook ID is zero - nothing to unhook");
            }
        }
    }

    private nint SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule? curModule = curProcess.MainModule)
        {
            if (curModule != null)
            {
                _logger.LogTrace("Module name = {ModuleName}", curModule.ModuleName);
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
            else
            {
                _logger.LogError("Process module is null!");
            }
        }
        return nint.Zero;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        bool shouldSuppressKey = false;

        try
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isKeyDown = wParam == WM_KEYDOWN;

                // Fire raw key events (for recorder)
                if (isKeyDown)
                    KeyPressed?.Invoke(this, new KeyEventArgs(vkCode));
                else
                    KeyReleased?.Invoke(this, new KeyEventArgs(vkCode));

                // Track Control key state (both left and right control keys)
                if (vkCode == VK_LCONTROL || vkCode == VK_RCONTROL)
                {
                    _isControlPressed = isKeyDown;
                    _logger.LogDebug("Control key ({Type}) {State}",
                        vkCode == VK_LCONTROL ? "Left" : "Right",
                        isKeyDown ? "PRESSED" : "RELEASED");
                }

                // Check for ESC key press (emergency exit)
                if (vkCode == VK_ESCAPE && isKeyDown)
                {
                    _logger.LogInformation("🔴 ESC pressed - emergency exit");
                    EscapePressed?.Invoke(this, EventArgs.Empty);
                }

                // Check for Delete key press (clear canvas - only when drawing mode is active)
                if (vkCode == VK_DELETE && isKeyDown && _isDrawingModeActive)
                {
                    _logger.LogDebug("Delete key pressed - clear canvas confirmation request");
                    ClearCanvasPressed?.Invoke(this, EventArgs.Empty);
                    
                    // Suppress Delete key when drawing mode is active to prevent deleting in underlying apps
                    shouldSuppressKey = true;
                    _logger.LogDebug("Delete key suppressed - drawing mode is active");
                }

                // Check for L key press (line tool)
                if (vkCode == VK_L && isKeyDown)
                {
                    _logger.LogDebug("L key pressed - line tool request");
                    LineToolPressed?.Invoke(this, EventArgs.Empty);
                }

                // Check for P key press (pen tool)
                if (vkCode == VK_P && isKeyDown)
                {
                    _logger.LogDebug("P key pressed - pen tool request");
                    PenToolPressed?.Invoke(this, EventArgs.Empty);
                }

                // Check for E key press (eraser tool)
                if (vkCode == VK_E && isKeyDown)
                {
                    _logger.LogDebug("E key pressed - eraser tool request");
                    EraserToolPressed?.Invoke(this, EventArgs.Empty);
                }

                // Check for U key press (rectangle tool)
                if (vkCode == VK_U && isKeyDown)
                {
                    _logger.LogDebug("U key pressed - rectangle tool request");
                    RectangleToolPressed?.Invoke(this, EventArgs.Empty);
                }

                // Check for C key press (circle tool)
                if (vkCode == VK_C && isKeyDown)
                {
                    _logger.LogDebug("C key pressed - circle tool request");
                    CircleToolPressed?.Invoke(this, EventArgs.Empty);
                }

                // Check for F1 key press (help)
                if (vkCode == VK_F1 && isKeyDown)
                {
                    _logger.LogDebug("F1 key pressed - help request");
                    HelpPressed?.Invoke(this, EventArgs.Empty);
                }

                // Check for Ctrl+S key press (full screenshot only - no snipping tool)
                if (vkCode == VK_S && isKeyDown && _isControlPressed)
                {
                    _logger.LogInformation("====== CTRL+S DETECTED ======");
                    _logger.LogInformation("Control key state: {IsControlPressed}", _isControlPressed);
                    _logger.LogInformation("Drawing mode active: {IsDrawingModeActive}", _isDrawingModeActive);

                    _logger.LogInformation("Ctrl+S pressed - firing ScreenshotFullPressed event");
                    ScreenshotFullPressed?.Invoke(this, EventArgs.Empty);
                    _logger.LogInformation("ScreenshotFullPressed event fired, subscribers: {Count}",
                        ScreenshotFullPressed?.GetInvocationList().Length ?? 0);

                    // Suppress Ctrl+S when drawing mode is active to prevent Windows Snipping Tool
                    if (_isDrawingModeActive)
                    {
                        shouldSuppressKey = true;
                        _logger.LogInformation("KEY WILL BE SUPPRESSED - Drawing mode is active");
                    }
                    else
                    {
                        _logger.LogInformation("KEY WILL NOT BE SUPPRESSED - Drawing mode is inactive");
                    }

                    _logger.LogInformation("====== END CTRL+S HANDLING ======");
                }

                // Check for Ctrl+Z key press (undo - only when drawing mode is active)
                if (vkCode == VK_Z && isKeyDown && _isControlPressed && _isDrawingModeActive)
                {
                    _logger.LogInformation("Ctrl+Z pressed - firing UndoPressed event");
                    UndoPressed?.Invoke(this, EventArgs.Empty);

                    // Suppress Ctrl+Z when drawing mode is active to prevent underlying apps from receiving it
                    shouldSuppressKey = true;
                    _logger.LogDebug("Ctrl+Z suppressed - drawing mode is active");
                }

                // Track hotkey state
                if (_hotkeyVKs.Contains(vkCode))
                {
                    _keyStates[vkCode] = isKeyDown;

                    _logger.LogDebug("Key VK:{VkCode} {State}", vkCode, isKeyDown ? "DOWN" : "UP");

                    // Check if ALL hotkey keys are pressed
                    bool allPressed = _hotkeyVKs.All(vk => _keyStates[vk]);

                    // Fire events on state changes
                    if (allPressed && !_wasHotkeyActive)
                    {
                        _logger.LogInformation("🟢 HOTKEY PRESSED");
                        HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    }
                    else if (!allPressed && _wasHotkeyActive)
                    {
                        _logger.LogInformation("🟢 HOTKEY RELEASED");
                        HotkeyReleased?.Invoke(this, EventArgs.Empty);
                    }

                    _wasHotkeyActive = allPressed;
                }
            }
        }
        catch (Exception ex)
        {
            // CRITICAL: Never throw from hook callback - log and continue
            _logger.LogError(ex, "Exception in keyboard hook callback");
        }

        // If we want to suppress the key, return 1 to block it from reaching other applications
        // Otherwise, call CallNextHookEx to allow other applications to process the hook
        if (shouldSuppressKey)
        {
            _logger.LogTrace("Key suppressed - not calling CallNextHookEx");
            return (nint)1;
        }

        // MUST call CallNextHookEx for non-suppressed keys to allow other applications to process them
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed)
            {
                _logger.LogDebug("Dispose called but already disposed - skipping");
                return;
            }

            _logger.LogDebug("Disposing keyboard hook");
            _disposed = true;

            // Stop will check _disposed flag and handle cleanup safely
            Stop();
        }
    }

    /// <summary>
    /// Sets the drawing mode state. When active, certain keys (like Ctrl+S) will be suppressed
    /// to prevent Windows from intercepting them.
    /// </summary>
    public void SetDrawingModeActive(bool isActive)
    {
        var previousState = _isDrawingModeActive;
        _isDrawingModeActive = isActive;

        if (previousState != isActive)
        {
            _logger.LogInformation("====== DRAWING MODE STATE CHANGED ======");
            _logger.LogInformation("Previous state: {PreviousState}, New state: {NewState}", previousState, isActive);
            _logger.LogInformation("Timestamp: {Timestamp}", DateTime.Now.ToString("HH:mm:ss.fff"));
        }
        else
        {
            _logger.LogDebug("Drawing mode state set to: {IsActive} (no change)", isActive);
        }
    }

    // P/Invoke declarations
    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint GetModuleHandle(string lpModuleName);
}
