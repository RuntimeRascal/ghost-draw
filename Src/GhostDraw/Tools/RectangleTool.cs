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
/// Rectangle drawing tool - click two points to draw a rectangle
/// </summary>
public class RectangleTool(ILogger<RectangleTool> logger) : IDrawingTool
{
    private readonly ILogger<RectangleTool> _logger = logger;
    private System.Windows.Shapes.Rectangle? _currentRectangle;
    private Point? _rectangleStartPoint;
    private bool _isCreatingRectangle = false;
    private string _currentColor = "#FF0000";
    private double _currentThickness = 3.0;

    public event EventHandler<DrawingActionCompletedEventArgs>? ActionCompleted;

    public void OnMouseDown(Point position, Canvas canvas)
    {
        if (!_isCreatingRectangle)
        {
            // First click - start the rectangle
            StartNewRectangle(position, canvas);
        }
        else
        {
            // Second click - finish the rectangle
            FinishRectangle(position);
        }
    }

    public void OnMouseMove(Point position, Canvas canvas, MouseButtonState leftButtonState)
    {
        if (_isCreatingRectangle && _currentRectangle != null && _rectangleStartPoint.HasValue)
        {
            // Update rectangle dimensions to follow cursor
            // Check if Shift key is held down for perfect square
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            UpdateRectangle(_rectangleStartPoint.Value, position, isShiftPressed);
        }
    }

    public void OnMouseUp(Point position, Canvas canvas)
    {
        // Rectangle tool uses click-click, not click-drag, so no action on mouse up
    }

    public void OnActivated()
    {
        _logger.LogDebug("Rectangle tool activated");
    }

    public void OnDeactivated()
    {
        _logger.LogDebug("Rectangle tool deactivated");
        if (_currentRectangle != null)
        {
            _currentRectangle = null;
            _rectangleStartPoint = null;
            _isCreatingRectangle = false;
        }
    }

    public void OnColorChanged(string colorHex)
    {
        _currentColor = colorHex;

        // Update in-progress rectangle color if one exists
        if (_currentRectangle != null)
        {
            _currentRectangle.Stroke = CreateBrushFromHex(colorHex);
        }

        _logger.LogDebug("Rectangle color changed to {Color}", colorHex);
    }

    public void OnThicknessChanged(double thickness)
    {
        _currentThickness = thickness;

        // Update in-progress rectangle thickness if one exists
        if (_currentRectangle != null)
        {
            _currentRectangle.StrokeThickness = thickness;
        }

        _logger.LogDebug("Rectangle thickness changed to {Thickness}", thickness);
    }

    private void StartNewRectangle(Point startPoint, Canvas canvas)
    {
        _rectangleStartPoint = startPoint;
        _isCreatingRectangle = true;

        var brush = CreateBrushFromHex(_currentColor);

        _currentRectangle = new System.Windows.Shapes.Rectangle
        {
            Stroke = brush,
            StrokeThickness = _currentThickness,
            Fill = Brushes.Transparent // Outline only, no fill
        };

        // Set initial position and size (will be updated on mouse move)
        Canvas.SetLeft(_currentRectangle, startPoint.X);
        Canvas.SetTop(_currentRectangle, startPoint.Y);
        _currentRectangle.Width = 0;
        _currentRectangle.Height = 0;

        canvas.Children.Add(_currentRectangle);
        _logger.LogInformation("Rectangle started at ({X:F0}, {Y:F0})", startPoint.X, startPoint.Y);
    }

    private void UpdateRectangle(Point startPoint, Point currentPoint, bool isPerfectSquare = false)
    {
        if (_currentRectangle == null)
            return;

        // Calculate the top-left corner and dimensions
        double left = Math.Min(startPoint.X, currentPoint.X);
        double top = Math.Min(startPoint.Y, currentPoint.Y);
        double width = Math.Abs(currentPoint.X - startPoint.X);
        double height = Math.Abs(currentPoint.Y - startPoint.Y);

        // If Shift is held, make it a perfect square (width = height = min dimension)
        // Note: Uses Math.Min (not Math.Max like CircleTool) so the square fits within
        // the dragged bounding box, making the final size more predictable and visible
        // to the user as they drag. This matches the behavior specified in the issue.
        if (isPerfectSquare)
        {
            double size = Math.Min(width, height);
            width = size;
            height = size;
        }

        Canvas.SetLeft(_currentRectangle, left);
        Canvas.SetTop(_currentRectangle, top);
        _currentRectangle.Width = width;
        _currentRectangle.Height = height;
    }

    private void FinishRectangle(Point endPoint)
    {
        if (_currentRectangle != null && _rectangleStartPoint.HasValue)
        {
            // Check if Shift was held on the final click
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            UpdateRectangle(_rectangleStartPoint.Value, endPoint, isShiftPressed);
            _logger.LogInformation("Rectangle finished at ({X:F0}, {Y:F0})", endPoint.X, endPoint.Y);
            
            // Fire ActionCompleted event for history tracking
            ActionCompleted?.Invoke(this, new DrawingActionCompletedEventArgs(_currentRectangle));
        }

        _currentRectangle = null;
        _rectangleStartPoint = null;
        _isCreatingRectangle = false;
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
        if (_currentRectangle != null)
        {
            canvas.Children.Remove(_currentRectangle);
            _currentRectangle = null;
            _rectangleStartPoint = null;
            _isCreatingRectangle = false;
            _logger.LogDebug("In-progress rectangle cancelled and removed");
        }
    }
}
