using System.Text.Json.Serialization;

namespace GhostDraw.Core;

/// <summary>
/// Available drawing tools
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DrawTool
{
    /// <summary>
    /// Freehand drawing tool (default)
    /// </summary>
    Pen,

    /// <summary>
    /// Straight line tool - click two points to draw a line
    /// </summary>
    Line,

    /// <summary>
    /// Eraser tool - removes drawing objects underneath the cursor
    /// </summary>
    Eraser,

    /// <summary>
    /// Rectangle tool - click two points to draw a rectangle
    /// </summary>
    Rectangle,

    /// <summary>
    /// Circle tool - click two points to draw a circle/ellipse
    /// </summary>
    Circle,

    /// <summary>
    /// Arrow tool - click two points to draw a line with an arrowhead
    /// </summary>
    Arrow,

    /// <summary>
    /// Text tool - click to start typing, click outside to commit
    /// </summary>
    Text
}
