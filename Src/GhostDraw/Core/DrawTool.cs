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
    Line
}
