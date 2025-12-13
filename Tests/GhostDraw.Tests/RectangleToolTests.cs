using System.Text.Json;
using GhostDraw.Core;
using GhostDraw.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostDraw.Tests;

public class RectangleToolTests
{
    private readonly Mock<ILogger<AppSettingsService>> _mockLogger;
    private readonly InMemorySettingsStore _inMemoryStore;

    public RectangleToolTests()
    {
        _mockLogger = new Mock<ILogger<AppSettingsService>>();
        _inMemoryStore = new InMemorySettingsStore();
    }

    private AppSettingsService CreateService()
    {
        return new AppSettingsService(_mockLogger.Object, _inMemoryStore);
    }

    [Fact]
    public void DrawTool_Enum_ShouldIncludeRectangle()
    {
        // Assert
        Assert.Equal(0, (int)DrawTool.Pen);
        Assert.Equal(1, (int)DrawTool.Line);
        Assert.Equal(2, (int)DrawTool.Eraser);
        Assert.Equal(3, (int)DrawTool.Rectangle);
    }

    [Fact]
    public void AppSettings_ActiveTool_CanBeSetToRectangle()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveTool = DrawTool.Rectangle
        };

        // Assert
        Assert.Equal(DrawTool.Rectangle, settings.ActiveTool);
    }

    [Fact]
    public void AppSettings_Clone_ShouldCopyRectangleTool()
    {
        // Arrange
        var original = new AppSettings
        {
            ActiveTool = DrawTool.Rectangle
        };

        // Act
        var clone = original.Clone();
        clone.ActiveTool = DrawTool.Pen;

        // Assert
        Assert.Equal(DrawTool.Rectangle, original.ActiveTool);
        Assert.Equal(DrawTool.Pen, clone.ActiveTool);
    }

    [Fact]
    public void AppSettings_ActiveTool_RectangleShouldSerializeToJson()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveTool = DrawTool.Rectangle
        };

        // Act
        var json = JsonSerializer.Serialize(settings);

        // Assert
        Assert.Contains("\"activeTool\":", json);
        Assert.Contains("\"Rectangle\"", json);
    }

    [Fact]
    public void AppSettings_ActiveTool_RectangleShouldDeserializeFromJson()
    {
        // Arrange
        var json = "{\"activeTool\":\"Rectangle\",\"activeBrush\":\"#FF0000\",\"brushThickness\":3.0}";

        // Act
        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(DrawTool.Rectangle, settings.ActiveTool);
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_ShouldUpdateToRectangle()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.SetActiveTool(DrawTool.Rectangle);
        var tool = service.GetActiveTool();

        // Assert
        Assert.Equal(DrawTool.Rectangle, tool);
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_RectangleShouldPersistInMemory()
    {
        // Arrange
        var service = CreateService();

        // Act - Set to Rectangle
        service.SetActiveTool(DrawTool.Rectangle);

        // Assert - The setting should be retrievable
        Assert.Equal(DrawTool.Rectangle, service.GetActiveTool());

        // Create new service instance to verify persistence
        var newService = CreateService();
        Assert.Equal(DrawTool.Rectangle, newService.GetActiveTool());
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_RectangleShouldLogToolChange()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.SetActiveTool(DrawTool.Rectangle);

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

        // Assert - Updated to 6 to include Arrow tool
        Assert.Equal(6, toolCount);
    }
}
