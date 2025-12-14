using System;
using System.Collections.Generic;
using System.Linq;
using GhostDraw.Core;

namespace GhostDraw.Helpers;

/// <summary>
/// Builds the user-facing text shown in the drawing mode hint popup.
/// </summary>
public static class DrawingModeHintMessageBuilder
{
    public static string Build(DrawTool activeTool, IReadOnlyList<int> hotkeyKeys, bool isLockMode)
    {
        var toolName = GetToolDisplayName(activeTool);
        var hotkeyDisplayName = VirtualKeyHelper.GetCombinationDisplayName(hotkeyKeys?.ToList() ?? new List<int>());
        var exitInstruction = isLockMode
            ? $"Press Esc or {hotkeyDisplayName} to exit draw mode"
            : $"Press Esc or release {hotkeyDisplayName} to exit draw mode";

        return string.Join(Environment.NewLine,
            $"Current tool: {toolName}",
            "Press F1 for help",
            "Press Delete to clear canvas",
            exitInstruction);
    }

    private static string GetToolDisplayName(DrawTool tool) => tool switch
    {
        DrawTool.Pen => "Pen",
        DrawTool.Line => "Line",
        DrawTool.Arrow => "Arrow",
        DrawTool.Eraser => "Eraser",
        DrawTool.Rectangle => "Rectangle",
        DrawTool.Circle => "Circle",
        DrawTool.Text => "Text",
        _ => tool.ToString()
    };
}
