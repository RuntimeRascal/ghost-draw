using GhostDraw.Core;
using GhostDraw.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace GhostDraw.Tests;

public class LineToolTests
{
    private readonly Mock<ILogger<AppSettingsService>> _mockLogger;
    private readonly InMemorySettingsStore _inMemoryStore;

    public LineToolTests()
    {
        _mockLogger = new Mock<ILogger<AppSettingsService>>();
        _inMemoryStore = new InMemorySettingsStore();
    }

    private AppSettingsService CreateService()
    {
        return new AppSettingsService(_mockLogger.Object, _inMemoryStore);
    }

    [Fact]
    public void DrawTool_Enum_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)DrawTool.Pen);
        Assert.Equal(1, (int)DrawTool.Line);
    }

    [Fact]
    public void AppSettings_ActiveTool_ShouldDefaultToPen()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal(DrawTool.Pen, settings.ActiveTool);
    }

    [Fact]
    public void AppSettings_Clone_ShouldCopyActiveTool()
    {
        // Arrange
        var original = new AppSettings
        {
            ActiveTool = DrawTool.Line
        };

        // Act
        var clone = original.Clone();
        clone.ActiveTool = DrawTool.Pen;

        // Assert
        Assert.Equal(DrawTool.Line, original.ActiveTool);
        Assert.Equal(DrawTool.Pen, clone.ActiveTool);
    }

    [Fact]
    public void AppSettings_ActiveTool_ShouldSerializeToJson()
    {
        // Arrange
        var settings = new AppSettings
        {
            ActiveTool = DrawTool.Line
        };

        // Act
        var json = JsonSerializer.Serialize(settings);

        // Assert
        Assert.Contains("\"activeTool\":", json);
        Assert.Contains("\"Line\"", json);
    }

    [Fact]
    public void AppSettings_ActiveTool_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = "{\"activeTool\":\"Line\",\"activeBrush\":\"#FF0000\",\"brushThickness\":3.0}";

        // Act
        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(DrawTool.Line, settings.ActiveTool);
    }

    [Fact]
    public void AppSettingsService_GetActiveTool_ShouldReturnCurrentTool()
    {
        // Arrange
        var service = CreateService();
        service.SetActiveTool(DrawTool.Pen);

        // Act
        var tool = service.GetActiveTool();

        // Assert
        Assert.Equal(DrawTool.Pen, tool);
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_ShouldUpdateTool()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.SetActiveTool(DrawTool.Line);
        var tool = service.GetActiveTool();

        // Assert
        Assert.Equal(DrawTool.Line, tool);
    }

    [Fact]
    public void AppSettingsService_ToggleTool_ShouldSwitchFromPenToLine()
    {
        // Arrange
        var service = CreateService();
        service.SetActiveTool(DrawTool.Pen);

        // Act
        var newTool = service.ToggleTool();

        // Assert
        Assert.Equal(DrawTool.Line, newTool);
        Assert.Equal(DrawTool.Line, service.GetActiveTool());
    }

    [Fact]
    public void AppSettingsService_ToggleTool_ShouldSwitchFromLineToPen()
    {
        // Arrange
        var service = CreateService();
        service.SetActiveTool(DrawTool.Line);

        // Act
        var newTool = service.ToggleTool();

        // Assert
        Assert.Equal(DrawTool.Pen, newTool);
        Assert.Equal(DrawTool.Pen, service.GetActiveTool());
    }

    [Fact]
    public void AppSettingsService_ToggleTool_ShouldCycleBetweenTools()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - Start with Pen (default)
        Assert.Equal(DrawTool.Pen, service.GetActiveTool());

        // Toggle to Line
        var tool1 = service.ToggleTool();
        Assert.Equal(DrawTool.Line, tool1);

        // Toggle back to Pen
        var tool2 = service.ToggleTool();
        Assert.Equal(DrawTool.Pen, tool2);

        // Toggle to Line again
        var tool3 = service.ToggleTool();
        Assert.Equal(DrawTool.Line, tool3);
    }

    [Fact]
    public void AppSettingsService_SetActiveTool_ShouldPersistInMemory()
    {
        // Arrange
        var service = CreateService();

        // Act - Set to Line
        service.SetActiveTool(DrawTool.Line);

        // Assert - The setting should be retrievable
        Assert.Equal(DrawTool.Line, service.GetActiveTool());

        // Create new service instance to verify persistence
        var newService = CreateService();
        Assert.Equal(DrawTool.Line, newService.GetActiveTool());
    }

    [Fact]
    public void AppSettingsService_ToggleTool_ShouldLogToolChange()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.ToggleTool();

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
}
