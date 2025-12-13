using System.Windows.Controls;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace GhostDraw.Tools;

/// <summary>
/// Interface for drawing tools that can be used in the overlay
/// </summary>
public interface IDrawingTool
{
    /// <summary>
    /// Event fired when a drawing action is completed (e.g., stroke finished, shape placed)
    /// </summary>
    event EventHandler<DrawingActionCompletedEventArgs>? ActionCompleted;

    /// <summary>
    /// Called when the mouse left button is pressed
    /// </summary>
    void OnMouseDown(Point position, Canvas canvas);

    /// <summary>
    /// Called when the mouse moves
    /// </summary>
    void OnMouseMove(Point position, Canvas canvas, MouseButtonState leftButtonState);

    /// <summary>
    /// Called when the mouse left button is released
    /// </summary>
    void OnMouseUp(Point position, Canvas canvas);

    /// <summary>
    /// Called when the tool is activated
    /// </summary>
    void OnActivated();

    /// <summary>
    /// Called when the tool is deactivated
    /// </summary>
    void OnDeactivated();

    /// <summary>
    /// Called when the brush color changes
    /// </summary>
    void OnColorChanged(string colorHex);

    /// <summary>
    /// Called when the brush thickness changes
    /// </summary>
    void OnThicknessChanged(double thickness);

    /// <summary>
    /// Cancels any in-progress operation (e.g., line drawing)
    /// </summary>
    void Cancel(Canvas canvas);
}

/// <summary>
/// Event args for when a drawing action is completed
/// </summary>
public class DrawingActionCompletedEventArgs : EventArgs
{
    /// <summary>
    /// The UIElement that was added to the canvas
    /// </summary>
    public System.Windows.UIElement Element { get; }

    public DrawingActionCompletedEventArgs(System.Windows.UIElement element)
    {
        Element = element;
    }
}
