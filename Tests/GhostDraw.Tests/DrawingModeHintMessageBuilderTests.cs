using System;
using GhostDraw.Core;
using GhostDraw.Helpers;

namespace GhostDraw.Tests;

public class DrawingModeHintMessageBuilderTests
{
    [Fact]
    public void Build_LockMode_UsesPressHotkeyMessage()
    {
        var message = DrawingModeHintMessageBuilder.Build(DrawTool.Line, new[] { 0x11, 0x12, 0x58 }, true);

        var expected = string.Join(Environment.NewLine,
            "Current tool: Line",
            "Press F1 for help",
            "Press Delete to clear canvas",
            "Press Esc or Ctrl + Alt + X to exit draw mode");

        Assert.Equal(expected, message);
    }

    [Fact]
    public void Build_HoldMode_UsesReleaseHotkeyMessage()
    {
        var message = DrawingModeHintMessageBuilder.Build(DrawTool.Text, new[] { 0x10, 0x20 }, false);

        var expected = string.Join(Environment.NewLine,
            "Current tool: Text",
            "Press F1 for help",
            "Press Delete to clear canvas",
            "Press Esc or release Shift + Space to exit draw mode");

        Assert.Equal(expected, message);
    }
}
