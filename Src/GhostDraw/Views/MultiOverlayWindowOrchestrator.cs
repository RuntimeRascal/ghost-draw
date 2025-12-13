using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using GhostDraw.Core;
using GhostDraw.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GhostDraw.Views;

/// <summary>
/// Manages one <see cref="OverlayWindow"/> per monitor and exposes them as a single <see cref="IOverlayWindow"/>.
/// </summary>
public sealed class MultiOverlayWindowOrchestrator : IOverlayWindow, IDisposable
{
    private readonly ILogger<MultiOverlayWindowOrchestrator> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DrawingHistory _drawingHistory;
    private readonly List<OverlayWindow> _overlays;
    private bool _isClearCanvasModalVisible;
    private int _clearCanvasDecisionTaken;
    private Action? _pendingClearCanvasCancel;

    public MultiOverlayWindowOrchestrator(
        ILogger<MultiOverlayWindowOrchestrator> logger,
        IServiceProvider serviceProvider,
        DrawingHistory drawingHistory)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _drawingHistory = drawingHistory;

        _overlays = CreateOverlays();
        _logger.LogInformation("Created {Count} overlay window(s)", _overlays.Count);
    }

    public bool IsVisible => _overlays.Any(o => o.IsVisible);
    public bool IsActive => _overlays.Any(o => o.IsActive);
    public bool IsFocused => _overlays.Any(o => o.IsFocused);

    public void EnableDrawing()
    {
        foreach (var overlay in _overlays)
        {
            try
            {
                overlay.EnableDrawing();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable drawing on overlay {OverlayId}", overlay.OverlayId);
            }
        }
    }

    public void DisableDrawing()
    {
        try
        {
            _drawingHistory.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear drawing history during DisableDrawing");
        }

        _isClearCanvasModalVisible = false;
        _pendingClearCanvasCancel = null;

        foreach (var overlay in _overlays)
        {
            try
            {
                overlay.DisableDrawingInternal(clearHistory: false);
                overlay.HideClearCanvasConfirmation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable drawing on overlay {OverlayId}", overlay.OverlayId);
            }
        }
    }

    public void Show()
    {
        foreach (var overlay in _overlays)
        {
            try
            {
                overlay.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show overlay {OverlayId}", overlay.OverlayId);
            }
        }
    }

    public void Hide()
    {
        _isClearCanvasModalVisible = false;
        _pendingClearCanvasCancel = null;

        foreach (var overlay in _overlays)
        {
            try
            {
                overlay.HideClearCanvasConfirmation();
                overlay.Hide();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide overlay {OverlayId}", overlay.OverlayId);
            }
        }
    }

    public bool Activate()
    {
        var target = GetOverlayForCursor();
        if (target == null)
        {
            return false;
        }

        try
        {
            return target.Activate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate overlay {OverlayId}", target.OverlayId);
            return false;
        }
    }

    public bool Focus()
    {
        var target = GetOverlayForCursor();
        if (target == null)
        {
            return false;
        }

        try
        {
            return target.Focus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to focus overlay {OverlayId}", target.OverlayId);
            return false;
        }
    }

    public void OnToolChanged(DrawTool newTool)
    {
        foreach (var overlay in _overlays)
        {
            try
            {
                overlay.OnToolChanged(newTool);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change tool on overlay {OverlayId}", overlay.OverlayId);
            }
        }
    }

    public void ClearCanvas()
    {
        try
        {
            _drawingHistory.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear drawing history during ClearCanvas");
        }

        foreach (var overlay in _overlays)
        {
            try
            {
                overlay.ClearCanvasInternal(clearHistory: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear canvas on overlay {OverlayId}", overlay.OverlayId);
            }
        }
    }

    public void ToggleHelp()
    {
        foreach (var overlay in _overlays)
        {
            try
            {
                overlay.ToggleHelp();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set help visibility on overlay {OverlayId}", overlay.OverlayId);
            }
        }
    }

    public bool HandleEscapeKey()
    {
        try
        {
            if (_isClearCanvasModalVisible)
            {
                _logger.LogDebug("ESC pressed while clear canvas modal visible - canceling");
                CancelClearCanvasConfirmation();
                return false;
            }

            bool anyClosedHelp = false;
            foreach (var overlay in _overlays)
            {
                // If help is visible on an overlay, this will close it and return false.
                // If nothing is visible, it returns true with no side effects.
                if (!overlay.HandleEscapeKey())
                {
                    anyClosedHelp = true;
                }
            }

            return !anyClosedHelp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle ESC key");
            // On error, always exit drawing mode for safety
            return true;
        }
    }

    public void ShowScreenshotSaved()
    {
        foreach (var overlay in _overlays)
        {
            try
            {
                overlay.ShowScreenshotSaved();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show screenshot toast on overlay {OverlayId}", overlay.OverlayId);
            }
        }
    }

    public void UndoLastAction()
    {
        try
        {
            var undo = _drawingHistory.UndoLastAction();
            if (undo == null)
            {
                return;
            }

            var overlay = _overlays.FirstOrDefault(o =>
                string.Equals(o.OverlayId, undo.OverlayId, StringComparison.OrdinalIgnoreCase));

            if (overlay == null)
            {
                _logger.LogWarning("Undo: No overlay found for OverlayId={OverlayId}", undo.OverlayId);
                return;
            }

            if (!overlay.TryRemoveElementById(undo.ElementId))
            {
                _logger.LogWarning("Undo: Element not found on overlay {OverlayId} (ElementId={ElementId})", undo.OverlayId, undo.ElementId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo last action");
        }
    }

    public void ShowClearCanvasConfirmation(Action onConfirm, Action onCancel)
    {
        try
        {
            _isClearCanvasModalVisible = true;
            _clearCanvasDecisionTaken = 0;

            Action confirmOnce = () =>
            {
                if (System.Threading.Interlocked.Exchange(ref _clearCanvasDecisionTaken, 1) != 0)
                {
                    return;
                }

                _isClearCanvasModalVisible = false;
                _pendingClearCanvasCancel = null;

                foreach (var overlay in _overlays)
                {
                    overlay.HideClearCanvasConfirmation();
                }

                onConfirm();
            };

            Action cancelOnce = () =>
            {
                if (System.Threading.Interlocked.Exchange(ref _clearCanvasDecisionTaken, 1) != 0)
                {
                    return;
                }

                _isClearCanvasModalVisible = false;
                _pendingClearCanvasCancel = null;

                foreach (var overlay in _overlays)
                {
                    overlay.HideClearCanvasConfirmation();
                }

                onCancel();
            };

            _pendingClearCanvasCancel = cancelOnce;

            foreach (var overlay in _overlays)
            {
                overlay.ShowClearCanvasConfirmation(confirmOnce, cancelOnce);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show clear canvas confirmation modal on all overlays");
            try
            {
                _isClearCanvasModalVisible = false;
                _pendingClearCanvasCancel = null;
                onCancel();
            }
            catch
            {
                // best-effort
            }
        }
    }

    private void CancelClearCanvasConfirmation()
    {
        try
        {
            var cancel = _pendingClearCanvasCancel;
            if (cancel != null)
            {
                cancel();
            }
            else
            {
                // Fallback: ensure modals are dismissed even if callbacks are missing
                _isClearCanvasModalVisible = false;
                foreach (var overlay in _overlays)
                {
                    overlay.HideClearCanvasConfirmation();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel clear canvas confirmation");
        }
    }

    private OverlayWindow? GetOverlayForCursor()
    {
        try
        {
            var cursorPoint = Control.MousePosition;
            var screen = Screen.FromPoint(cursorPoint);
            return _overlays.FirstOrDefault(o =>
                string.Equals(o.OverlayId, screen.DeviceName, StringComparison.OrdinalIgnoreCase)) ?? _overlays.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine overlay for cursor");
            return _overlays.FirstOrDefault();
        }
    }

    private List<OverlayWindow> CreateOverlays()
    {
        var overlays = new List<OverlayWindow>();

        try
        {
            foreach (var screen in Screen.AllScreens)
            {
                var overlayId = screen.DeviceName;
                var boundsDip = MonitorBoundsHelper.GetScreenBoundsInDips(screen);
                var overlay = ActivatorUtilities.CreateInstance<OverlayWindow>(_serviceProvider, overlayId, boundsDip);
                overlays.Add(overlay);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to create per-monitor overlays; falling back to a single virtual-screen overlay");
        }

        if (overlays.Count == 0)
        {
            overlays.Add(ActivatorUtilities.CreateInstance<OverlayWindow>(_serviceProvider, "VirtualScreen"));
        }

        return overlays;
    }

    public void Dispose()
    {
        foreach (var overlay in _overlays)
        {
            try
            {
                overlay.Hide();
                overlay.Close();
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private static class MonitorBoundsHelper
    {
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const int MDT_EFFECTIVE_DPI = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        public static Rect GetScreenBoundsInDips(Screen screen)
        {
            var boundsPx = screen.Bounds;

            uint dpiX = 96;
            uint dpiY = 96;

            try
            {
                var center = new POINT
                {
                    X = boundsPx.Left + (boundsPx.Width / 2),
                    Y = boundsPx.Top + (boundsPx.Height / 2)
                };

                var monitor = MonitorFromPoint(center, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var hr = GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                    if (hr != 0)
                    {
                        dpiX = 96;
                        dpiY = 96;
                    }
                }
            }
            catch (DllNotFoundException)
            {
                dpiX = 96;
                dpiY = 96;
            }
            catch (EntryPointNotFoundException)
            {
                dpiX = 96;
                dpiY = 96;
            }
            catch
            {
                dpiX = 96;
                dpiY = 96;
            }

            double scaleX = 96.0 / dpiX;
            double scaleY = 96.0 / dpiY;

            return new Rect(
                boundsPx.Left * scaleX,
                boundsPx.Top * scaleY,
                boundsPx.Width * scaleX,
                boundsPx.Height * scaleY);
        }
    }
}
