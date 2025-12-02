using GhostDraw.Core;

namespace GhostDraw.Services;

/// <summary>
/// Abstraction for settings persistence, allowing different implementations (file-based, in-memory, etc.)
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Loads settings from the store
    /// </summary>
    /// <returns>Loaded settings, or null if not found</returns>
    AppSettings? Load();

    /// <summary>
    /// Saves settings to the store
    /// </summary>
    /// <param name="settings">Settings to save</param>
    void Save(AppSettings settings);

    /// <summary>
    /// Gets the store location (for logging/debugging purposes)
    /// </summary>
    string Location { get; }
}
