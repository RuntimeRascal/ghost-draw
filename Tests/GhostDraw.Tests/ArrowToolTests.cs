using System.Text.Json;
using GhostDraw.Core;
using GhostDraw.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostDraw.Tests;

public class ArrowToolTests
{
    private readonly Mock<ILogger<AppSettingsService>> _mockLogger;
    private readonly InMemorySettingsStore _inMemoryStore;

    public ArrowToolTests()
    {
        _mockLogger = new Mock<ILogger<AppSettingsService>>();
        _inMemoryStore = new InMemorySettingsStore();
    }

    private AppSettingsService CreateService()
    {
        return new AppSettingsService(_mockLogger.Object, _inMemoryStore);
    }

    [Fact]
    public void DrawTool_Enum_ShouldIncludeArrow()
    {
        Assert.Equal(5, (int)DrawTool.Arrow);
    }

    [Fact]
    public void AppSettings_ActiveTool_CanBeSetToArrow()
    {
        var settings = new AppSettings
        {
            ActiveTool = DrawTool.Arrow
        };

        Assert.Equal(DrawTool.Arrow, settings.ActiveTool);
    }

    [Fact]
    public void AppSettings_ActiveTool_ArrowShouldSerializeToJson()
    {
        var settings = new AppSettings
        {
            ActiveTool = DrawTool.Arrow
        };

        var json = JsonSerializer.Serialize(settings);

        Assert.Contains("\"activeTool\":", json);
        Assert.Contains("\"Arrow\"", json);
    }

    [Fact]
    public void GlobalKeyboardHook_ShouldHaveArrowToolPressedEvent()
    {
        var mockLogger = new Mock<ILogger<GlobalKeyboardHook>>();
        var hook = new GlobalKeyboardHook(mockLogger.Object);
        var subscribed = false;

        hook.ArrowToolPressed += (s, e) => subscribed = true;

        Assert.NotNull(hook);
        Assert.False(subscribed);

        hook.Dispose();
    }

    [Fact]
    public void GlobalKeyboardHook_Dispose_ShouldNotThrowWithArrowToolEvent()
    {
        var mockLogger = new Mock<ILogger<GlobalKeyboardHook>>();
        var hook = new GlobalKeyboardHook(mockLogger.Object);
        hook.ArrowToolPressed += (s, e) => { };

        var exception = Record.Exception(() => hook.Dispose());
        Assert.Null(exception);
    }
}
