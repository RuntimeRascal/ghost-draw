using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using GhostDraw.Core;
using GhostDraw.Helpers;
using GhostDraw.Services;
using GhostDraw.Tools;
using Microsoft.Extensions.Logging;
using WpfCursors = System.Windows.Input.Cursors;

namespace GhostDraw.Views;

public partial class OverlayWindow : Window
{
    private readonly ILogger<OverlayWindow> _logger;
    private readonly AppSettingsService _appSettings;
    private readonly CursorHelper _cursorHelper;
    private bool _isDrawing = false;
    private bool _isHelpVisible = false;

    // Tool instances
    private readonly PenTool _penTool;
    private readonly LineTool _lineTool;
    private readonly EraserTool _eraserTool;
    private readonly RectangleTool _rectangleTool;
    private readonly CircleTool _circleTool;
    private IDrawingTool? _activeTool;

    /// <summary>
    /// Gets the display name of the current hotkey combination for binding
    /// </summary>
    public string HotkeyDisplayName => _appSettings.CurrentSettings.HotkeyDisplayName;

    // Thickness indicator animation
    private readonly DispatcherTimer _thicknessIndicatorTimer;
    private readonly TimeSpan _indicatorDisplayDuration = TimeSpan.FromSeconds(1.5);
    private readonly TimeSpan _fadeOutDuration = TimeSpan.FromMilliseconds(300);

    // Canvas cleared toast animation
    private readonly DispatcherTimer _canvasClearedToastTimer;
    private readonly TimeSpan _toastDisplayDuration = TimeSpan.FromSeconds(1.0);
    private readonly TimeSpan _toastFadeOutDuration = TimeSpan.FromMilliseconds(200);

    // Drawing mode hint animation
    private readonly DispatcherTimer _drawingModeHintTimer;
    private readonly TimeSpan _hintDisplayDuration = TimeSpan.FromSeconds(3.0);
    private readonly TimeSpan _hintFadeOutDuration = TimeSpan.FromMilliseconds(500);

    // Help popup animation (timer removed - help now stays visible until manually toggled)
    private readonly TimeSpan _helpFadeOutDuration = TimeSpan.FromMilliseconds(500);
    private readonly TimeSpan _helpFadeInDuration = TimeSpan.FromMilliseconds(200);

    // Screenshot saved toast animation
    private readonly DispatcherTimer _screenshotSavedToastTimer;
    private readonly TimeSpan _screenshotToastDisplayDuration = TimeSpan.FromSeconds(1.5);
    private readonly TimeSpan _screenshotToastFadeOutDuration = TimeSpan.FromMilliseconds(300);

    public OverlayWindow(ILogger<OverlayWindow> logger, AppSettingsService appSettings, CursorHelper cursorHelper,
        PenTool penTool, LineTool lineTool, EraserTool eraserTool, RectangleTool rectangleTool, CircleTool circleTool)
    {
        _logger = logger;
        _appSettings = appSettings;
        _cursorHelper = cursorHelper;
        _penTool = penTool;
        _lineTool = lineTool;
        _eraserTool = eraserTool;
        _rectangleTool = rectangleTool;
        _circleTool = circleTool;
        _logger.LogDebug("OverlayWindow constructor called");

        InitializeComponent();

        // Initialize thickness indicator timer
        _thicknessIndicatorTimer = new DispatcherTimer
        {
            Interval = _indicatorDisplayDuration
        };
        _thicknessIndicatorTimer.Tick += ThicknessIndicatorTimer_Tick;

        // Initialize canvas cleared toast timer
        _canvasClearedToastTimer = new DispatcherTimer
        {
            Interval = _toastDisplayDuration
        };
        _canvasClearedToastTimer.Tick += CanvasClearedToastTimer_Tick;

        // Initialize drawing mode hint timer
        _drawingModeHintTimer = new DispatcherTimer
        {
            Interval = _hintDisplayDuration
        };
        _drawingModeHintTimer.Tick += DrawingModeHintTimer_Tick;

        // Note: Help popup timer removed - help now stays visible until manually toggled

        // Initialize screenshot saved toast timer
        _screenshotSavedToastTimer = new DispatcherTimer
        {
            Interval = _screenshotToastDisplayDuration
        };
        _screenshotSavedToastTimer.Tick += ScreenshotSavedToastTimer_Tick;

        // Make window span all monitors
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

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
            _thicknessIndicatorTimer.Stop();
            _canvasClearedToastTimer.Stop();
            _drawingModeHintTimer.Stop();
            _screenshotSavedToastTimer.Stop();
        };

