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
/// Straight line drawing tool - click two points to draw a line
/// </summary>
public class LineTool(ILogger<LineTool> logger) : IDrawingTool
{
    private readonly ILogger<LineTool> _logger = logger;
    private Line? _currentLine;
    private Point? _lineStartPoint;
    private bool _isCreatingLine = false;
    private string _currentColor = "#FF0000";
    private double _currentThickness = 3.0;

    public event EventHandler<DrawingActionCompletedEventArgs>? ActionCompleted;

    public void OnMouseDown(Point position, Canvas canvas)
    {
        if (!_isCreatingLine)
        {
            // First click - start the line
            StartNewLine(position, canvas);
        }
        else
        {
            // Second click - finish the line
            FinishLine(position);
        }
    }

    public void OnMouseMove(Point position, Canvas canvas, MouseButtonState leftButtonState)
    {
        if (_isCreatingLine && _currentLine != null)
        {
            // Update line endpoint to follow cursor
            _currentLine.X2 = position.X;
            _currentLine.Y2 = position.Y;
        }
    }

    public void OnMouseUp(Point position, Canvas canvas)
    {
        // Line tool uses click-click, not click-drag, so no action on mouse up
    }

    public void OnActivated()
    {
        _logger.LogDebug("Line tool activated");
    }

    public void OnDeactivated()
    {
        _logger.LogDebug("Line tool deactivated");
        if (_currentLine != null)
        {
            _currentLine = null;
            _lineStartPoint = null;
            _isCreatingLine = false;
        }
    }

    public void OnColorChanged(string colorHex)
    {
        _currentColor = colorHex;

        // Update in-progress line color if one exists
        if (_currentLine != null)
        {
            _currentLine.Stroke = CreateBrushFromHex(colorHex);
        }

        _logger.LogDebug("Line color changed to {Color}", colorHex);
    }

    public void OnThicknessChanged(double thickness)
    {
        _currentThickness = thickness;

        // Update in-progress line thickness if one exists
        if (_currentLine != null)
        {
            _currentLine.StrokeThickness = thickness;
        }

        _logger.LogDebug("Line thickness changed to {Thickness}", thickness);
    }

    private void StartNewLine(Point startPoint, Canvas canvas)
    {
        _lineStartPoint = startPoint;
        _isCreatingLine = true;

        var brush = CreateBrushFromHex(_currentColor);

        _currentLine = new Line
        {
            X1 = startPoint.X,
            Y1 = startPoint.Y,
            X2 = startPoint.X,  // Initially same as start
            Y2 = startPoint.Y,
            Stroke = brush,
            StrokeThickness = _currentThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        canvas.Children.Add(_currentLine);
        _logger.LogInformation("Line started at ({X:F0}, {Y:F0})", startPoint.X, startPoint.Y);
    }

    private void FinishLine(Point endPoint)
    {
        if (_currentLine != null)
        {
            _currentLine.X2 = endPoint.X;
            _currentLine.Y2 = endPoint.Y;

            _logger.LogInformation("Line finished at ({X:F0}, {Y:F0})", endPoint.X, endPoint.Y);
            
            // Fire ActionCompleted event for history tracking
            ActionCompleted?.Invoke(this, new DrawingActionCompletedEventArgs(_currentLine));
        }

        _currentLine = null;
        _lineStartPoint = null;
        _isCreatingLine = false;
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
        if (_currentLine != null)
        {
            canvas.Children.Remove(_currentLine);
            _currentLine = null;
            _lineStartPoint = null;
            _isCreatingLine = false;
            _logger.LogDebug("In-progress line cancelled and removed");
        }
    }
}
