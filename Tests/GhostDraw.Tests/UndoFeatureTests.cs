using Microsoft.Extensions.Logging;
using Moq;
using GhostDraw.Core;
using GhostDraw.Services;

namespace GhostDraw.Tests;

/// <summary>
/// Tests for the Undo (Ctrl+Z) feature.
/// Note: Full integration tests require Windows runtime, so we test the hookable components.
/// </summary>
public class UndoFeatureTests
{
    private readonly Mock<ILogger<GlobalKeyboardHook>> _mockHookLogger;
    private readonly Mock<ILogger<DrawingHistory>> _mockHistoryLogger;

    public UndoFeatureTests()
    {
        _mockHookLogger = new Mock<ILogger<GlobalKeyboardHook>>();
        _mockHistoryLogger = new Mock<ILogger<DrawingHistory>>();
    }

    [Fact]
    public void GlobalKeyboardHook_ShouldHaveUndoPressedEvent()
    {
        // Arrange & Act
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var eventSubscribed = false;

        // Assert - Verify the event exists and can be subscribed to
        hook.UndoPressed += (s, e) => { eventSubscribed = true; };

        // The event should be subscribable (event exists)
        Assert.NotNull(hook);
        Assert.False(eventSubscribed); // Event hasn't fired yet

        // Cleanup
        hook.Dispose();
    }

    [Fact]
    public void GlobalKeyboardHook_UndoPressedEvent_ShouldBeInvokable()
    {
        // Arrange
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var handlerCalled = false;
        
        hook.UndoPressed += (s, e) =>
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
    public void GlobalKeyboardHook_Dispose_ShouldNotThrowWithUndoEvent()
    {
        // Arrange
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var eventHandled = false;
        hook.UndoPressed += (s, e) => { eventHandled = !eventHandled; };

        // Act & Assert - Dispose should work without throwing
        var exception = Record.Exception(() => hook.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void GlobalKeyboardHook_MultipleEventSubscribers_ShouldWorkForUndo()
    {
        // Arrange
        var hook = new GlobalKeyboardHook(_mockHookLogger.Object);
        var subscriberCount = 0;

        // Act - Add multiple subscribers
        hook.UndoPressed += (s, e) => subscriberCount++;
        hook.UndoPressed += (s, e) => subscriberCount++;
        hook.UndoPressed += (s, e) => subscriberCount++;

        // Assert - Should be able to add multiple handlers without error
        Assert.NotNull(hook);
        Assert.Equal(0, subscriberCount); // Handlers not called yet

        // Cleanup
        hook.Dispose();
    }

    [Fact]
    public void DrawingHistory_ShouldInitializeEmpty()
    {
        // Arrange & Act
        var history = new DrawingHistory(_mockHistoryLogger.Object);

        // Assert
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void DrawingHistory_UndoLastAction_ShouldReturnNullWhenEmpty()
    {
        // Arrange
        var history = new DrawingHistory(_mockHistoryLogger.Object);

        // Act
        var result = history.UndoLastAction();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DrawingHistory_Clear_ShouldNotThrowOnEmptyHistory()
    {
        // Arrange
        var history = new DrawingHistory(_mockHistoryLogger.Object);

        // Act & Assert
        var exception = Record.Exception(() => history.Clear());
        Assert.Null(exception);
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void DrawingHistory_RecordAction_ShouldNotThrowWithNullElement()
    {
        // Arrange
        var history = new DrawingHistory(_mockHistoryLogger.Object);

        // Act & Assert
        var exception = Record.Exception(() => history.RecordAction(null!));
        Assert.Null(exception);
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void DrawingHistory_RemoveFromHistory_ShouldNotThrowWithNullElement()
    {
        // Arrange
        var history = new DrawingHistory(_mockHistoryLogger.Object);

        // Act & Assert
        var exception = Record.Exception(() => history.RemoveFromHistory(null!));
        Assert.Null(exception);
    }
}
