using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using GhostDraw.Core;
using GhostDraw.Helpers;
using GhostDraw.Services;
using GhostDraw.Tools;
using Microsoft.Extensions.Logging;
using WpfCursors = System.Windows.Input.Cursors;

namespace GhostDraw.Views;

public partial class OverlayWindow : Window, IOverlayWindow
{
    private readonly ILogger<OverlayWindow> _logger;
    private readonly AppSettingsService _appSettings;
    private readonly CursorHelper _cursorHelper;
    private readonly DrawingHistory _drawingHistory;
    private bool _isDrawing = false;
    private bool _isHelpVisible = false;

    // Tool instances
    private readonly PenTool _penTool;
    private readonly LineTool _lineTool;
    private readonly ArrowTool _arrowTool;
    private readonly EraserTool _eraserTool;
    private readonly RectangleTool _rectangleTool;
    private readonly CircleTool _circleTool;
    private readonly TextTool _textTool;
    private IDrawingTool? _activeTool;

    public string OverlayId { get; }

    /// <summary>
    /// Gets the display name of the current hotkey combination for binding
    /// </summary>
    public string HotkeyDisplayName => _appSettings.CurrentSettings.HotkeyDisplayName;

    public OverlayWindow(ILogger<OverlayWindow> logger, AppSettingsService appSettings, CursorHelper cursorHelper,
        DrawingHistory drawingHistory, PenTool penTool, LineTool lineTool, ArrowTool arrowTool, EraserTool eraserTool,
        RectangleTool rectangleTool, CircleTool circleTool, TextTool textTool, string? overlayId = null, Rect? screenBounds = null)
    {
        _logger = logger;
        _appSettings = appSettings;
        _cursorHelper = cursorHelper;
        _drawingHistory = drawingHistory;
        _penTool = penTool;
        _lineTool = lineTool;
        _arrowTool = arrowTool;
        _eraserTool = eraserTool;
        _rectangleTool = rectangleTool;
        _circleTool = circleTool;
        _textTool = textTool;
        OverlayId = string.IsNullOrWhiteSpace(overlayId) ? "VirtualScreen" : overlayId;
        _logger.LogDebug("OverlayWindow constructor called");

        InitializeComponent();

        // Keep per-overlay tool instances in sync with the shared settings service
        _appSettings.BrushColorChanged += AppSettings_BrushColorChanged;
        _appSettings.BrushThicknessChanged += AppSettings_BrushThicknessChanged;

        // Subscribe to tool events for history tracking
        _penTool.ActionCompleted += OnToolActionCompleted;
        _lineTool.ActionCompleted += OnToolActionCompleted;
        _arrowTool.ActionCompleted += OnToolActionCompleted;
        _rectangleTool.ActionCompleted += OnToolActionCompleted;
        _circleTool.ActionCompleted += OnToolActionCompleted;
        _eraserTool.ElementErased += OnElementErased;
        _textTool.ActionCompleted += OnToolActionCompleted;

        // Configure overlay user controls
        ThicknessIndicator.DisplayDuration = TimeSpan.FromSeconds(1.5);
        ThicknessIndicator.FadeOutDuration = TimeSpan.FromMilliseconds(300);

        CanvasClearedToast.DisplayDuration = TimeSpan.FromSeconds(1.0);
        CanvasClearedToast.FadeOutDuration = TimeSpan.FromMilliseconds(200);

        DrawingModeHint.DisplayDuration = TimeSpan.FromSeconds(3.0);
        DrawingModeHint.FadeOutDuration = TimeSpan.FromMilliseconds(500);

        ScreenshotSavedToast.DisplayDuration = TimeSpan.FromSeconds(1.5);
        ScreenshotSavedToast.FadeOutDuration = TimeSpan.FromMilliseconds(300);

        ClearCanvasConfirmation.Confirmed += ClearCanvasConfirmation_OnConfirmed;
        ClearCanvasConfirmation.Cancelled += ClearCanvasConfirmation_OnCancelled;

        // Per-monitor overlays require manual positioning.
        // If the window is Maximized, WPF will maximize it on a single monitor (typically primary).
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowState = WindowState.Normal;

        // Size this overlay to either a specific monitor or the full virtual desktop
        if (screenBounds.HasValue)
        {
            Left = screenBounds.Value.Left;
            Top = screenBounds.Value.Top;
            Width = screenBounds.Value.Width;
            Height = screenBounds.Value.Height;
        }
        else
        {
            // Fallback: virtual desktop spanning all monitors
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        _logger.LogInformation("Overlay dimensions - Left:{Left} Top:{Top} Size:{Width}x{Height}",
            Left, Top, Width, Height);
        _logger.LogDebug("Topmost={Topmost}, AllowsTransparency={AllowsTransparency}",
            Topmost, AllowsTransparency);

        // Wire up mouse events
        MouseLeftButtonDown += OverlayWindow_MouseLeftButtonDown;
        MouseLeftButtonUp += OverlayWindow_MouseLeftButtonUp;
        MouseMove += OverlayWindow_MouseMove;
        MouseRightButtonDown += OverlayWindow_MouseRightButtonDown;
        MouseWheel += OverlayWindow_MouseWheel;

        // Add additional event handlers for debugging
        Loaded += (s, e) => _logger.LogDebug("Loaded event fired");
        IsVisibleChanged += (s, e) => _logger.LogDebug("IsVisibleChanged ? {IsVisible}", IsVisible);

        // Ensure timers are stopped when window is closed
        Closed += (s, e) =>
        {
            _appSettings.BrushColorChanged -= AppSettings_BrushColorChanged;
            _appSettings.BrushThicknessChanged -= AppSettings_BrushThicknessChanged;
        };

        _logger.LogDebug("Mouse events wired up");
    }

    private void AppSettings_BrushColorChanged(object? sender, string colorHex)
    {
        try
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => AppSettings_BrushColorChanged(sender, colorHex));
                return;
            }