        _logger.LogDebug("Mouse events wired up");
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
            DrawTool.Eraser => _eraserTool,
            DrawTool.Rectangle => _rectangleTool,
            DrawTool.Circle => _circleTool,
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
        _logger.LogInformation("?? Drawing disabled");
        _isDrawing = false;

        // Deactivate current tool and cancel any in-progress operations
        if (_activeTool != null)
        {
            _activeTool.Cancel(DrawingCanvas);
            _activeTool.OnDeactivated();
        }

        // Hide all indicators and toasts
        HideThicknessIndicator();
        HideCanvasClearedToast();
        HideDrawingModeHint();
        HideHelpPopup();
        HideScreenshotSavedToast();

        // Clear canvas when exiting drawing mode too
        ClearDrawing();

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
                DrawTool.Eraser => _cursorHelper.CreateEraserCursor(),
                DrawTool.Rectangle => _cursorHelper.CreateRectangleCursor(settings.ActiveBrush),
                DrawTool.Circle => _cursorHelper.CreateCircleCursor(settings.ActiveBrush),
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
                DrawTool.Eraser => _eraserTool,
                DrawTool.Rectangle => _rectangleTool,
                DrawTool.Circle => _circleTool,
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
            // Update the text
            ThicknessIndicatorText.Text = $"{thickness:0} px";

            // Stop any existing animations and timer
            _thicknessIndicatorTimer.Stop();
            ThicknessIndicatorBorder.BeginAnimation(OpacityProperty, null);

            // Show the indicator immediately
            ThicknessIndicatorBorder.Opacity = 1.0;

            // Start the timer for fade-out
            _thicknessIndicatorTimer.Start();

