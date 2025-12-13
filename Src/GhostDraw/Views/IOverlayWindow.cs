using GhostDraw.Core;

namespace GhostDraw.Views;

/// <summary>
/// Abstraction over the overlay window to make drawing logic testable
/// without depending on WPF-specific implementation details.
/// </summary>
public interface IOverlayWindow
{
    bool IsVisible { get; }
    bool IsActive { get; }
    bool IsFocused { get; }

    void EnableDrawing();
    void DisableDrawing();
    void Show();
    void Hide();
    bool Activate();
    bool Focus();

    void OnToolChanged(DrawTool newTool);
    void ClearCanvas();
    void ToggleHelp();

    /// <summary>
    /// Handles ESC key press. Returns true if drawing mode should exit,
    /// false if only the help overlay was closed.
    /// </summary>
    bool HandleEscapeKey();

    void ShowScreenshotSaved();

    /// <summary>
    /// Undoes the last drawing action
    /// </summary>
    void UndoLastAction();

    /// <summary>
    /// Shows the clear canvas confirmation modal
    /// </summary>
    /// <param name="onConfirm">Action to call when user confirms (clicks Yes)</param>
    /// <param name="onCancel">Action to call when user cancels (clicks No or closes modal)</param>
    void ShowClearCanvasConfirmation(Action onConfirm, Action onCancel);
}
