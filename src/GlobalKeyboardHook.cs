using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace GhostDraw
{
    public class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        // Virtual Key Codes
        private const int VK_LCONTROL = 0xA2;  // 162
        private const int VK_RCONTROL = 0xA3;  // 163
        private const int VK_LMENU = 0xA4;     // 164 (Alt)
        private const int VK_RMENU = 0xA5;     // 165 (Alt)
        private const int VK_D = 0x44;         // 68
        private const int VK_ESCAPE = 0x1B;    // 27

        private readonly ILogger<GlobalKeyboardHook> _logger;
        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private bool _disposed = false;
        private readonly object _disposeLock = new object();

        public event EventHandler? HotkeyPressed;
        public event EventHandler? HotkeyReleased;
        public event EventHandler? EscapePressed;

        private bool _isCtrlPressed = false;
        private bool _isAltPressed = false;
        private bool _isDPressed = false;
        private bool _wasHotkeyActive = false;

        public GlobalKeyboardHook(ILogger<GlobalKeyboardHook> logger)
        {
            _logger = logger;
            _logger.LogDebug("GlobalKeyboardHook constructor called");
            _proc = HookCallback;
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
                
                if (_hookID == IntPtr.Zero)
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

                if (_hookID != IntPtr.Zero)
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
                        _hookID = IntPtr.Zero;
                    }
                }
                else
                {
                    _logger.LogDebug("Stop called but hook ID is zero - nothing to unhook");
                }
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
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
            return IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN;
                    
                    string keyName = GetKeyName(vkCode);
                    
                    // Check for ESC key press (emergency exit)
                    if (vkCode == VK_ESCAPE && isKeyDown)
                    {
                        _logger.LogInformation("? ESC pressed - emergency exit");
                        EscapePressed?.Invoke(this, EventArgs.Empty);
                    }
                    
                    // Only log hotkey-related keys
                    bool isHotkeyKey = vkCode == VK_LCONTROL || vkCode == VK_RCONTROL || 
                                       vkCode == VK_LMENU || vkCode == VK_RMENU || vkCode == VK_D || vkCode == VK_ESCAPE;
                    
                    if (isHotkeyKey)
                    {
                        _logger.LogDebug("Key {KeyName} (VK:{VkCode}) {State}", keyName, vkCode, isKeyDown ? "DOWN" : "UP");
                    }

                    // Track modifier keys using actual virtual key codes
                    if (vkCode == VK_LCONTROL || vkCode == VK_RCONTROL)
                    {
                        _isCtrlPressed = isKeyDown;
                        _logger.LogDebug("Ctrl state ? {State}", _isCtrlPressed);
                    }
                    else if (vkCode == VK_LMENU || vkCode == VK_RMENU)
                    {
                        _isAltPressed = isKeyDown;
                        _logger.LogDebug("Alt state ? {State}", _isAltPressed);
                    }
                    else if (vkCode == VK_D)
                    {
                        _isDPressed = isKeyDown;
                        _logger.LogDebug("D state ? {State}", _isDPressed);
                    }

                    // Check if hotkey combination is active
                    bool isHotkeyActive = _isCtrlPressed && _isAltPressed && _isDPressed;

                    // Fire events on state changes
                    if (isHotkeyActive && !_wasHotkeyActive)
                    {
                        _logger.LogInformation("? HOTKEY PRESSED - Ctrl+Alt+D active");
                        HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    }
                    else if (!isHotkeyActive && _wasHotkeyActive)
                    {
                        _logger.LogInformation("? HOTKEY RELEASED - Ctrl+Alt+D released");
                        HotkeyReleased?.Invoke(this, EventArgs.Empty);
                    }

                    _wasHotkeyActive = isHotkeyActive;
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

        private string GetKeyName(int vkCode)
        {
            return vkCode switch
            {
                VK_LCONTROL => "LeftCtrl",
                VK_RCONTROL => "RightCtrl",
                VK_LMENU => "LeftAlt",
                VK_RMENU => "RightAlt",
                VK_D => "D",
                VK_ESCAPE => "ESC",
                _ => $"VK_{vkCode}"
            };
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
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
