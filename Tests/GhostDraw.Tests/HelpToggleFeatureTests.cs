using Microsoft.Extensions.Logging;
using Moq;
using GhostDraw.Core;
using GhostDraw.Views;
using GhostDraw.Services;
using GhostDraw.Helpers;
using GhostDraw.Tools;
using GhostDraw.Managers;

namespace GhostDraw.Tests;

/// <summary>
/// Tests for the Help Toggle (F1 key) feature with conditional ESC behavior.
/// Note: Full UI integration tests require Windows runtime, so we test the core logic.
/// </summary>
public class HelpToggleFeatureTests
{
    private readonly Mock<ILogger<GlobalKeyboardHook>> _mockHookLogger;
    private readonly Mock<ILogger<OverlayWindow>> _mockOverlayLogger;
    private readonly Mock<ILogger<DrawingManager>> _mockManagerLogger;

    public HelpToggleFeatureTests()
    {
        _mockHookLogger = new Mock<ILogger<GlobalKeyboardHook>>();
        _mockOverlayLogger = new Mock<ILogger<OverlayWindow>>();
        _mockManagerLogger = new Mock<ILogger<DrawingManager>>();
    }

    [Fact]
    public void GlobalKeyboardHook_ShouldHaveHelpPressedEvent()
    {
        // Arrange & Act
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var eventSubscribed = false;

        // Assert - Verify the event exists and can be subscribed to
        hook.HelpPressed += (s, e) => { eventSubscribed = true; };

        Assert.NotNull(hook);
        Assert.False(eventSubscribed); // Event hasn't fired yet

        // Cleanup
        hook.Dispose();
    }

