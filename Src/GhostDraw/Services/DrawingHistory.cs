using System.Windows;
using Microsoft.Extensions.Logging;

namespace GhostDraw.Services;

/// <summary>
/// Manages the history of completed drawing actions for undo functionality.
/// Uses a stable GUID identifier on each drawable element's Tag property.
/// </summary>
public class DrawingHistory
{
    private readonly ILogger<DrawingHistory> _logger;

    // Stack of completed drawing actions (most recent at the top)
    private readonly Stack<HistoryEntry> _undoStack = new();

    // Dictionary for O(1) lookup when eraser needs to remove history entries
    private readonly Dictionary<Guid, HistoryEntry> _elementIdToEntry = new();

    public DrawingHistory(ILogger<DrawingHistory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a completed drawing action. Assigns a unique ID to the element.
    /// </summary>
    /// <param name="element">The UIElement that was added to the canvas</param>
    public void RecordAction(UIElement element)
    {
        try
        {
            if (element == null)
            {
                _logger.LogWarning("Attempted to record null element");
                return;
            }

            // Assign a unique ID to this element using Tag (cast to FrameworkElement)
            if (element is FrameworkElement frameworkElement)
            {
                var id = Guid.NewGuid();
                frameworkElement.Tag = id;

                var entry = new HistoryEntry(id, element);
                _undoStack.Push(entry);
                _elementIdToEntry[id] = entry;

                _logger.LogDebug("Action recorded: ID={Id}, Type={Type}, StackSize={StackSize}",
                    id, element.GetType().Name, _undoStack.Count);
            }
            else
            {
                _logger.LogWarning("Element is not a FrameworkElement, cannot assign Tag");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record action");
        }
    }

    /// <summary>
    /// Removes the most recent completed action from history.
    /// Returns the element to be removed from the canvas, or null if history is empty.
    /// </summary>
    public UIElement? UndoLastAction()
    {
        try
        {
            while (_undoStack.Count > 0)
            {
                var entry = _undoStack.Pop();

                // Remove from dictionary
                _elementIdToEntry.Remove(entry.Id);

                // Check if element is still valid (not already removed)
                if (!entry.IsRemoved)
                {
                    _logger.LogInformation("Undo: Removing element ID={Id}, Type={Type}, RemainingActions={Count}",
                        entry.Id, entry.Element.GetType().Name, _undoStack.Count);
                    return entry.Element;
                }
                else
                {
                    // Element was already removed (e.g., by eraser), skip to next
                    _logger.LogDebug("Undo: Skipping removed element ID={Id}", entry.Id);
                }
            }

            _logger.LogDebug("Undo: History stack is empty");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo last action");
            return null;
        }
    }

    /// <summary>
    /// Removes an element from the history (called when eraser deletes it).
    /// This ensures erased elements can never be restored by undo.
    /// </summary>
    /// <param name="element">The element that was erased</param>
    public void RemoveFromHistory(UIElement element)
    {
        try
        {
            if (element is FrameworkElement frameworkElement && frameworkElement.Tag is Guid id)
            {
                if (_elementIdToEntry.TryGetValue(id, out var entry))
                {
                    // Mark the entry as removed so it will be skipped during undo
                    entry.IsRemoved = true;
                    _elementIdToEntry.Remove(id);

                    _logger.LogDebug("Element removed from history: ID={Id}, Type={Type}",
                        id, element.GetType().Name);
                }
                else
                {
                    _logger.LogDebug("Element not found in history dictionary: ID={Id}", id);
                }
            }
            else
            {
                _logger.LogDebug("Element has no GUID tag, cannot remove from history");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove element from history");
        }
    }

    /// <summary>
    /// Clears all history (called when canvas is cleared or drawing mode exits)
    /// </summary>
    public void Clear()
    {
        try
        {
            var count = _undoStack.Count;
            _undoStack.Clear();
            _elementIdToEntry.Clear();

            if (count > 0)
            {
                _logger.LogInformation("History cleared: {Count} entries removed", count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear history");
        }
    }

    /// <summary>
    /// Returns the number of actions that can be undone
    /// </summary>
    public int Count => _undoStack.Count;

    /// <summary>
    /// Represents a single action in the history
    /// </summary>
    private class HistoryEntry
    {
        public Guid Id { get; }
        public UIElement Element { get; }
        public bool IsRemoved { get; set; }

        public HistoryEntry(Guid id, UIElement element)
        {
            Id = id;
            Element = element;
            IsRemoved = false;
        }
    }
}
