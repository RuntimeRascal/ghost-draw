using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using Vector = System.Windows.Vector;

namespace GhostDraw.Tools;

/// <summary>
/// Arrow drawing tool - click two points to draw a line with an arrowhead at the end.
/// </summary>
public class ArrowTool(ILogger<ArrowTool> logger) : IDrawingTool
{
    private readonly ILogger<ArrowTool> _logger = logger;
    private Path? _currentPath;
    private Point? _startPoint;
    private bool _isCreatingArrow;
    private string _currentColor = "#FF0000";
    private double _currentThickness = 3.0;

    public event EventHandler<DrawingActionCompletedEventArgs>? ActionCompleted;

    public void OnMouseDown(Point position, Canvas canvas)
    {
        if (!_isCreatingArrow)
        {
            StartNewArrow(position, canvas);
        }
        else
        {
            FinishArrow(position);
        }
    }

    public void OnMouseMove(Point position, Canvas canvas, MouseButtonState leftButtonState)
    {
        if (_isCreatingArrow && _currentPath != null && _startPoint.HasValue)
        {
            _currentPath.Data = BuildArrowGeometry(_startPoint.Value, position, _currentThickness);
        }
    }

    public void OnMouseUp(Point position, Canvas canvas)
    {
        // Arrow tool uses click-click, not click-drag
    }

    public void OnActivated()
    {
        _logger.LogDebug("Arrow tool activated");
    }

    public void OnDeactivated()
    {
        _logger.LogDebug("Arrow tool deactivated");
        _currentPath = null;
        _startPoint = null;
        _isCreatingArrow = false;
    }

    public void OnColorChanged(string colorHex)
    {
        _currentColor = colorHex;

        if (_currentPath != null)
        {
            var brush = CreateBrushFromHex(colorHex);
            _currentPath.Stroke = brush;
            _currentPath.Fill = brush;
        }

        _logger.LogDebug("Arrow color changed to {Color}", colorHex);
    }

    public void OnThicknessChanged(double thickness)
    {
        _currentThickness = thickness;

        if (_currentPath != null)
        {
            _currentPath.StrokeThickness = thickness;

            if (_startPoint.HasValue)
            {
                // Rebuild geometry so arrowhead scales with thickness
                var endPoint = GetCurrentEndPoint(_currentPath) ?? _startPoint.Value;
                _currentPath.Data = BuildArrowGeometry(_startPoint.Value, endPoint, thickness);
            }
        }

        _logger.LogDebug("Arrow thickness changed to {Thickness}", thickness);
    }

    public void Cancel(Canvas canvas)
    {
        if (_currentPath != null)
        {
            canvas.Children.Remove(_currentPath);
            _currentPath = null;
            _startPoint = null;
            _isCreatingArrow = false;
            _logger.LogDebug("In-progress arrow cancelled and removed");
        }
    }

    private void StartNewArrow(Point startPoint, Canvas canvas)
    {
        _startPoint = startPoint;
        _isCreatingArrow = true;

        var brush = CreateBrushFromHex(_currentColor);

        _currentPath = new Path
        {
            Stroke = brush,
            Fill = brush,
            StrokeThickness = _currentThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Data = BuildArrowGeometry(startPoint, startPoint, _currentThickness)
        };

        canvas.Children.Add(_currentPath);
        _logger.LogInformation("Arrow started at ({X:F0}, {Y:F0})", startPoint.X, startPoint.Y);
    }

    private void FinishArrow(Point endPoint)
    {
        if (_currentPath != null && _startPoint.HasValue)
        {
            _currentPath.Data = BuildArrowGeometry(_startPoint.Value, endPoint, _currentThickness);
            _logger.LogInformation("Arrow finished at ({X:F0}, {Y:F0})", endPoint.X, endPoint.Y);

            ActionCompleted?.Invoke(this, new DrawingActionCompletedEventArgs(_currentPath));
        }

        _currentPath = null;
        _startPoint = null;
        _isCreatingArrow = false;
    }

    private Geometry BuildArrowGeometry(Point start, Point end, double thickness)
    {
        var group = new GeometryGroup
        {
            FillRule = FillRule.Nonzero
        };

        // Shaft
        group.Children.Add(new LineGeometry(start, end));

        // Arrowhead
        var delta = end - start;
        if (delta.Length < 0.001)
        {
            return group;
        }

        Vector dir = delta;
        dir.Normalize();

        // Perpendicular vector
        var perp = new Vector(-dir.Y, dir.X);

        double arrowLength = Math.Max(12.0, thickness * 4.0);
        double arrowWidth = Math.Max(8.0, thickness * 3.0);

        var tip = end;
        var baseCenter = end - dir * arrowLength;
        var left = baseCenter + perp * (arrowWidth / 2.0);
        var right = baseCenter - perp * (arrowWidth / 2.0);

        var figure = new PathFigure
        {
            StartPoint = tip,
            IsClosed = true,
            IsFilled = true
        };
        figure.Segments.Add(new LineSegment(left, true));
        figure.Segments.Add(new LineSegment(right, true));

        group.Children.Add(new PathGeometry(new[] { figure }));

        return group;
    }

    private Brush CreateBrushFromHex(string colorHex)
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse brush color {Color}, using default red", colorHex);
            return Brushes.Red;
        }
    }

    private static Point? GetCurrentEndPoint(Path path)
    {
        if (path.Data is GeometryGroup group)
        {
            var line = group.Children.OfType<LineGeometry>().FirstOrDefault();
            return line?.EndPoint;
        }

        return null;
    }
}