    [Fact]
    public void GlobalKeyboardHook_HelpPressedEvent_ShouldBeInvokable()
    {
        // Arrange
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var handlerCalled = false;
        
        hook.HelpPressed += (s, e) =>
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
    public void GlobalKeyboardHook_Dispose_ShouldNotThrowWithHelpEvent()
    {
        // Arrange
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var eventHandled = false;
        hook.HelpPressed += (s, e) => { eventHandled = !eventHandled; };

        // Act & Assert - Dispose should work without throwing
        var exception = Record.Exception(() => hook.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void GlobalKeyboardHook_MultipleEventSubscribers_ShouldWorkForHelp()
    {
        // Arrange
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var subscriberCount = 0;

        // Act - Add multiple subscribers
        hook.HelpPressed += (s, e) => subscriberCount++;
        hook.HelpPressed += (s, e) => subscriberCount++;
        hook.HelpPressed += (s, e) => subscriberCount++;

        // Assert - Should be able to add multiple handlers without error
        Assert.NotNull(hook);
        Assert.Equal(0, subscriberCount); // Handlers not called yet

        // Cleanup
        hook.Dispose();
    }

    [Fact]
    public void DrawingManager_ShouldHaveToggleHelpMethod()
    {
        // Arrange
        var mockAppSettings = new Mock<AppSettingsService>(
            Mock.Of<ILogger<AppSettingsService>>()
        );
        var mockScreenshotService = new Mock<ScreenshotService>(
            Mock.Of<ILogger<ScreenshotService>>(),
            mockAppSettings.Object
        );
        var mockOverlayWindow = new Mock<OverlayWindow>(
            _mockOverlayLogger.Object,
            mockAppSettings.Object,
            Mock.Of<CursorHelper>(),
            Mock.Of<PenTool>(),
            Mock.Of<LineTool>(),
            Mock.Of<EraserTool>(),
            Mock.Of<RectangleTool>(),
            Mock.Of<CircleTool>()
        );
        var mockKeyboardHook = new Mock<GlobalKeyboardHook>(_mockHookLogger.Object);

        // Act
        var manager = new DrawingManager(
            _mockManagerLogger.Object,
            mockOverlayWindow.Object,
            mockAppSettings.Object,
            mockScreenshotService.Object,
            mockKeyboardHook.Object
        );

        // Assert - ToggleHelp method should exist and be callable
        var exception = Record.Exception(() => manager.ToggleHelp());
        Assert.Null(exception); // Should not throw
    }

    [Fact]
    public void DrawingManager_ToggleHelp_ShouldCallOverlayToggleHelp_WhenVisible()
    {
        // Arrange
        var mockAppSettings = new Mock<AppSettingsService>(
            Mock.Of<ILogger<AppSettingsService>>()
        );
        var mockScreenshotService = new Mock<ScreenshotService>(
            Mock.Of<ILogger<ScreenshotService>>(),
            mockAppSettings.Object
        );
        var mockOverlayWindow = new Mock<OverlayWindow>(
            _mockOverlayLogger.Object,
            mockAppSettings.Object,
            Mock.Of<CursorHelper>(),
            Mock.Of<PenTool>(),
            Mock.Of<LineTool>(),
            Mock.Of<EraserTool>(),
            Mock.Of<RectangleTool>(),
            Mock.Of<CircleTool>()
        );
        var mockKeyboardHook = new Mock<GlobalKeyboardHook>(_mockHookLogger.Object);
        
        // Setup overlay as visible
        mockOverlayWindow.Setup(x => x.IsVisible).Returns(true);

        var manager = new DrawingManager(
            _mockManagerLogger.Object,
            mockOverlayWindow.Object,
            mockAppSettings.Object,
            mockScreenshotService.Object,
            mockKeyboardHook.Object
        );

        // Act
        manager.ToggleHelp();

        // Assert - ToggleHelp should be called on overlay when visible
        mockOverlayWindow.Verify(x => x.ToggleHelp(), Times.Once);
    }

    [Fact]
    public void DrawingManager_ToggleHelp_ShouldNotCallOverlayToggleHelp_WhenNotVisible()
    {
        // Arrange
        var mockAppSettings = new Mock<AppSettingsService>(
            Mock.Of<ILogger<AppSettingsService>>()
        );
        var mockScreenshotService = new Mock<ScreenshotService>(
            Mock.Of<ILogger<ScreenshotService>>(),
            mockAppSettings.Object
        );
        var mockOverlayWindow = new Mock<OverlayWindow>(
            _mockOverlayLogger.Object,
            mockAppSettings.Object,
            Mock.Of<CursorHelper>(),
            Mock.Of<PenTool>(),
            Mock.Of<LineTool>(),
            Mock.Of<EraserTool>(),
            Mock.Of<RectangleTool>(),
            Mock.Of<CircleTool>()
        );
        var mockKeyboardHook = new Mock<GlobalKeyboardHook>(_mockHookLogger.Object);
        
        // Setup overlay as not visible
        mockOverlayWindow.Setup(x => x.IsVisible).Returns(false);

        var manager = new DrawingManager(
            _mockManagerLogger.Object,
            mockOverlayWindow.Object,
            mockAppSettings.Object,
            mockScreenshotService.Object,
            mockKeyboardHook.Object
        );

        // Act
        manager.ToggleHelp();

        // Assert - ToggleHelp should NOT be called on overlay when not visible
        mockOverlayWindow.Verify(x => x.ToggleHelp(), Times.Never);
    }

    [Fact]
    public void DrawingManager_ForceDisableDrawing_ShouldCallHandleEscapeKey()
    {
        // Arrange
        var mockAppSettings = new Mock<AppSettingsService>(
            Mock.Of<ILogger<AppSettingsService>>()
        );
        var mockScreenshotService = new Mock<ScreenshotService>(
            Mock.Of<ILogger<ScreenshotService>>(),
            mockAppSettings.Object
        );
        var mockOverlayWindow = new Mock<OverlayWindow>(
            _mockOverlayLogger.Object,
            mockAppSettings.Object,
            Mock.Of<CursorHelper>(),
            Mock.Of<PenTool>(),
            Mock.Of<LineTool>(),
            Mock.Of<EraserTool>(),
            Mock.Of<RectangleTool>(),
            Mock.Of<CircleTool>()
        );
        var mockKeyboardHook = new Mock<GlobalKeyboardHook>(_mockHookLogger.Object);
        
        // Setup HandleEscapeKey to return true (exit drawing mode)
        mockOverlayWindow.Setup(x => x.HandleEscapeKey()).Returns(true);

        var manager = new DrawingManager(
            _mockManagerLogger.Object,
            mockOverlayWindow.Object,
            mockAppSettings.Object,
            mockScreenshotService.Object,
            mockKeyboardHook.Object
        );

        // Act
        manager.ForceDisableDrawing();

        // Assert - HandleEscapeKey should be called
        mockOverlayWindow.Verify(x => x.HandleEscapeKey(), Times.Once);
    }

    [Fact]
    public void DrawingManager_ForceDisableDrawing_ShouldExitDrawingMode_WhenHandleEscapeKeyReturnsTrue()
    {
        // Arrange
        var mockAppSettings = new Mock<AppSettingsService>(
            Mock.Of<ILogger<AppSettingsService>>()
        );
        var mockScreenshotService = new Mock<ScreenshotService>(
            Mock.Of<ILogger<ScreenshotService>>(),
            mockAppSettings.Object
        );
        var mockOverlayWindow = new Mock<OverlayWindow>(
            _mockOverlayLogger.Object,
            mockAppSettings.Object,
            Mock.Of<CursorHelper>(),
            Mock.Of<PenTool>(),
            Mock.Of<LineTool>(),
            Mock.Of<EraserTool>(),
            Mock.Of<RectangleTool>(),
            Mock.Of<CircleTool>()
        );
        var mockKeyboardHook = new Mock<GlobalKeyboardHook>(_mockHookLogger.Object);
        
        // Setup HandleEscapeKey to return true (help not visible - should exit)
        mockOverlayWindow.Setup(x => x.HandleEscapeKey()).Returns(true);

        var manager = new DrawingManager(
            _mockManagerLogger.Object,
            mockOverlayWindow.Object,
            mockAppSettings.Object,
            mockScreenshotService.Object,
            mockKeyboardHook.Object
        );

        // Act
        manager.ForceDisableDrawing();

        // Assert - Should call DisableDrawing and Hide when HandleEscapeKey returns true
        mockOverlayWindow.Verify(x => x.DisableDrawing(), Times.Once);
        mockOverlayWindow.Verify(x => x.Hide(), Times.Once);
        mockKeyboardHook.Verify(x => x.SetDrawingModeActive(false), Times.Once);
    }

    [Fact]
    public void DrawingManager_ForceDisableDrawing_ShouldNotExitDrawingMode_WhenHandleEscapeKeyReturnsFalse()
    {
        // Arrange
        var mockAppSettings = new Mock<AppSettingsService>(
            Mock.Of<ILogger<AppSettingsService>>()
        );
        var mockScreenshotService = new Mock<ScreenshotService>(
            Mock.Of<ILogger<ScreenshotService>>(),
            mockAppSettings.Object
        );
        var mockOverlayWindow = new Mock<OverlayWindow>(
            _mockOverlayLogger.Object,
            mockAppSettings.Object,
            Mock.Of<CursorHelper>(),
            Mock.Of<PenTool>(),
            Mock.Of<LineTool>(),
            Mock.Of<EraserTool>(),
            Mock.Of<RectangleTool>(),
            Mock.Of<CircleTool>()
        );
        var mockKeyboardHook = new Mock<GlobalKeyboardHook>(_mockHookLogger.Object);
        
        // Setup HandleEscapeKey to return false (help was visible - only close help)
        mockOverlayWindow.Setup(x => x.HandleEscapeKey()).Returns(false);

        var manager = new DrawingManager(
            _mockManagerLogger.Object,
            mockOverlayWindow.Object,
            mockAppSettings.Object,
            mockScreenshotService.Object,
            mockKeyboardHook.Object
        );

        // Act
        manager.ForceDisableDrawing();

        // Assert - Should NOT call DisableDrawing or Hide when HandleEscapeKey returns false
        mockOverlayWindow.Verify(x => x.DisableDrawing(), Times.Never);
        mockOverlayWindow.Verify(x => x.Hide(), Times.Never);
        mockKeyboardHook.Verify(x => x.SetDrawingModeActive(false), Times.Never);
    }
}
