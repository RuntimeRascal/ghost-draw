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
/// Freehand drawing tool
/// </summary>
public class PenTool(ILogger<PenTool> logger) : IDrawingTool
{
    private readonly ILogger<PenTool> _logger = logger;
    private Polyline? _currentStroke;
    private string _currentColor = "#FF0000";
    private double _currentThickness = 3.0;

    public event EventHandler<DrawingActionCompletedEventArgs>? ActionCompleted;

    public void OnMouseDown(Point position, Canvas canvas)
    {
        _logger.LogInformation("Starting new stroke at ({X:F0}, {Y:F0})", position.X, position.Y);

        Brush strokeBrush;
        try
        {
            strokeBrush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(_currentColor));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse brush color {Color}, using default red", _currentColor);
            strokeBrush = Brushes.Red;
        }

        _currentStroke = new Polyline
        {
            Stroke = strokeBrush,
            StrokeThickness = _currentThickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        _currentStroke.Points.Add(position);
        canvas.Children.Add(_currentStroke);

        _logger.LogDebug("Stroke added to canvas with color {Color} and thickness {Thickness}, total strokes: {StrokeCount}",
            _currentColor, _currentThickness, canvas.Children.Count);
    }

    public void OnMouseMove(Point position, Canvas canvas, MouseButtonState leftButtonState)
    {
        if (leftButtonState == MouseButtonState.Pressed && _currentStroke != null)
        {
            _logger.LogTrace("Adding point at ({X:F0}, {Y:F0})", position.X, position.Y);
            _currentStroke.Points.Add(position);
        }
    }

    public void OnMouseUp(Point position, Canvas canvas)
    {
        if (_currentStroke != null)
        {
            _logger.LogInformation("Stroke ended with {PointCount} points", _currentStroke.Points.Count);

            // Fire ActionCompleted event for history tracking
            ActionCompleted?.Invoke(this, new DrawingActionCompletedEventArgs(_currentStroke));

            _currentStroke = null;
        }
    }

    public void OnActivated()
    {
        _logger.LogDebug("Pen tool activated");
    }

    public void OnDeactivated()
    {
        _logger.LogDebug("Pen tool deactivated");
        _currentStroke = null;
    }

    public void OnColorChanged(string colorHex)
    {
        _currentColor = colorHex;
        _logger.LogDebug("Pen color changed to {Color}", colorHex);
    }

    public void OnThicknessChanged(double thickness)
    {
        _currentThickness = thickness;
        _logger.LogDebug("Pen thickness changed to {Thickness}", thickness);
    }

    public void Cancel(Canvas canvas)
    {
        // Pen tool doesn't have in-progress operations to cancel
        _currentStroke = null;
    }
}
