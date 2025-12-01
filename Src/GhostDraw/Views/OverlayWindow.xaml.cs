using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
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

    // Thickness indicator animation
    private readonly DispatcherTimer _thicknessIndicatorTimer;
    private readonly TimeSpan _indicatorDisplayDuration = TimeSpan.FromSeconds(1.5);
    private readonly TimeSpan _fadeOutDuration = TimeSpan.FromMilliseconds(300);

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

        // Ensure timer is stopped when window is closed
        Closed += (s, e) => _thicknessIndicatorTimer.Stop();

        _logger.LogDebug("Mouse events wired up");
    }

    public void EnableDrawing()
    {
        _logger.LogInformation("?? Drawing enabled");

        // Clear canvas FIRST, before any visibility changes
        ClearDrawing();

        _isDrawing = true;
        UpdateCursor();
        _logger.LogDebug("IsHitTestVisible={IsHitTestVisible}, Focusable={Focusable}",
            IsHitTestVisible, Focusable);
    }

    public void DisableDrawing()
    {
        _logger.LogInformation("?? Drawing disabled");
        _isDrawing = false;

        // Hide thickness indicator
        HideThicknessIndicator();

        // Clear canvas when exiting drawing mode too
        ClearDrawing();

        this.Cursor = WpfCursors.Arrow;
    }

    private void UpdateCursor()
    {
        try
        {
            var settings = _appSettings.CurrentSettings;
            this.Cursor = _cursorHelper.CreateColoredPencilCursor(settings.ActiveBrush);
            _logger.LogDebug("Updated cursor with color {Color}", settings.ActiveBrush);
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
            _logger.LogInformation("Starting new stroke at ({X:F0}, {Y:F0})", position.X, position.Y);
            StartNewStroke(position);
        }
    }

    private void OverlayWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _logger.LogDebug("MouseLeftButtonUp - isDrawing:{IsDrawing}", _isDrawing);
        if (_isDrawing && _currentStroke != null)
        {
            _logger.LogInformation("Stroke ended with {PointCount} points", _currentStroke.Points.Count);
            _currentStroke = null;
        }
    }

    private void OverlayWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDrawing && e.LeftButton == MouseButtonState.Pressed)
        {
            var position = e.GetPosition(DrawingCanvas);
            _logger.LogTrace("MouseMove - adding point at ({X:F0}, {Y:F0})", position.X, position.Y);
            AddPointToStroke(position);
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

    private void ClearDrawing()
    {
        int childCount = DrawingCanvas.Children.Count;
        if (childCount > 0)
        {
            _logger.LogDebug("Clearing {ChildCount} strokes from canvas", childCount);
        }
        DrawingCanvas.Children.Clear();
        _currentStroke = null;
    }

    private void AdjustBrushThickness(double delta)
    {
        // Modify the brush thickness, ensuring it remains within a reasonable range
        var settings = _appSettings.CurrentSettings;
        settings.BrushThickness = Math.Max(1, settings.BrushThickness + delta);

        _logger.LogInformation("Brush thickness adjusted to {Thickness}", settings.BrushThickness);

        // Update the stroke's thickness if currently drawing
        if (_currentStroke != null)
        {
            _currentStroke.StrokeThickness = settings.BrushThickness;
        }
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
}