            _logger.LogDebug("Thickness indicator shown: {Thickness} px", thickness);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show thickness indicator");
        }
    }

    /// <summary>
    /// Timer tick handler that starts the fade-out animation
    /// </summary>
    private void ThicknessIndicatorTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            _thicknessIndicatorTimer.Stop();

            // Create and start fade-out animation
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(_fadeOutDuration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            ThicknessIndicatorBorder.BeginAnimation(OpacityProperty, fadeOutAnimation);

            _logger.LogDebug("Thickness indicator fade-out started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fade out thickness indicator");
            // Ensure indicator is hidden even if animation fails
            ThicknessIndicatorBorder.Opacity = 0;
        }
    }

    /// <summary>
    /// Immediately hides the thickness indicator (called when drawing mode is disabled)
    /// </summary>
    private void HideThicknessIndicator()
    {
        try
        {
            _thicknessIndicatorTimer.Stop();
            ThicknessIndicatorBorder.BeginAnimation(OpacityProperty, null);
            ThicknessIndicatorBorder.Opacity = 0;
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
            
            // Always show feedback so user knows R key was recognized
            ShowCanvasClearedToast();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear canvas");
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
            // Stop any existing animations and timer
            _canvasClearedToastTimer.Stop();
            CanvasClearedToastBorder.BeginAnimation(OpacityProperty, null);

            // Show the toast immediately
            CanvasClearedToastBorder.Opacity = 1.0;

            // Start the timer for fade-out
            _canvasClearedToastTimer.Start();

            _logger.LogDebug("Canvas cleared toast shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show canvas cleared toast");
        }
    }

    /// <summary>
    /// Timer tick handler that starts the fade-out animation for the toast
    /// </summary>
    private void CanvasClearedToastTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            _canvasClearedToastTimer.Stop();

            // Create and start fade-out animation
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(_toastFadeOutDuration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            CanvasClearedToastBorder.BeginAnimation(OpacityProperty, fadeOutAnimation);

            _logger.LogDebug("Canvas cleared toast fade-out started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fade out canvas cleared toast");
            // Ensure toast is hidden even if animation fails
            CanvasClearedToastBorder.Opacity = 0;
        }
    }

    /// <summary>
    /// Immediately hides the canvas cleared toast
    /// </summary>
    private void HideCanvasClearedToast()
    {
        try
        {
            _canvasClearedToastTimer.Stop();
            CanvasClearedToastBorder.BeginAnimation(OpacityProperty, null);
            CanvasClearedToastBorder.Opacity = 0;
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
            // Stop any existing animations and timer
            _drawingModeHintTimer.Stop();
            DrawingModeHintBorder.BeginAnimation(OpacityProperty, null);

            // Show the hint with fade-in animation
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 0.8,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            DrawingModeHintBorder.BeginAnimation(OpacityProperty, fadeInAnimation);

            // Start the timer for fade-out
            _drawingModeHintTimer.Start();

            _logger.LogDebug("Drawing mode hint shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show drawing mode hint");
        }
    }

    /// <summary>
    /// Timer tick handler that starts the fade-out animation for the hint
    /// </summary>
    private void DrawingModeHintTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            _drawingModeHintTimer.Stop();

            // Create and start fade-out animation
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 0.0,
                Duration = new Duration(_hintFadeOutDuration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            DrawingModeHintBorder.BeginAnimation(OpacityProperty, fadeOutAnimation);

            _logger.LogDebug("Drawing mode hint fade-out started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fade out drawing mode hint");
            // Ensure hint is hidden even if animation fails
            DrawingModeHintBorder.Opacity = 0;
        }
    }

    /// <summary>
    /// Immediately hides the drawing mode hint
    /// </summary>
    private void HideDrawingModeHint()
    {
        try
        {
            _drawingModeHintTimer.Stop();
            DrawingModeHintBorder.BeginAnimation(OpacityProperty, null);
            DrawingModeHintBorder.Opacity = 0;
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

    /// <summary>
    /// Shows the help popup with all keyboard shortcuts (no auto-hide)
    /// </summary>
    private void ShowHelp()
    {
        try
        {
            // Stop any existing animations
            HelpPopupBorder.BeginAnimation(OpacityProperty, null);

            // Show the help with fade-in animation
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 0.95,
                Duration = new Duration(_helpFadeInDuration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            HelpPopupBorder.BeginAnimation(OpacityProperty, fadeInAnimation);
            
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
            // Stop any existing animations
            HelpPopupBorder.BeginAnimation(OpacityProperty, null);

            // Create and start fade-out animation
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 0.95,
                To = 0.0,
                Duration = new Duration(_helpFadeOutDuration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            HelpPopupBorder.BeginAnimation(OpacityProperty, fadeOutAnimation);
            
            _isHelpVisible = false;

            _logger.LogDebug("Help popup hidden");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide help popup");
            // Ensure popup is hidden even if animation fails
            HelpPopupBorder.Opacity = 0;
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
            HelpPopupBorder.BeginAnimation(OpacityProperty, null);
            HelpPopupBorder.Opacity = 0;
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
    /// Handles ESC key press. If help is visible, only closes help. Otherwise, signals to exit drawing mode.
    /// </summary>
    /// <returns>True if ESC should exit drawing mode, False if it only closed help</returns>
    public bool HandleEscapeKey()
    {
        try
        {
            if (_isHelpVisible)
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
            // Stop any existing animations and timer
            _screenshotSavedToastTimer.Stop();
            ScreenshotSavedToastBorder.BeginAnimation(OpacityProperty, null);

            // Show the toast immediately
            ScreenshotSavedToastBorder.Opacity = 1.0;

            // Start the timer for fade-out
            _screenshotSavedToastTimer.Start();

            _logger.LogDebug("Screenshot saved toast shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show screenshot saved toast");
        }
    }

    /// <summary>
    /// Timer tick handler that starts the fade-out animation for the screenshot toast
    /// </summary>
    private void ScreenshotSavedToastTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            _screenshotSavedToastTimer.Stop();

            // Create and start fade-out animation
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(_screenshotToastFadeOutDuration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            ScreenshotSavedToastBorder.BeginAnimation(OpacityProperty, fadeOutAnimation);

            _logger.LogDebug("Screenshot saved toast fade-out started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fade out screenshot saved toast");
            // Ensure toast is hidden even if animation fails
            ScreenshotSavedToastBorder.Opacity = 0;
        }
    }

    /// <summary>
    /// Immediately hides the screenshot saved toast
    /// </summary>
    private void HideScreenshotSavedToast()
    {
        try
        {
            _screenshotSavedToastTimer.Stop();
            ScreenshotSavedToastBorder.BeginAnimation(OpacityProperty, null);
            ScreenshotSavedToastBorder.Opacity = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide screenshot saved toast");
        }
    }

    #endregion
}
