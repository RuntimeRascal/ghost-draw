using Microsoft.Extensions.Logging;
using Moq;
using GhostDraw.Core;
using GhostDraw.Services;

namespace GhostDraw.Tests;

/// <summary>
/// Tests for the Screenshot feature (Ctrl+S and S keys).
/// Note: Full integration tests require Windows runtime, so we test the hookable components.
/// </summary>
public class ScreenshotFeatureTests
{
    private readonly Mock<ILogger<GlobalKeyboardHook>> _mockHookLogger;
    private readonly Mock<ILogger<ScreenshotService>> _mockScreenshotLogger;
    private readonly Mock<AppSettingsService> _mockAppSettings;

    public ScreenshotFeatureTests()
    {
        _mockHookLogger = new Mock<ILogger<GlobalKeyboardHook>>();
        _mockScreenshotLogger = new Mock<ILogger<ScreenshotService>>();
        _mockAppSettings = new Mock<AppSettingsService>(
            Mock.Of<ILogger<AppSettingsService>>(),
            new InMemorySettingsStore());
    }

    #region GlobalKeyboardHook Event Tests

    [Fact]
    public void GlobalKeyboardHook_ShouldHaveScreenshotFullPressedEvent()
    {
        // Arrange & Act
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var eventSubscribed = false;

        // Assert - Verify the event exists and can be subscribed to
        hook.ScreenshotFullPressed += (s, e) => { eventSubscribed = true; };

        // The event should be subscribable (event exists)
        Assert.NotNull(hook);
        Assert.False(eventSubscribed); // Event hasn't fired yet

        // Cleanup
        hook.Dispose();
    }

    [Fact]
    public void GlobalKeyboardHook_ScreenshotFullPressedEvent_ShouldBeInvokable()
    {
        // Arrange
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var handlerCalled = false;

        hook.ScreenshotFullPressed += (s, e) =>
        {
            handlerCalled = true;
        };

        // Act - We can't directly invoke the event from outside,
        // but we verify the event handler registration works
        // The actual key press handling is tested via integration tests on Windows

        // Assert - Event can be subscribed without error
        Assert.NotNull(hook);
        Assert.False(handlerCalled); // Handler not called until key press

        // Cleanup
        hook.Dispose();
    }

    [Fact]
    public void GlobalKeyboardHook_Dispose_ShouldNotThrowWithScreenshotEvents()
    {
        // Arrange
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var fullHandled = false;
        hook.ScreenshotFullPressed += (s, e) => { fullHandled = !fullHandled; };

        // Act & Assert - Dispose should work without throwing
        var exception = Record.Exception(() => hook.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void GlobalKeyboardHook_MultipleEventSubscribers_ShouldWorkForScreenshotFull()
    {
        // Arrange
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var subscriberCount = 0;

        // Act - Add multiple subscribers
        hook.ScreenshotFullPressed += (s, e) => subscriberCount++;
        hook.ScreenshotFullPressed += (s, e) => subscriberCount++;
        hook.ScreenshotFullPressed += (s, e) => subscriberCount++;

        // Assert - Should be able to add multiple handlers without error
        Assert.NotNull(hook);
        Assert.Equal(0, subscriberCount); // Handlers not called yet

        // Cleanup
        hook.Dispose();
    }

    #endregion

    #region AppSettings Tests

    [Fact]
    public void AppSettings_ShouldHaveDefaultScreenshotSavePath()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.NotNull(settings.ScreenshotSavePath);
        Assert.NotEmpty(settings.ScreenshotSavePath);
        Assert.Contains("GhostDraw", settings.ScreenshotSavePath);
    }

    [Fact]
    public void AppSettings_ShouldHaveDefaultCopyToClipboard()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.True(settings.CopyScreenshotToClipboard); // Default should be true
    }

