using GhostDraw.Core;

namespace GhostDraw.Tests;

public class GlobalKeyboardHookTests
{
    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    public void ShouldSuppressDelete_FollowsTextSessionAndDrawing(bool isKeyDown, bool isDrawing, bool isTextSession, bool expected)
    {
        var result = GlobalKeyboardHook.ShouldSuppressDelete(isKeyDown, isDrawing, isTextSession);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, true, true, false, true)]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, false, true, false, false)]
    [InlineData(false, true, true, false, false)]
    public void ShouldSuppressCtrlS_FollowsTextSessionAndDrawing(bool isKeyDown, bool isCtrl, bool isDrawing, bool isTextSession, bool expected)
    {
        var result = GlobalKeyboardHook.ShouldSuppressCtrlS(isKeyDown, isCtrl, isDrawing, isTextSession);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, true, true, false, true)]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, false, true, false, false)]
    [InlineData(false, true, true, false, false)]
    public void ShouldSuppressCtrlZ_FollowsTextSessionAndDrawing(bool isKeyDown, bool isCtrl, bool isDrawing, bool isTextSession, bool expected)
    {
        var result = GlobalKeyboardHook.ShouldSuppressCtrlZ(isKeyDown, isCtrl, isDrawing, isTextSession);
        Assert.Equal(expected, result);
    }
}
