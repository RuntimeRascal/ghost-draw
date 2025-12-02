using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using GhostDraw.Core;
using GhostDraw.Helpers;
using GhostDraw.Services;
using Microsoft.Extensions.Logging;
using WpfCursors = System.Windows.Input.Cursors;

namespace GhostDraw.Views;

public partial class OverlayWindow : Window
{
    private readonly ILogger<OverlayWindow> _logger;
    private readonly AppSettingsService _appSettings;
    private readonly CursorHelper _cursorHelper;
    private Polyline? _currentStroke;
    private bool _isDrawing = false;

    // Line tool state
    private Line? _currentLine;
    private System.Windows.Point? _lineStartPoint;
    private bool _isCreatingLine = false;

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

    // Help popup animation
    private readonly DispatcherTimer _helpPopupTimer;
    private readonly TimeSpan _helpDisplayDuration = TimeSpan.FromSeconds(4.0);
    private readonly TimeSpan _helpFadeOutDuration = TimeSpan.FromMilliseconds(500);

    public OverlayWindow(ILogger<OverlayWindow> logger, AppSettingsService appSettings, CursorHelper cursorHelper)
    {
        _logger = logger;
        _appSettings = appSettings;
        _cursorHelper = cursorHelper;
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

        // Initialize help popup timer
        _helpPopupTimer = new DispatcherTimer
        {
            Interval = _helpDisplayDuration
        };
        _helpPopupTimer.Tick += HelpPopupTimer_Tick;

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
            _helpPopupTimer.Stop();
        };

