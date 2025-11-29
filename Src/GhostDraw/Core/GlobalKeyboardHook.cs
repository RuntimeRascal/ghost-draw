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

    private readonly ILogger<GlobalKeyboardHook> _logger;
    private readonly LowLevelKeyboardProc _proc;
    private nint _hookID = nint.Zero;
    private bool _disposed = false;
    private readonly object _disposeLock = new object();

    // Events
    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;
    public event EventHandler? EscapePressed;

    // NEW: Raw key events for recorder
    public event EventHandler<KeyEventArgs>? KeyPressed;
    public event EventHandler<KeyEventArgs>? KeyReleased;

    // NEW: Configurable hotkey VKs
    private List<int> _hotkeyVKs = new() { 0xA2, 0xA4, 0x44 };  // Default: Ctrl+Alt+D
    private Dictionary<int, bool> _keyStates = new();
    private bool _wasHotkeyActive = false;

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

                // Check for ESC key press (emergency exit)
                if (vkCode == VK_ESCAPE && isKeyDown)
                {
                    _logger.LogInformation("?? ESC pressed - emergency exit");
                    EscapePressed?.Invoke(this, EventArgs.Empty);
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
            // CRITICAL: Never throw from hook callback - log and continue
            _logger.LogError(ex, "Exception in keyboard hook callback");
        }

        // MUST ALWAYS call CallNextHookEx to allow other applications to process the hook
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
