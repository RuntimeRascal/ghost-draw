using GhostDraw.Core;

namespace GhostDraw.Services;

/// <summary>
/// In-memory implementation of settings storage for testing purposes
/// </summary>
public class InMemorySettingsStore : ISettingsStore
{
    private AppSettings? _settings;

    public string Location => "In-Memory";

    public AppSettings? Load()
    {
        return _settings?.Clone();
    }

    public void Save(AppSettings settings)
    {
        _settings = settings.Clone();
    }

    /// <summary>
    /// Clears the stored settings (useful for test cleanup)
    /// </summary>
    public void Clear()
    {
        _settings = null;
    }
}