        _logger.LogDebug("Mouse events wired up");
    }

    public void EnableDrawing()
    {
        _logger.LogInformation("?? Drawing enabled");

        // Clear canvas FIRST, before any visibility changes
        ClearDrawing();

        _isDrawing = true;
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

        // Cancel any in-progress line
        CancelCurrentLine();

        // Hide all indicators and toasts
        HideThicknessIndicator();
        HideCanvasClearedToast();
        HideDrawingModeHint();
        HideHelpPopup();

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
        if (_isDrawing)
        {
            var position = e.GetPosition(DrawingCanvas);
            var tool = _appSettings.CurrentSettings.ActiveTool;
            
            switch (tool)
            {
                case DrawTool.Pen:
                    _logger.LogInformation("Starting new stroke at ({X:F0}, {Y:F0})", position.X, position.Y);
                    StartNewStroke(position);
                    break;
                    
                case DrawTool.Line:
                    HandleLineToolClick(position);
                    break;
            }
        }
    }

    private void OverlayWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _logger.LogDebug("MouseLeftButtonUp - isDrawing:{IsDrawing}", _isDrawing);
        
        // Only finalize strokes for Pen tool
        // Line tool uses click-click, not click-drag
        if (_isDrawing && _appSettings.CurrentSettings.ActiveTool == DrawTool.Pen && _currentStroke != null)
        {
            _logger.LogInformation("Stroke ended with {PointCount} points", _currentStroke.Points.Count);
            _currentStroke = null;
        }
    }

    private void OverlayWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDrawing)
        {
            var position = e.GetPosition(DrawingCanvas);
            var tool = _appSettings.CurrentSettings.ActiveTool;
            
            switch (tool)
            {
                case DrawTool.Pen:
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        _logger.LogTrace("MouseMove - adding point at ({X:F0}, {Y:F0})", position.X, position.Y);
                        AddPointToStroke(position);
                    }
                    break;
                    
                case DrawTool.Line:
                    if (_isCreatingLine && _currentLine != null)
                    {
                        // Update line endpoint to follow cursor
                        _currentLine.X2 = position.X;
                        _currentLine.Y2 = position.Y;
                    }
                    break;
            }
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

                // Update in-progress line color if Line tool is active
                if (_currentLine != null)
                {
                    _currentLine.Stroke = CreateBrushFromHex(newColor);
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

                // Update in-progress line thickness if Line tool is active
                if (_currentLine != null)
                {
                    _currentLine.StrokeThickness = newThickness;
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

    private void StartNewStroke(System.Windows.Point startPoint)
    {
        _logger.LogDebug("Creating polyline stroke");

        // Get current brush settings
        var settings = _appSettings.CurrentSettings;
        System.Windows.Media.Brush strokeBrush;

        try
        {
            strokeBrush = new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.ActiveBrush));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse brush color {Color}, using default red", settings.ActiveBrush);
            strokeBrush = System.Windows.Media.Brushes.Red;
        }

        _currentStroke = new Polyline
        {
            Stroke = strokeBrush,
            StrokeThickness = settings.BrushThickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        _currentStroke.Points.Add(startPoint);
        DrawingCanvas.Children.Add(_currentStroke);
        _logger.LogDebug("Stroke added to canvas with color {Color} and thickness {Thickness}, total strokes: {StrokeCount}",
            settings.ActiveBrush, settings.BrushThickness, DrawingCanvas.Children.Count);
    }

    private void AddPointToStroke(System.Windows.Point point)
    {
        if (_currentStroke != null)
        {
            _currentStroke.Points.Add(point);
        }
    }

    private System.Windows.Media.Brush CreateBrushFromHex(string colorHex)
    {
        try
        {
            return new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse brush color {Color}, using default red", colorHex);
            return System.Windows.Media.Brushes.Red;
        }
    }

    #region Line Tool Methods

    private void HandleLineToolClick(System.Windows.Point position)
    {
        if (!_isCreatingLine)
        {
            // First click - start the line
            StartNewLine(position);
        }
        else
        {
            // Second click - finish the line
            FinishLine(position);
        }
    }

    private void StartNewLine(System.Windows.Point startPoint)
    {
        _lineStartPoint = startPoint;
        _isCreatingLine = true;
        
        var settings = _appSettings.CurrentSettings;
        var brush = CreateBrushFromHex(settings.ActiveBrush);
        
        _currentLine = new Line
        {
            X1 = startPoint.X,
            Y1 = startPoint.Y,
            X2 = startPoint.X,  // Initially same as start
            Y2 = startPoint.Y,
            Stroke = brush,
            StrokeThickness = settings.BrushThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        
        DrawingCanvas.Children.Add(_currentLine);
        _logger.LogInformation("Line started at ({X:F0}, {Y:F0})", startPoint.X, startPoint.Y);
    }

    private void FinishLine(System.Windows.Point endPoint)
    {
        if (_currentLine != null)
        {
            _currentLine.X2 = endPoint.X;
            _currentLine.Y2 = endPoint.Y;
            
            _logger.LogInformation("Line finished at ({X:F0}, {Y:F0})", endPoint.X, endPoint.Y);
        }
        
        _currentLine = null;
        _lineStartPoint = null;
        _isCreatingLine = false;
    }

    private void CancelCurrentLine()
    {
        if (_currentLine != null)
        {
            DrawingCanvas.Children.Remove(_currentLine);
            _currentLine = null;
            _lineStartPoint = null;
            _isCreatingLine = false;
            _logger.LogDebug("In-progress line cancelled");
        }
    }

    /// <summary>
    /// Called when the active tool changes
    /// </summary>
    public void OnToolChanged(DrawTool newTool)
    {
        try
        {
            // Cancel any in-progress line when switching tools
            CancelCurrentLine();
            
            // Update cursor for new tool
            UpdateCursor();
            
            _logger.LogInformation("Tool changed to {Tool}", newTool);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle tool change");
        }
    }

    #endregion

    private void ClearDrawing()
    {
        int childCount = DrawingCanvas.Children.Count;
        if (childCount > 0)
        {
            _logger.LogDebug("Clearing {ChildCount} strokes from canvas", childCount);
        }
        DrawingCanvas.Children.Clear();
        _currentStroke = null;
        _currentLine = null;
        _lineStartPoint = null;
        _isCreatingLine = false;
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
    /// Shows the help popup with all keyboard shortcuts
    /// </summary>
    public void ShowHelp()
    {
        try
        {
            // Stop any existing animations and timer
            _helpPopupTimer.Stop();
            HelpPopupBorder.BeginAnimation(OpacityProperty, null);

            // Show the help with fade-in animation
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 0.95,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            HelpPopupBorder.BeginAnimation(OpacityProperty, fadeInAnimation);

            // Start the timer for fade-out
            _helpPopupTimer.Start();

            _logger.LogDebug("Help popup shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show help popup");
        }
    }

    /// <summary>
    /// Timer tick handler that starts the fade-out animation for the help popup
    /// </summary>
    private void HelpPopupTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            _helpPopupTimer.Stop();

            // Create and start fade-out animation
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 0.95,
                To = 0.0,
                Duration = new Duration(_helpFadeOutDuration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            HelpPopupBorder.BeginAnimation(OpacityProperty, fadeOutAnimation);

            _logger.LogDebug("Help popup fade-out started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fade out help popup");
            // Ensure popup is hidden even if animation fails
            HelpPopupBorder.Opacity = 0;
        }
    }

    /// <summary>
    /// Immediately hides the help popup
    /// </summary>
    private void HideHelpPopup()
    {
        try
        {
            _helpPopupTimer.Stop();
            HelpPopupBorder.BeginAnimation(OpacityProperty, null);
            HelpPopupBorder.Opacity = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hide help popup");
        }
    }

    #endregion
}
