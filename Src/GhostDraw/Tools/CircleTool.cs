using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using Point = System.Windows.Point;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace GhostDraw.Tools;

/// <summary>
/// Circle drawing tool - click two points to draw a circle/ellipse
/// Uses bounding box approach (two opposite corners define the ellipse)
/// Hold Shift for perfect circles
/// </summary>
public class CircleTool(ILogger<CircleTool> logger) : IDrawingTool
{
    private readonly ILogger<CircleTool> _logger = logger;
    private Ellipse? _currentCircle;
    private Point? _circleStartPoint;
    private bool _isCreatingCircle = false;
    private string _currentColor = "#FF0000";
    private double _currentThickness = 3.0;

    public void OnMouseDown(Point position, Canvas canvas)
    {
        if (!_isCreatingCircle)
        {
            // First click - start the circle
            StartNewCircle(position, canvas);
        }
        else
        {
            // Second click - finish the circle
            FinishCircle(position);
        }
    }

    public void OnMouseMove(Point position, Canvas canvas, MouseButtonState leftButtonState)
    {
        if (_isCreatingCircle && _currentCircle != null && _circleStartPoint.HasValue)
        {
            // Update circle dimensions to follow cursor
            // Check if Shift key is held down for perfect circle
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            UpdateCircle(_circleStartPoint.Value, position, isShiftPressed);
        }
    }

    public void OnMouseUp(Point position, Canvas canvas)
    {
        // Circle tool uses click-click, not click-drag, so no action on mouse up
    }

    public void OnActivated()
    {
        _logger.LogDebug("Circle tool activated");
    }

    public void OnDeactivated()
    {
        _logger.LogDebug("Circle tool deactivated");
        if (_currentCircle != null)
        {
            _currentCircle = null;
            _circleStartPoint = null;
            _isCreatingCircle = false;
        }
    }

    public void OnColorChanged(string colorHex)
    {
        _currentColor = colorHex;

        // Update in-progress circle color if one exists
        if (_currentCircle != null)
        {
            _currentCircle.Stroke = CreateBrushFromHex(colorHex);
        }

        _logger.LogDebug("Circle color changed to {Color}", colorHex);
    }

    public void OnThicknessChanged(double thickness)
    {
        _currentThickness = thickness;

        // Update in-progress circle thickness if one exists
        if (_currentCircle != null)
        {
            _currentCircle.StrokeThickness = thickness;
        }

        _logger.LogDebug("Circle thickness changed to {Thickness}", thickness);
    }

    private void StartNewCircle(Point startPoint, Canvas canvas)
    {
        _circleStartPoint = startPoint;
        _isCreatingCircle = true;

        var brush = CreateBrushFromHex(_currentColor);

        _currentCircle = new Ellipse
        {
            Stroke = brush,
            StrokeThickness = _currentThickness,
            Fill = Brushes.Transparent // Outline only, no fill
        };

        // Set initial position and size (will be updated on mouse move)
        Canvas.SetLeft(_currentCircle, startPoint.X);
        Canvas.SetTop(_currentCircle, startPoint.Y);
        _currentCircle.Width = 0;
        _currentCircle.Height = 0;

        canvas.Children.Add(_currentCircle);
        _logger.LogInformation("Circle started at ({X:F0}, {Y:F0})", startPoint.X, startPoint.Y);
    }

    private void UpdateCircle(Point startPoint, Point currentPoint, bool isPerfectCircle)
    {
        if (_currentCircle == null)
            return;

        // Calculate the top-left corner and dimensions using bounding box approach
        double left = Math.Min(startPoint.X, currentPoint.X);
        double top = Math.Min(startPoint.Y, currentPoint.Y);
        double width = Math.Abs(currentPoint.X - startPoint.X);
        double height = Math.Abs(currentPoint.Y - startPoint.Y);

        // If Shift is held, make it a perfect circle (width = height = max dimension)
        if (isPerfectCircle)
        {
            double maxDimension = Math.Max(width, height);
            
            // Adjust left/top based on which direction we're dragging
            if (currentPoint.X < startPoint.X)
                left = startPoint.X - maxDimension;
            if (currentPoint.Y < startPoint.Y)
                top = startPoint.Y - maxDimension;
            
            width = maxDimension;
            height = maxDimension;
        }

        Canvas.SetLeft(_currentCircle, left);
        Canvas.SetTop(_currentCircle, top);
        _currentCircle.Width = width;
        _currentCircle.Height = height;
    }

    private void FinishCircle(Point endPoint)
    {
        if (_currentCircle != null && _circleStartPoint.HasValue)
        {
            // Check if Shift was held on the final click
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            UpdateCircle(_circleStartPoint.Value, endPoint, isShiftPressed);
            _logger.LogInformation("Circle finished at ({X:F0}, {Y:F0})", endPoint.X, endPoint.Y);
        }

        _currentCircle = null;
        _circleStartPoint = null;
        _isCreatingCircle = false;
    }

    private Brush CreateBrushFromHex(string colorHex)
    {
        try
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(colorHex));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse brush color {Color}, using default red", colorHex);
            return Brushes.Red;
        }
    }

    public void Cancel(Canvas canvas)
    {
        if (_currentCircle != null)
        {
            canvas.Children.Remove(_currentCircle);
            _currentCircle = null;
            _circleStartPoint = null;
            _isCreatingCircle = false;
            _logger.LogDebug("In-progress circle cancelled and removed");
        }
    }
}