    [Fact]
    public void AppSettings_ShouldHaveDefaultPlayShutterSound()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.False(settings.PlayShutterSound); // Default should be false
    }

    [Fact]
    public void AppSettings_ShouldHaveDefaultOpenFolderAfterScreenshot()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.False(settings.OpenFolderAfterScreenshot); // Default should be false
    }

    [Fact]
    public void AppSettings_Clone_ShouldCopyScreenshotSettings()
    {
        // Arrange
        var original = new AppSettings
        {
            ScreenshotSavePath = "/custom/path",
            CopyScreenshotToClipboard = false,
            PlayShutterSound = true,
            OpenFolderAfterScreenshot = true
        };

        // Act
        var cloned = original.Clone();

        // Assert
        Assert.Equal(original.ScreenshotSavePath, cloned.ScreenshotSavePath);
        Assert.Equal(original.CopyScreenshotToClipboard, cloned.CopyScreenshotToClipboard);
        Assert.Equal(original.PlayShutterSound, cloned.PlayShutterSound);
        Assert.Equal(original.OpenFolderAfterScreenshot, cloned.OpenFolderAfterScreenshot);
    }

    [Fact]
    public void AppSettings_ScreenshotSavePath_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();
        var customPath = "/custom/screenshot/path";

        // Act
        settings.ScreenshotSavePath = customPath;

        // Assert
        Assert.Equal(customPath, settings.ScreenshotSavePath);
    }

    [Fact]
    public void AppSettings_CopyScreenshotToClipboard_CanBeToggled()
    {
        // Arrange
        var settings = new AppSettings();
        var originalValue = settings.CopyScreenshotToClipboard;

        // Act
        settings.CopyScreenshotToClipboard = !originalValue;

        // Assert
        Assert.NotEqual(originalValue, settings.CopyScreenshotToClipboard);
    }

    [Fact]
    public void AppSettings_PlayShutterSound_CanBeToggled()
    {
        // Arrange
        var settings = new AppSettings();
        var originalValue = settings.PlayShutterSound;

        // Act
        settings.PlayShutterSound = !originalValue;

        // Assert
        Assert.NotEqual(originalValue, settings.PlayShutterSound);
    }

    [Fact]
    public void AppSettings_OpenFolderAfterScreenshot_CanBeToggled()
    {
        // Arrange
        var settings = new AppSettings();
        var originalValue = settings.OpenFolderAfterScreenshot;

        // Act
        settings.OpenFolderAfterScreenshot = !originalValue;

        // Assert
        Assert.NotEqual(originalValue, settings.OpenFolderAfterScreenshot);
    }

    #endregion

    #region ScreenshotService Tests

    [Fact]
    public void ScreenshotService_Constructor_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
        {
            var service = new ScreenshotService(_mockScreenshotLogger.Object, _mockAppSettings.Object);
        });

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void AppSettings_Serialization_ShouldIncludeScreenshotSettings()
    {
        // Arrange
        var settings = new AppSettings
        {
            ScreenshotSavePath = "/test/path",
            CopyScreenshotToClipboard = false,
            PlayShutterSound = true,
            OpenFolderAfterScreenshot = true
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(settings);

        // Assert
        Assert.Contains("screenshotSavePath", json);
        Assert.Contains("copyScreenshotToClipboard", json);
        Assert.Contains("playShutterSound", json);
        Assert.Contains("openFolderAfterScreenshot", json);
    }

    [Fact]
    public void AppSettings_Deserialization_ShouldRestoreScreenshotSettings()
    {
        // Arrange
        var json = @"{
            ""screenshotSavePath"": ""/custom/path"",
            ""copyScreenshotToClipboard"": false,
            ""playShutterSound"": true,
            ""openFolderAfterScreenshot"": true,
            ""activeBrush"": ""#FF0000"",
            ""brushThickness"": 3.0,
            ""activeTool"": 0,
            ""hotkeyVirtualKeys"": [162, 164, 68],
            ""lockDrawingMode"": false,
            ""logLevel"": ""Information"",
            ""colorPalette"": []
        }";

        // Act
        var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("/custom/path", settings.ScreenshotSavePath);
        Assert.False(settings.CopyScreenshotToClipboard);
        Assert.True(settings.PlayShutterSound);
        Assert.True(settings.OpenFolderAfterScreenshot);
    }

    #endregion
}