            if (!_isDrawing || _activeTool == null)
            {
                return;
            }

            _activeTool.OnColorChanged(colorHex);
            UpdateCursor();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply brush color change to overlay");
        }
    }

    private void AppSettings_BrushThicknessChanged(object? sender, double thickness)
    {
        try
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => AppSettings_BrushThicknessChanged(sender, thickness));
                return;
            }

            if (!_isDrawing || _activeTool == null)
            {
                return;
            }

            _activeTool.OnThicknessChanged(thickness);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply brush thickness change to overlay");
        }
    }

    public void EnableDrawing()
    {
        _logger.LogInformation("?? Drawing enabled");

        // Clear canvas FIRST, before any visibility changes
        ClearDrawing();

        _isDrawing = true;

        // Initialize active tool based on current settings
        var settings = _appSettings.CurrentSettings;
        _activeTool = settings.ActiveTool switch
        {
            DrawTool.Pen => _penTool,
            DrawTool.Line => _lineTool,
            DrawTool.Arrow => _arrowTool,
            DrawTool.Eraser => _eraserTool,
            DrawTool.Rectangle => _rectangleTool,
            DrawTool.Circle => _circleTool,
            DrawTool.Text => _textTool,
            _ => _penTool
        };

        _activeTool.OnActivated();
        _activeTool.OnColorChanged(settings.ActiveBrush);
        _activeTool.OnThicknessChanged(settings.BrushThickness);

        UpdateCursor();

        // Show the drawing mode hint
        ShowDrawingModeHint();

        _logger.LogDebug("IsHitTestVisible={IsHitTestVisible}, Focusable={Focusable}",
            IsHitTestVisible, Focusable);
    }

    public void DisableDrawing()
    {
        DisableDrawingInternal(clearHistory: true);
    }

    public void DisableDrawingInternal(bool clearHistory)
    {
        _logger.LogInformation("?? Drawing disabled");
        _isDrawing = false;

        // Deactivate current tool and cancel any in-progress operations
        if (_activeTool != null)
        {
            _activeTool.Cancel(DrawingCanvas);
            _activeTool.OnDeactivated();
        }

        // Hide all indicators, toasts, and modals
        HideThicknessIndicator();
        HideCanvasClearedToast();
        HideDrawingModeHint();
        HideHelpPopup();
        HideScreenshotSavedToast();
        HideClearCanvasModal();

        // Clear canvas when exiting drawing mode too
        ClearDrawing();

        if (clearHistory)
        {
            // Clear history when exiting drawing mode
            _drawingHistory.Clear();
        }

        this.Cursor = WpfCursors.Arrow;
    }

    private void UpdateCursor()
    {
        try
        {
            var settings = _appSettings.CurrentSettings;

            this.Cursor = settings.ActiveTool switch
            {
                DrawTool.Pen => _cursorHelper.CreateColoredPencilCursor(settings.ActiveBrush),
                DrawTool.Line => _cursorHelper.CreateLineCursor(settings.ActiveBrush),
                DrawTool.Arrow => _cursorHelper.CreateArrowCursor(settings.ActiveBrush),
                DrawTool.Eraser => _cursorHelper.CreateEraserCursor(),
                DrawTool.Rectangle => _cursorHelper.CreateRectangleCursor(settings.ActiveBrush),
                DrawTool.Circle => _cursorHelper.CreateCircleCursor(settings.ActiveBrush),
                DrawTool.Text => WpfCursors.IBeam,
                _ => WpfCursors.Cross
            };

            _logger.LogDebug("Updated cursor for tool {Tool} with color {Color}", settings.ActiveTool, settings.ActiveBrush);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cursor, using default");
            this.Cursor = WpfCursors.Pen;
        }
    }

    private void OverlayWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _logger.LogDebug("MouseLeftButtonDown - isDrawing:{IsDrawing}", _isDrawing);
        if (_isDrawing && _activeTool != null)
        {
            var position = e.GetPosition(DrawingCanvas);
            _activeTool.OnMouseDown(position, DrawingCanvas);
        }
    }

    private void OverlayWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _logger.LogDebug("MouseLeftButtonUp - isDrawing:{IsDrawing}", _isDrawing);

        if (_isDrawing && _activeTool != null)
        {
            var position = e.GetPosition(DrawingCanvas);
            _activeTool.OnMouseUp(position, DrawingCanvas);
        }
    }

    private void OverlayWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDrawing && _activeTool != null)
        {
            var position = e.GetPosition(DrawingCanvas);
            _activeTool.OnMouseMove(position, DrawingCanvas, e.LeftButton);
        }
    }

    private void OverlayWindow_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isDrawing)
        {
            try
            {
                // Cycle to next color in palette
                string newColor = _appSettings.GetNextColor();
                _logger.LogInformation("Color cycled to {Color} via right-click", newColor);

                // Update cursor to reflect new color
                UpdateCursor();

                // Notify active tool of color change
                if (_activeTool != null)
                {
                    _activeTool.OnColorChanged(newColor);
                }

                // Prevent context menu from appearing
                e.Handled = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cycle color");
            }
        }
    }

    private void OverlayWindow_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_isDrawing)
        {
            try
            {
                var settings = _appSettings.CurrentSettings;

                // Adjust thickness based on wheel delta (typically 120 units per notch)
                double adjustment = e.Delta > 0 ? 1.0 : -1.0;
                double newThickness = settings.BrushThickness + adjustment;

                // Clamp to min/max
                newThickness = Math.Max(settings.MinBrushThickness,
                                       Math.Min(settings.MaxBrushThickness, newThickness));

                _appSettings.SetBrushThickness(newThickness);
                _logger.LogInformation("Brush thickness adjusted to {Thickness} via mouse wheel", newThickness);

                // Notify active tool of thickness change
                if (_activeTool != null)
                {
                    _activeTool.OnThicknessChanged(newThickness);
                }

                // Show thickness indicator
                ShowThicknessIndicator(newThickness);

                // Mark event as handled
                e.Handled = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to adjust brush thickness");
            }
        }
    }

    /// <summary>
    /// Called when the active tool changes
    /// </summary>
    public void OnToolChanged(DrawTool newTool)
    {
        try
        {
            // Deactivate current tool and cancel any in-progress operations
            if (_activeTool != null)
            {
                _activeTool.Cancel(DrawingCanvas);
                _activeTool.OnDeactivated();
            }

            // Switch to new tool
            _activeTool = newTool switch
            {
                DrawTool.Pen => _penTool,
                DrawTool.Line => _lineTool,
                DrawTool.Arrow => _arrowTool,
                DrawTool.Eraser => _eraserTool,
                DrawTool.Rectangle => _rectangleTool,
                DrawTool.Circle => _circleTool,
                DrawTool.Text => _textTool,
                _ => _penTool
            };

            // Activate new tool
            var settings = _appSettings.CurrentSettings;
            _activeTool.OnActivated();
            _activeTool.OnColorChanged(settings.ActiveBrush);
            _activeTool.OnThicknessChanged(settings.BrushThickness);

            // Update cursor for new tool
            UpdateCursor();

            _logger.LogInformation("Tool changed to {Tool}", newTool);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle tool change");
        }
    }

    private void ClearDrawing()
    {
        int childCount = DrawingCanvas.Children.Count;
        if (childCount > 0)
        {
            _logger.LogDebug("Clearing {ChildCount} strokes from canvas", childCount);
        }
        DrawingCanvas.Children.Clear();
    }

    /// <summary>
    /// Shows the thickness indicator with the specified value and resets the fade timer
    /// </summary>
    /// <param name="thickness">The current brush thickness value</param>
    private void ShowThicknessIndicator(double thickness)
    {
        try
        {
            ThicknessIndicator.Show(thickness);
            _logger.LogDebug("Thickness indicator shown: {Thickness} px", thickness);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show thickness indicator");
        }
    }

    /// <summary>
    /// Immediately hides the thickness indicator (called when drawing mode is disabled)
    /// </summary>
    private void HideThicknessIndicator()
    {
        try
        {
            ThicknessIndicator.HideImmediate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide thickness indicator");
        }
    }

    /// <summary>
    /// Public method to clear the canvas and show visual feedback (called via R key)
    /// </summary>
    public void ClearCanvas()
    {
        ClearCanvasInternal(clearHistory: true);
    }

    public void ClearCanvasInternal(bool clearHistory)
    {
        try
        {
            int strokeCount = DrawingCanvas.Children.Count;

            if (strokeCount > 0)
            {
                _logger.LogInformation("Clearing {StrokeCount} strokes from canvas via R key", strokeCount);
                ClearDrawing();
            }
            else
            {
                _logger.LogDebug("Canvas already empty, clear acknowledged");
            }

            if (clearHistory)
            {
                // Clear history when canvas is cleared
                _drawingHistory.Clear();
            }

            // Always show feedback so user knows R key was recognized
            ShowCanvasClearedToast();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear canvas");
        }
    }

    public void HideClearCanvasConfirmation()
    {
        try
        {
            if (!_isClearCanvasModalVisible)
            {
                return;
            }

            HideClearCanvasModal();

            // Clear callbacks to prevent memory leaks
            _clearCanvasConfirmCallback = null;
            _clearCanvasCancelCallback = null;

            RestoreToolStateAfterModal();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force-hide clear canvas confirmation modal");
        }
    }

    public bool TryRemoveElementById(Guid elementId)
    {
        try
        {
            for (int i = DrawingCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (DrawingCanvas.Children[i] is FrameworkElement fe && fe.Tag is Guid id && id == elementId)
                {
                    DrawingCanvas.Children.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove element by id from canvas");
            return false;
        }
    }

    #region Canvas Cleared Toast

    /// <summary>
    /// Shows the "Canvas Cleared" toast with animation
    /// </summary>
    private void ShowCanvasClearedToast()
    {
        try
        {
            CanvasClearedToast.Show();
            _logger.LogDebug("Canvas cleared toast shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show canvas cleared toast");
        }
    }

    /// <summary>
    /// Immediately hides the canvas cleared toast
    /// </summary>
    private void HideCanvasClearedToast()
    {
        try
        {
            CanvasClearedToast.HideImmediate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide canvas cleared toast");
        }
    }

    #endregion

    #region Drawing Mode Hint

    /// <summary>
    /// Shows the drawing mode hint with animation (displayed when entering drawing mode)
    /// </summary>
    private void ShowDrawingModeHint()
    {
        try
        {
            DrawingModeHint.Show("Press Delete to clear canvas  |  ESC to exit");
            _logger.LogDebug("Drawing mode hint shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show drawing mode hint");
        }
    }

    /// <summary>
    /// Immediately hides the drawing mode hint
    /// </summary>
    private void HideDrawingModeHint()
    {
        try
        {
            DrawingModeHint.HideImmediate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide drawing mode hint");
        }
    }

    #endregion

    #region Help Popup

    /// <summary>
    /// Toggles the help popup with all keyboard shortcuts
    /// </summary>
    public void ToggleHelp()
    {
        try
        {
            if (_isHelpVisible)
            {
                // Help is currently visible, hide it
                HideHelp();
            }
            else
            {
                // Help is currently hidden, show it
                ShowHelp();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle help popup");
        }
    }

    public void SetHelpVisible(bool visible)
    {
        try
        {
            if (visible)
            {
                if (!_isHelpVisible)
                {
                    ShowHelp();
                }
            }
            else
            {
                if (_isHelpVisible)
                {
                    HideHelp();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set help visibility");
        }
    }

    /// <summary>
    /// Shows the help popup with all keyboard shortcuts (no auto-hide)
    /// </summary>
    private void ShowHelp()
    {
        try
        {
            HelpPopup.Show();
            _isHelpVisible = true;

            _logger.LogDebug("Help popup shown (toggle mode)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show help popup");
        }
    }

    /// <summary>
    /// Hides the help popup with fade-out animation
    /// </summary>
    private void HideHelp()
    {
        try
        {
            HelpPopup.Hide();
            _isHelpVisible = false;

            _logger.LogDebug("Help popup hidden");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide help popup");
            _isHelpVisible = false;
        }
    }

    /// <summary>
    /// Immediately hides the help popup (called when drawing mode is disabled)
    /// </summary>
    private void HideHelpPopup()
    {
        try
        {
            HelpPopup.HideImmediate();
            _isHelpVisible = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide help popup");
        }
    }

    #endregion

    #region ESC Key Handling

    /// <summary>
    /// Handles ESC key press. If help or confirmation modal is visible, only closes them. Otherwise, signals to exit drawing mode.
    /// </summary>
    /// <returns>True if ESC should exit drawing mode, False if it only closed help or modal</returns>
    public bool HandleEscapeKey()
    {
        try
        {
            // Check if confirmation modal is visible first (highest priority)
            if (_isClearCanvasModalVisible)
            {
                _logger.LogDebug("ESC pressed while confirmation modal visible - canceling clear");
                HideClearCanvasModal();
                _clearCanvasCancelCallback?.Invoke();

                // Clear callbacks to prevent memory leaks
                _clearCanvasConfirmCallback = null;
                _clearCanvasCancelCallback = null;

                RestoreToolStateAfterModal();
                return false; // Don't exit drawing mode
            }
            else if (_isHelpVisible)
            {
                // Help is visible - only hide help, don't exit drawing mode
                _logger.LogDebug("ESC pressed while help visible - closing help only");
                HideHelp();
                return false; // Don't exit drawing mode
            }
            else
            {
                // Help is not visible - exit drawing mode
                _logger.LogDebug("ESC pressed - exiting drawing mode");
                return true; // Exit drawing mode
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle ESC key");
            // On error, always exit drawing mode for safety
            return true;
        }
    }

    #endregion

    #region Screenshot Saved Toast

    /// <summary>
    /// Shows the "Screenshot Saved" toast with animation
    /// </summary>
    public void ShowScreenshotSaved()
    {
        try
        {
            ScreenshotSavedToast.Show("Screenshot saved", string.Empty);
            _logger.LogDebug("Screenshot saved toast shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show screenshot saved toast");
        }
    }

    /// <summary>
    /// Immediately hides the screenshot saved toast
    /// </summary>
    private void HideScreenshotSavedToast()
    {
        try
        {
            ScreenshotSavedToast.HideImmediate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide screenshot saved toast");
        }
    }

    #endregion

    #region Clear Canvas Confirmation Modal

    private Action? _clearCanvasConfirmCallback;
    private Action? _clearCanvasCancelCallback;
    private IDrawingTool? _toolBeforeModal;
    private bool _wasDrawingBeforeModal;
    private bool _isClearCanvasModalVisible;

    /// <summary>
    /// Shows the clear canvas confirmation modal
    /// </summary>
    public void ShowClearCanvasConfirmation(Action onConfirm, Action onCancel)
    {
        try
        {
            _logger.LogInformation("Showing clear canvas confirmation modal");

            // Store callbacks
            _clearCanvasConfirmCallback = onConfirm;
            _clearCanvasCancelCallback = onCancel;

            // Store current tool and drawing state
            _toolBeforeModal = _activeTool;
            _wasDrawingBeforeModal = _isDrawing;

            // Cancel any in-progress drawing
            CancelActiveToolSafely();

            // Temporarily disable drawing and reset cursor
            _isDrawing = false;
            this.Cursor = WpfCursors.Arrow;

            // Show the modal
            ClearCanvasModalHost.Visibility = Visibility.Visible;
            ClearCanvasConfirmation.Show();
            _isClearCanvasModalVisible = true;

            _logger.LogDebug("Clear canvas confirmation modal shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show clear canvas confirmation modal");
            // On error, call cancel callback to restore state
            onCancel?.Invoke();
        }
    }

    /// <summary>
    /// Handles confirmation
    /// </summary>
    private void ClearCanvasConfirmation_OnConfirmed(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("User confirmed clear canvas");

            // Hide the modal
            HideClearCanvasModal();

            // Call the confirm callback
            _clearCanvasConfirmCallback?.Invoke();

            // Clear callbacks to prevent memory leaks
            _clearCanvasConfirmCallback = null;
            _clearCanvasCancelCallback = null;

            // Restore tool state
            RestoreToolStateAfterModal();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle Yes button click");
            // Ensure modal is hidden and state is restored
            HideClearCanvasModal();
            _clearCanvasConfirmCallback = null;
            _clearCanvasCancelCallback = null;
            RestoreToolStateAfterModal();
        }
    }

    /// <summary>
    /// Handles cancel
    /// </summary>
    private void ClearCanvasConfirmation_OnCancelled(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("User canceled clear canvas");

            // Hide the modal
            HideClearCanvasModal();

            // Call the cancel callback
            _clearCanvasCancelCallback?.Invoke();

            // Clear callbacks to prevent memory leaks
            _clearCanvasConfirmCallback = null;
            _clearCanvasCancelCallback = null;

            // Restore tool state
            RestoreToolStateAfterModal();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle No button click");
            // Ensure modal is hidden and state is restored
            HideClearCanvasModal();
            _clearCanvasConfirmCallback = null;
            _clearCanvasCancelCallback = null;
            RestoreToolStateAfterModal();
        }
    }

    /// <summary>
    /// Hides the clear canvas confirmation modal
    /// </summary>
    private void HideClearCanvasModal()
    {
        try
        {
            ClearCanvasConfirmation.Hide();
            ClearCanvasModalHost.Visibility = Visibility.Collapsed;
            _isClearCanvasModalVisible = false;
            _logger.LogDebug("Clear canvas confirmation modal hidden");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide clear canvas confirmation modal");
        }
    }

    /// <summary>
    /// Restores the tool state after the modal is closed
    /// </summary>
    private void RestoreToolStateAfterModal()
    {
        try
        {
            // Restore drawing state
            _isDrawing = _wasDrawingBeforeModal;

            // Restore active tool
            if (_toolBeforeModal != null)
            {
                _activeTool = _toolBeforeModal;
            }

            // Restore cursor
            if (_isDrawing)
            {
                UpdateCursor();
            }

            _logger.LogDebug("Tool state restored after modal - Drawing: {IsDrawing}, Tool: {Tool}",
                _isDrawing, _activeTool?.GetType().Name ?? "null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore tool state after modal");
            // On error, ensure cursor is at least set to something reasonable
            this.Cursor = _isDrawing ? WpfCursors.Cross : WpfCursors.Arrow;
        }
    }

    /// <summary>
    /// Safely cancels the active tool if one is active
    /// </summary>
    private void CancelActiveToolSafely()
    {
        try
        {
            if (_activeTool != null)
            {
                _activeTool.Cancel(DrawingCanvas);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel active tool");
        }
    }

    #endregion

    #region Undo/Redo

    /// <summary>
    /// Handles when a tool completes a drawing action
    /// </summary>
    private void OnToolActionCompleted(object? sender, DrawingActionCompletedEventArgs e)
    {
        try
        {
            _drawingHistory.RecordAction(OverlayId, e.Element);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record action in history");
        }
    }

    /// <summary>
    /// Handles when the eraser removes an element
    /// </summary>
    private void OnElementErased(object? sender, ElementErasedEventArgs e)
    {
        try
        {
            _drawingHistory.RemoveFromHistory(e.Element);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove element from history");
        }
    }

    /// <summary>
    /// Undoes the last drawing action (called via Ctrl+Z)
    /// </summary>
    public void UndoLastAction()
    {
        try
        {
            if (_isDrawing)
            {
                var undo = _drawingHistory.UndoLastAction();
                if (undo != null)
                {
                    if (string.Equals(undo.OverlayId, OverlayId, StringComparison.OrdinalIgnoreCase))
                    {
                        DrawingCanvas.Children.Remove(undo.Element);
                        _logger.LogInformation("Undo: Removed element from canvas");
                    }
                    else
                    {
                        _logger.LogWarning("Undo: Overlay mismatch (Expected={Expected}, Actual={Actual})", OverlayId, undo.OverlayId);
                    }
                }
                else
                {
                    _logger.LogDebug("Undo: No more actions to undo");
                }
            }
            else
            {
                _logger.LogDebug("UndoLastAction ignored - drawing mode not active");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo last action");
        }
    }

    #endregion
}
