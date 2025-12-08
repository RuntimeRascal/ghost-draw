using GhostDraw.Core;
using GhostDraw.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace GhostDraw.Tests;

public class CircleToolTests
{
    private readonly Mock<ILogger<AppSettingsService>> _mockLogger;
    private readonly InMemorySettingsStore _inMemoryStore;

    public CircleToolTests()
    {
        _mockLogger = new Mock<ILogger<AppSettingsService>>();
        _inMemoryStore = new InMemorySettingsStore();
    }

    private AppSettingsService CreateService()
    {
        return new AppSettingsService(_mockLogger.Object, _inMemoryStore);
    }

    [Fact]
    public void DrawTool_Enum_ShouldIncludeCircle()
    {
        // Assert
        Assert.Equal(0, (int)DrawTool.Pen);
        Assert.Equal(1, (int)DrawTool.Line);
        Assert.Equal(2, (int)DrawTool.Eraser);
        Assert.Equal(3, (int)DrawTool.Rectangle);
        Assert.Equal(4, (int)DrawTool.Circle);
    }

    [Fact]
    public void AppSettings_ActiveTool_CanBeSetToCircle()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveTool = DrawTool.Circle
        };

        // Assert
        Assert.Equal(DrawTool.Circle, settings.ActiveTool);
    }

    [Fact]
    public void AppSettings_Clone_ShouldCopyCircleTool()
    {
        // Arrange
        var original = new AppSettings
        {
            ActiveTool = DrawTool.Circle
        };

        // Act
        var clone = original.Clone();
        clone.ActiveTool = DrawTool.Pen;

        // Assert
        Assert.Equal(DrawTool.Circle, original.ActiveTool);
        Assert.Equal(DrawTool.Pen, clone.ActiveTool);
    }

    [Fact]
    public void AppSettings_ActiveTool_CircleShouldSerializeToJson()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveTool = DrawTool.Circle
        };

        // Act
        var json = JsonSerializer.Serialize(settings);

        // Assert
        Assert.Contains("\"activeTool\":", json);
        Assert.Contains("\"Circle\"", json);
    }

    [Fact]
    public void AppSettings_ActiveTool_CircleShouldDeserializeFromJson()
    {
        // Arrange
        var json = "{\"activeTool\":\"Circle\",\"activeBrush\":\"#FF0000\",\"brushThickness\":3.0}";

        // Act
        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(DrawTool.Circle, settings.ActiveTool);
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_ShouldUpdateToCircle()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.SetActiveTool(DrawTool.Circle);
        var tool = service.GetActiveTool();

        // Assert
        Assert.Equal(DrawTool.Circle, tool);
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_CircleShouldPersistInMemory()
    {
        // Arrange
        var service = CreateService();

        // Act - Set to Circle
        service.SetActiveTool(DrawTool.Circle);

        // Assert - The setting should be retrievable
        Assert.Equal(DrawTool.Circle, service.GetActiveTool());
        
        // Create new service instance to verify persistence
        var newService = CreateService();
        Assert.Equal(DrawTool.Circle, newService.GetActiveTool());
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_CircleShouldLogToolChange()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.SetActiveTool(DrawTool.Circle);

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
    public void DrawTool_AllValues_ShouldBeUnique()
    {
        // Arrange
        var values = Enum.GetValues<DrawTool>();
        
        // Act
        var uniqueValues = values.Distinct().ToArray();
        
        // Assert
        Assert.Equal(values.Length, uniqueValues.Length);
    }

    [Fact]
    public void DrawTool_ShouldHaveFiveTools()
    {
        // Arrange & Act
        var toolCount = Enum.GetValues<DrawTool>().Length;
        
        // Assert
        Assert.Equal(5, toolCount);
    }

    [Fact]
    public void DrawTool_CircleShouldBeLast()
    {
        // Arrange
        var values = Enum.GetValues<DrawTool>();
        
        // Act
        var lastTool = values[values.Length - 1];
        
        // Assert
        Assert.Equal(DrawTool.Circle, lastTool);
    }
}
