using GhostDraw.Core;
using GhostDraw.Services;
using GhostDraw.Views;
using Microsoft.Extensions.Logging;

namespace GhostDraw.Managers;

public class DrawingManager
{
    private readonly ILogger<DrawingManager> _logger;
    private readonly IOverlayWindow _overlayWindow;
    private readonly AppSettingsService _appSettings;
    private readonly ScreenshotService _screenshotService;
    private readonly GlobalKeyboardHook _keyboardHook;
    private bool _isDrawingLocked = false;

    // Delay in milliseconds before re-showing overlay after opening snipping tool
    private const int SnippingToolOverlayDelayMs = 500;

    public bool IsDrawingMode => _overlayWindow.IsVisible || _isDrawingLocked;

    public DrawingManager(ILogger<DrawingManager> logger, IOverlayWindow overlayWindow,
        AppSettingsService appSettings, ScreenshotService screenshotService,
        GlobalKeyboardHook keyboardHook)
    {
        _logger = logger;
        _overlayWindow = overlayWindow;
        _appSettings = appSettings;
        _screenshotService = screenshotService;
        _keyboardHook = keyboardHook;

        // Initialize lock mode state from saved settings
        _isDrawingLocked = _appSettings.CurrentSettings.LockDrawingMode;

        _logger.LogDebug("DrawingManager initialized - LockDrawingMode={LockMode}", _isDrawingLocked);
    }

    public void EnableDrawing()
    {
        try
        {
            var settings = _appSettings.CurrentSettings;

            if (settings.LockDrawingMode)
            {
                // Toggle mode: switch between locked on/off
                if (_isDrawingLocked)
                {
                    _logger.LogInformation("Toggling drawing mode OFF (was locked)");
                    _isDrawingLocked = false;
                    _overlayWindow.DisableDrawing();
                    _overlayWindow.Hide();

                    // Notify hook that drawing mode is inactive
                    _keyboardHook.SetDrawingModeActive(false);
                }
                else
                {
                    _logger.LogInformation("Toggling drawing mode ON (locking)");
                    _isDrawingLocked = true;
                    _overlayWindow.EnableDrawing();
                    _overlayWindow.Show();
                    _overlayWindow.Activate();
                    _overlayWindow.Focus();

                    // Notify hook that drawing mode is active
                    _keyboardHook.SetDrawingModeActive(true);
                }
            }
            else
            {
                // Hold mode: enable drawing only while hotkey is held
                _logger.LogInformation("Enabling drawing mode (hold)");
                _overlayWindow.EnableDrawing();
                _overlayWindow.Show();
                _overlayWindow.Activate();
                _overlayWindow.Focus();

                // Notify hook that drawing mode is active
                _keyboardHook.SetDrawingModeActive(true);
            }

            _logger.LogDebug("Overlay shown - IsVisible={IsVisible}, IsActive={IsActive}, IsFocused={IsFocused}",
                _overlayWindow.IsVisible, _overlayWindow.IsActive, _overlayWindow.IsFocused);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable drawing mode");
            // Try to ensure clean state
            try
            {
                _overlayWindow.DisableDrawing();
                _overlayWindow.Hide();
                _isDrawingLocked = false;
                _keyboardHook.SetDrawingModeActive(false);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Failed to cleanup after enable drawing error");
            }
            throw; // Re-throw so GlobalExceptionHandler can handle it
        }
    }

    public void DisableDrawing()
    {
        try
        {
            var settings = _appSettings.CurrentSettings;

            if (settings.LockDrawingMode)
            {
                // In lock mode, DisableDrawing is only called from EnableDrawing toggle
                // or from ESC key (which we'll handle separately)
                // So we ignore hotkey release events
                _logger.LogDebug("Ignoring disable request in lock mode (drawing is locked)");
                return;
            }

            // Hold mode: disable when hotkey is released
            _logger.LogInformation("Disabling drawing mode (hold released)");
            _overlayWindow.DisableDrawing();
            _overlayWindow.Hide();

            // Notify hook that drawing mode is inactive
            _keyboardHook.SetDrawingModeActive(false);

            _logger.LogDebug("Overlay hidden");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable drawing mode");
            // Try to ensure overlay is hidden
            try
            {
                _overlayWindow.Hide();
                _keyboardHook.SetDrawingModeActive(false);
            }
            catch (Exception hideEx)
            {
                _logger.LogError(hideEx, "Failed to hide overlay after disable drawing error");
            }
            throw; // Re-throw so GlobalExceptionHandler can handle it
        }
    }

