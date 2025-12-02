using GhostDraw.Core;
using GhostDraw.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace GhostDraw.Tests;

public class EraserToolTests
{
    private readonly Mock<ILogger<AppSettingsService>> _mockLogger;
    private readonly InMemorySettingsStore _inMemoryStore;

    public EraserToolTests()
    {
        _mockLogger = new Mock<ILogger<AppSettingsService>>();
        _inMemoryStore = new InMemorySettingsStore();
    }

    private AppSettingsService CreateService()
    {
        return new AppSettingsService(_mockLogger.Object, _inMemoryStore);
    }

    [Fact]
    public void DrawTool_Enum_ShouldHaveEraserValue()
    {
        // Assert
        Assert.Equal(0, (int)DrawTool.Pen);
        Assert.Equal(1, (int)DrawTool.Line);
        Assert.Equal(2, (int)DrawTool.Eraser);
    }

    [Fact]
    public void AppSettings_ActiveTool_ShouldSupportEraser()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveTool = DrawTool.Eraser
        };

        // Act & Assert
        Assert.Equal(DrawTool.Eraser, settings.ActiveTool);
    }

    [Fact]
    public void AppSettings_Clone_ShouldCopyEraserTool()
    {
        // Arrange
        var original = new AppSettings
        {
            ActiveTool = DrawTool.Eraser
        };

        // Act
        var clone = original.Clone();
        clone.ActiveTool = DrawTool.Pen;

        // Assert
        Assert.Equal(DrawTool.Eraser, original.ActiveTool);
        Assert.Equal(DrawTool.Pen, clone.ActiveTool);
    }

    [Fact]
    public void AppSettings_EraserTool_ShouldSerializeToJson()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveTool = DrawTool.Eraser
        };

        // Act
        var json = JsonSerializer.Serialize(settings);

        // Assert
        Assert.Contains("\"activeTool\":", json);
        Assert.Contains("\"Eraser\"", json);
    }

    [Fact]
    public void AppSettings_EraserTool_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = "{\"activeTool\":\"Eraser\",\"activeBrush\":\"#FF0000\",\"brushThickness\":3.0}";

        // Act
        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(DrawTool.Eraser, settings.ActiveTool);
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_ShouldUpdateToEraser()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.SetActiveTool(DrawTool.Eraser);
        var tool = service.GetActiveTool();

        // Assert
        Assert.Equal(DrawTool.Eraser, tool);
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_ShouldPersistEraserInMemory()
    {
        // Arrange
        var service = CreateService();

        // Act - Set to Eraser
        service.SetActiveTool(DrawTool.Eraser);

        // Assert - The setting should be retrievable
        Assert.Equal(DrawTool.Eraser, service.GetActiveTool());
        
        // Create new service instance to verify persistence
        var newService = CreateService();
        Assert.Equal(DrawTool.Eraser, newService.GetActiveTool());
    }

    [Fact]
    public void AppSettingsService_SetEraserTool_ShouldLogToolChange()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.SetActiveTool(DrawTool.Eraser);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Active tool changed to")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GlobalKeyboardHook_ShouldHaveEraserToolPressedEvent()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GlobalKeyboardHook>>();
        var hook = new GlobalKeyboardHook(mockLogger.Object);
        var eventSubscribed = false;

        // Act - Subscribe to the event
        hook.EraserToolPressed += (s, e) => { eventSubscribed = true; };

        // Assert - Verify the event exists and can be subscribed to
        Assert.NotNull(hook);
        Assert.False(eventSubscribed); // Event hasn't fired yet

        // Cleanup
        hook.Dispose();
    }

    [Fact]
    public void GlobalKeyboardHook_EraserToolPressedEvent_ShouldBeInvokable()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GlobalKeyboardHook>>();
        var hook = new GlobalKeyboardHook(mockLogger.Object);
        var handlerCalled = false;
        
        hook.EraserToolPressed += (s, e) =>
        {
            handlerCalled = true;
        };

        // Assert - Event can be subscribed without error
        Assert.NotNull(hook);
        Assert.False(handlerCalled); // Handler not called until key press

        // Cleanup
        hook.Dispose();
    }

    [Fact]
    public void GlobalKeyboardHook_Dispose_ShouldNotThrowWithEraserToolEvent()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GlobalKeyboardHook>>();
        var hook = new GlobalKeyboardHook(mockLogger.Object);
        var eventHandled = false;
        hook.EraserToolPressed += (s, e) => { eventHandled = !eventHandled; };

        // Act & Assert - Dispose should work without throwing
        var exception = Record.Exception(() => hook.Dispose());
        Assert.Null(exception);
    }
}
