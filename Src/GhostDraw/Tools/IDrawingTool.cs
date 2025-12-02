using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GhostDraw.Tools;

/// <summary>
/// Interface for drawing tools that can be used in the overlay
/// </summary>
public interface IDrawingTool
{
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