    public void ForceDisableDrawing()
    {
        try
        {
            _logger.LogInformation("ESC pressed - checking help visibility");

            // Check if help is visible and handle accordingly
            bool shouldExitDrawingMode = _overlayWindow.HandleEscapeKey();

            if (shouldExitDrawingMode)
            {
                // Help was not visible, or user wants to exit - force disable drawing mode
                _logger.LogDebug("Force disabling drawing mode");
                _isDrawingLocked = false;
                _overlayWindow.DisableDrawing();
                _overlayWindow.Hide();

                // Notify hook that drawing mode is inactive
                _keyboardHook.SetDrawingModeActive(false);

                _logger.LogDebug("Drawing mode force disabled");
            }
            else
            {
                // Help was visible and has been closed - stay in drawing mode
                _logger.LogDebug("ESC only closed help - remaining in drawing mode");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force disable drawing mode");
            // This is critical - make one last attempt
            try
            {
                _isDrawingLocked = false;
                _overlayWindow.Hide();
                _keyboardHook.SetDrawingModeActive(false);
            }
            catch
            {
                // Nothing more we can do
                _logger.LogCritical("CRITICAL: Failed to hide overlay after force disable");
            }
            // Don't re-throw - this is emergency exit
        }
    }

    public void DisableDrawingMode()
    {
        // Public method for emergency state reset
        try
        {
            _logger.LogWarning("DisableDrawingMode called (likely from emergency reset)");
            _isDrawingLocked = false;
            _overlayWindow.DisableDrawing();
            _overlayWindow.Hide();

            // Notify hook that drawing mode is inactive
            _keyboardHook.SetDrawingModeActive(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed in DisableDrawingMode");
            // Don't throw - this is for emergency cleanup
        }
    }

    /// <summary>
    /// Toggles between Pen and Line drawing tools
    /// </summary>
    public void ToggleTool()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                var newTool = _appSettings.ToggleTool();
                _overlayWindow.OnToolChanged(newTool);
                _logger.LogInformation("Tool toggled to {Tool}", newTool);
            }
            else
            {
                _logger.LogDebug("ToggleTool ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle tool");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Sets the active tool to Pen
    /// </summary>
    public void SetPenTool()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                _appSettings.SetActiveTool(DrawTool.Pen);
                _overlayWindow.OnToolChanged(DrawTool.Pen);
                _logger.LogInformation("Tool set to Pen");
            }
            else
            {
                _logger.LogDebug("SetPenTool ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set pen tool");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Sets the active tool to Line
    /// </summary>
    public void SetLineTool()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                _appSettings.SetActiveTool(DrawTool.Line);
                _overlayWindow.OnToolChanged(DrawTool.Line);
                _logger.LogInformation("Tool set to Line");
            }
            else
            {
                _logger.LogDebug("SetLineTool ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set line tool");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Sets the active tool to Arrow
    /// </summary>
    public void SetArrowTool()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                _appSettings.SetActiveTool(DrawTool.Arrow);
                _overlayWindow.OnToolChanged(DrawTool.Arrow);
                _logger.LogInformation("Tool set to Arrow");
            }
            else
            {
                _logger.LogDebug("SetArrowTool ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set arrow tool");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Sets the active tool to Eraser
    /// </summary>
    public void SetEraserTool()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                _appSettings.SetActiveTool(DrawTool.Eraser);
                _overlayWindow.OnToolChanged(DrawTool.Eraser);
                _logger.LogInformation("Tool set to Eraser");
            }
            else
            {
                _logger.LogDebug("SetEraserTool ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set eraser tool");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Sets the active tool to Rectangle
    /// </summary>
    public void SetRectangleTool()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                _appSettings.SetActiveTool(DrawTool.Rectangle);
                _overlayWindow.OnToolChanged(DrawTool.Rectangle);
                _logger.LogInformation("Tool set to Rectangle");
            }
            else
            {
                _logger.LogDebug("SetRectangleTool ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set rectangle tool");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Sets the active tool to Circle
    /// </summary>
    public void SetCircleTool()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                _appSettings.SetActiveTool(DrawTool.Circle);
                _overlayWindow.OnToolChanged(DrawTool.Circle);
                _logger.LogInformation("Tool set to Circle");
            }
            else
            {
                _logger.LogDebug("SetCircleTool ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set circle tool");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Toggles the help popup with keyboard shortcuts
    /// </summary>
    public void ToggleHelp()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                _overlayWindow.ToggleHelp();
                _logger.LogDebug("Help popup toggled");
            }
            else
            {
                _logger.LogDebug("ToggleHelp ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle help");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Requests to clear the canvas - shows confirmation modal first
    /// </summary>
    public void RequestClearCanvas()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                _logger.LogInformation("Requesting clear canvas confirmation (Delete key)");

                // Show confirmation modal with callbacks
                _overlayWindow.ShowClearCanvasConfirmation(
                    onConfirm: () =>
                    {
                        _logger.LogInformation("Clear canvas confirmed - clearing");
                        _overlayWindow.ClearCanvas();
                    },
                    onCancel: () =>
                    {
                        _logger.LogInformation("Clear canvas canceled - no action");
                    }
                );
            }
            else
            {
                _logger.LogDebug("RequestClearCanvas ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request clear canvas");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Captures a fullscreen screenshot (Ctrl+S)
    /// </summary>
    public void CaptureFullScreenshot()
    {
        try
        {
            _logger.LogInformation("====== CaptureFullScreenshot CALLED ======");
            _logger.LogInformation("Overlay visible: {IsVisible}", _overlayWindow.IsVisible);

            if (_overlayWindow.IsVisible)
            {
                _logger.LogInformation("Capturing full screenshot (Ctrl+S) - calling ScreenshotService");
                var filePath = _screenshotService.CaptureFullScreen();
                _logger.LogInformation("ScreenshotService returned file path: {FilePath}", filePath ?? "(null)");

                if (filePath != null)
                {
                    _logger.LogInformation("Screenshot saved successfully, showing toast notification");
                    _overlayWindow.ShowScreenshotSaved();
                }
                else
                {
                    _logger.LogWarning("Screenshot capture failed - file path is null");
                }
            }
            else
            {
                _logger.LogWarning("CaptureFullScreenshot ignored - overlay not visible");
            }
            _logger.LogInformation("====== CaptureFullScreenshot COMPLETED ======");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture full screenshot");
            // Don't re-throw - not critical
        }
    }

    /// <summary>
    /// Undoes the last drawing action (called via Ctrl+Z)
    /// </summary>
    public void UndoLastAction()
    {
        try
        {
            if (_overlayWindow.IsVisible)
            {
                _logger.LogInformation("Undo last action (Ctrl+Z)");
                _overlayWindow.UndoLastAction();
            }
            else
            {
                _logger.LogDebug("UndoLastAction ignored - overlay not visible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo last action");
            // Don't re-throw - not critical
        }
    }
}
