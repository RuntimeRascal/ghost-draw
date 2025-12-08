using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using Point = System.Windows.Point;

namespace GhostDraw.Tools;

/// <summary>
/// Eraser tool - removes drawing objects underneath the cursor
/// </summary>
public class EraserTool(ILogger<EraserTool> logger) : IDrawingTool
{
    private readonly ILogger<EraserTool> _logger = logger;
    private bool _isErasing = false;
    private double _currentThickness = 3.0;
    private readonly HashSet<UIElement> _erasedElements = new();

    // Tolerance for parallel line detection in intersection algorithm
    private const double PARALLEL_LINE_TOLERANCE = 0.001;

    public void OnMouseDown(Point position, Canvas canvas)
    {
        _isErasing = true;
        _erasedElements.Clear();
        EraseAtPosition(position, canvas);
        _logger.LogDebug("Eraser started at ({X:F0}, {Y:F0})", position.X, position.Y);
    }

    public void OnMouseMove(Point position, Canvas canvas, MouseButtonState leftButtonState)
    {
        if (leftButtonState == MouseButtonState.Pressed && _isErasing)
        {
            EraseAtPosition(position, canvas);
        }
    }

    public void OnMouseUp(Point position, Canvas canvas)
    {
        if (_isErasing)
        {
            _logger.LogInformation("Eraser finished, removed {Count} elements", _erasedElements.Count);
            _isErasing = false;
            _erasedElements.Clear();
        }
    }

    public void OnActivated()
    {
        _logger.LogDebug("Eraser tool activated");
    }

    public void OnDeactivated()
    {
        _logger.LogDebug("Eraser tool deactivated");
        _isErasing = false;
        _erasedElements.Clear();
    }

    public void OnColorChanged(string colorHex)
    {
        // Eraser doesn't use color
    }

    public void OnThicknessChanged(double thickness)
    {
        _currentThickness = thickness;
        _logger.LogDebug("Eraser size changed to {Thickness}", thickness);
    }

    public void Cancel(Canvas canvas)
    {
        // Eraser tool doesn't have in-progress operations to cancel
        _isErasing = false;
        _erasedElements.Clear();
    }

    private void EraseAtPosition(Point position, Canvas canvas)
    {
        try
        {
            // Create a small rectangle around the eraser position for hit testing
            // Use current thickness as the eraser size
            double eraserRadius = _currentThickness / 2.0;
            Rect eraserRect = new Rect(
                position.X - eraserRadius,
                position.Y - eraserRadius,
                _currentThickness,
                _currentThickness
            );

            // Find all elements that intersect with the eraser
            // NOTE: For very complex drawings with many points, consider implementing
            // spatial indexing (e.g., quad-tree) or bounding box pre-filtering
            // for improved performance. Current implementation is O(n*m) where n is
            // number of shapes and m is points per shape.
            List<UIElement> elementsToRemove = new();

            foreach (UIElement element in canvas.Children)
            {
                // Skip if already erased in this stroke
                if (_erasedElements.Contains(element))
                    continue;

                bool shouldErase = false;

                if (element is Polyline polyline)
                {
                    // Check if any point in the polyline is within eraser radius
                    foreach (Point point in polyline.Points)
                    {
                        if (eraserRect.Contains(point))
                        {
                            shouldErase = true;
                            break;
                        }
                    }
                }
                else if (element is Line line)
                {
                    // Check if line intersects with eraser rect
                    Point lineStart = new Point(line.X1, line.Y1);
                    Point lineEnd = new Point(line.X2, line.Y2);

                    if (eraserRect.Contains(lineStart) || eraserRect.Contains(lineEnd))
                    {
                        shouldErase = true;
                    }
                    else
                    {
                        // Check if line passes through eraser rect
                        if (LineIntersectsRect(lineStart, lineEnd, eraserRect))
                        {
                            shouldErase = true;
                        }
                    }
                }
                else if (element is System.Windows.Shapes.Rectangle rectangle)
                {
                    // Check if eraser intersects with rectangle bounds
                    double left = Canvas.GetLeft(rectangle);
                    double top = Canvas.GetTop(rectangle);
                    
                    // Handle NaN values (shouldn't happen, but be defensive)
                    if (!double.IsNaN(left) && !double.IsNaN(top) && 
                        !double.IsNaN(rectangle.Width) && !double.IsNaN(rectangle.Height))
                    {
                        Rect shapeRect = new Rect(left, top, rectangle.Width, rectangle.Height);
                        
                        if (eraserRect.IntersectsWith(shapeRect))
                        {
                            shouldErase = true;
                        }
                    }
                }
                else if (element is Ellipse ellipse)
                {
                    // Check if eraser intersects with ellipse bounds
                    double left = Canvas.GetLeft(ellipse);
                    double top = Canvas.GetTop(ellipse);
                    
                    // Handle NaN values (shouldn't happen, but be defensive)
                    if (!double.IsNaN(left) && !double.IsNaN(top) && 
                        !double.IsNaN(ellipse.Width) && !double.IsNaN(ellipse.Height))
                    {
                        Rect ellipseRect = new Rect(left, top, ellipse.Width, ellipse.Height);
                        
                        if (eraserRect.IntersectsWith(ellipseRect))
                        {
                            shouldErase = true;
                        }
                    }
                }

                if (shouldErase)
                {
                    elementsToRemove.Add(element);
                    _erasedElements.Add(element);
                }
            }

            // Remove elements outside the iteration
            foreach (var element in elementsToRemove)
            {
                canvas.Children.Remove(element);
                _logger.LogTrace("Erased element at position ({X:F0}, {Y:F0})", position.X, position.Y);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during erase operation");
        }
    }

    private bool LineIntersectsRect(Point lineStart, Point lineEnd, Rect rect)
    {
        // Simple line-rect intersection test
        // Check if line intersects any of the four edges of the rect
        Point topLeft = rect.TopLeft;
        Point topRight = rect.TopRight;
        Point bottomLeft = rect.BottomLeft;
        Point bottomRight = rect.BottomRight;

        return LinesIntersect(lineStart, lineEnd, topLeft, topRight) ||
               LinesIntersect(lineStart, lineEnd, topRight, bottomRight) ||
               LinesIntersect(lineStart, lineEnd, bottomRight, bottomLeft) ||
               LinesIntersect(lineStart, lineEnd, bottomLeft, topLeft) ||
               rect.Contains(lineStart) ||
               rect.Contains(lineEnd);
    }

    private bool LinesIntersect(Point p1, Point p2, Point p3, Point p4)
    {
        // Check if line segment p1-p2 intersects with line segment p3-p4
        double d = (p2.X - p1.X) * (p4.Y - p3.Y) - (p2.Y - p1.Y) * (p4.X - p3.X);
        if (Math.Abs(d) < PARALLEL_LINE_TOLERANCE) // Parallel or coincident
            return false;

        double t = ((p3.X - p1.X) * (p4.Y - p3.Y) - (p3.Y - p1.Y) * (p4.X - p3.X)) / d;
        double u = ((p3.X - p1.X) * (p2.Y - p1.Y) - (p3.Y - p1.Y) * (p2.X - p1.X)) / d;

        return t >= 0 && t <= 1 && u >= 0 && u <= 1;
    }
}
