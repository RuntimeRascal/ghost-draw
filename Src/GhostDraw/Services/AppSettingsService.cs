using GhostDraw.Core;
using Microsoft.Extensions.Logging;

namespace GhostDraw.Services;

/// <summary>
/// Service for loading, saving, and managing application settings
/// </summary>
public class AppSettingsService
{
    private readonly ILogger<AppSettingsService> _logger;
    private readonly ISettingsStore _settingsStore;
    private AppSettings _currentSettings;

    // Events for real-time UI updates
    public event EventHandler<string>? BrushColorChanged;
    public event EventHandler<double>? BrushThicknessChanged;
    public event EventHandler<bool>? LockDrawingModeChanged;
    public event EventHandler<(double min, double max)>? BrushThicknessRangeChanged;
    public event EventHandler<List<int>>? HotkeyChanged;
    public event EventHandler<List<string>>? ColorPaletteChanged;

    public AppSettingsService(ILogger<AppSettingsService> logger, ISettingsStore settingsStore)
    {
        _logger = logger;
        _settingsStore = settingsStore;

        _logger.LogInformation("Settings store location: {Location}", _settingsStore.Location);

        // Load or create default settings
        _currentSettings = LoadSettings();
    }

    /// <summary>
    /// Gets the current settings (read-only copy)
    /// </summary>
    public AppSettings CurrentSettings => _currentSettings.Clone();

    /// <summary>
    /// Loads settings from the store, or creates default settings if not found
    /// </summary>
    private AppSettings LoadSettings()
    {
        try
        {
            var settings = _settingsStore.Load();

            if (settings != null)
            {
                _logger.LogInformation("Settings loaded successfully");

                // Save to ensure latest format
                _settingsStore.Save(settings);
                return settings;
            }

            _logger.LogInformation("Settings not found, creating default settings");
            var defaultSettings = new AppSettings();
            _settingsStore.Save(defaultSettings);
            return defaultSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves settings to the store
    /// </summary>
    private void SaveSettings(AppSettings settings)
    {
        try
        {
            _settingsStore.Save(settings);
            _logger.LogDebug("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    /// <summary>
    /// Updates active brush color and persists to disk
    /// </summary>
    public void SetActiveBrush(string colorHex)
    {
        _logger.LogInformation("Setting active brush to {Color}", colorHex);
        _currentSettings.ActiveBrush = colorHex;
        SaveSettings(_currentSettings);

        // Raise event to notify UI
        BrushColorChanged?.Invoke(this, colorHex);
    }

    /// <summary>
    /// Updates brush thickness and persists to disk
    /// </summary>
    public void SetBrushThickness(double thickness)
    {
        // Clamp to min/max
        thickness = Math.Max(_currentSettings.MinBrushThickness,
                    Math.Min(_currentSettings.MaxBrushThickness, thickness));

        _logger.LogInformation("Setting brush thickness to {Thickness}", thickness);
        _currentSettings.BrushThickness = thickness;
        SaveSettings(_currentSettings);

        // Raise event to notify UI
        BrushThicknessChanged?.Invoke(this, thickness);
    }

    /// <summary>
    /// Updates drawing mode lock setting and persists to disk
    /// </summary>
    public void SetLockDrawingMode(bool lockMode)
    {
        _logger.LogInformation("Setting lock drawing mode to {LockMode}", lockMode);
        _currentSettings.LockDrawingMode = lockMode;
        SaveSettings(_currentSettings);

        // Raise event to notify UI
        LockDrawingModeChanged?.Invoke(this, lockMode);
    }

    /// <summary>
    /// Updates min/max brush thickness settings and persists to disk
    /// </summary>
    public void SetBrushThicknessRange(double min, double max)
    {
        _logger.LogInformation("Setting brush thickness range to {Min}-{Max}", min, max);
        _currentSettings.MinBrushThickness = min;
        _currentSettings.MaxBrushThickness = max;

        // Adjust current thickness if out of range
        if (_currentSettings.BrushThickness < min)
        {
            _logger.LogInformation("Adjusting brush thickness from {Old} to {New} (below min)", _currentSettings.BrushThickness, min);
            _currentSettings.BrushThickness = min;
        }
        if (_currentSettings.BrushThickness > max)
        {
            _logger.LogInformation("Adjusting brush thickness from {Old} to {New} (above max)", _currentSettings.BrushThickness, max);
            _currentSettings.BrushThickness = max;
        }

        SaveSettings(_currentSettings);
        _logger.LogDebug("Brush thickness range saved: Min={Min}, Max={Max}", min, max);

        // Raise event to notify UI
        BrushThicknessRangeChanged?.Invoke(this, (min, max));
    }

    /// <summary>
    /// Updates the hotkey combination and persists to disk
    /// </summary>
    /// <param name="virtualKeys">List of virtual key codes for the hotkey combination</param>
    public void SetHotkey(List<int> virtualKeys)
    {
        _logger.LogInformation("Setting hotkey to VKs: {VKs} ({DisplayName})",
            string.Join(", ", virtualKeys),
            Helpers.VirtualKeyHelper.GetCombinationDisplayName(virtualKeys));

        _currentSettings.HotkeyVirtualKeys = new List<int>(virtualKeys);

        SaveSettings(_currentSettings);

        // Raise event to notify listeners (GlobalKeyboardHook)
        HotkeyChanged?.Invoke(this, virtualKeys);
    }

    /// <summary>
    /// Gets the next color in the palette (for right-click cycling)
    /// </summary>
    public string GetNextColor()
    {
        var currentIndex = _currentSettings.ColorPalette.IndexOf(_currentSettings.ActiveBrush);
        var nextIndex = (currentIndex + 1) % _currentSettings.ColorPalette.Count;
        var nextColor = _currentSettings.ColorPalette[nextIndex];

        _logger.LogDebug("Cycling color from {CurrentColor} to {NextColor}",
            _currentSettings.ActiveBrush, nextColor);

        SetActiveBrush(nextColor);
        return nextColor;
    }

    /// <summary>
    /// Updates log level setting and persists to disk
    /// </summary>
    public void SetLogLevel(string logLevel)
    {
        _logger.LogInformation("Setting log level to {LogLevel}", logLevel);
        _currentSettings.LogLevel = logLevel;
        SaveSettings(_currentSettings);
    }

    /// <summary>
    /// Adds a color to the palette
    /// </summary>
    public void AddColorToPalette(string colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
        {
            _logger.LogWarning("Attempted to add invalid color to palette");
            return;
        }

        if (_currentSettings.ColorPalette.Contains(colorHex))
        {
            _logger.LogDebug("Color {Color} already exists in palette", colorHex);
            return;
        }

        _logger.LogInformation("Adding color {Color} to palette", colorHex);
        _currentSettings.ColorPalette.Add(colorHex);
        SaveSettings(_currentSettings);
        ColorPaletteChanged?.Invoke(this, new List<string>(_currentSettings.ColorPalette));
    }

    /// <summary>
    /// Removes a color from the palette
    /// </summary>
    public void RemoveColorFromPalette(string colorHex)
    {
        if (_currentSettings.ColorPalette.Count <= 1)
        {
            _logger.LogWarning("Cannot remove last color from palette");
            return;
        }

        if (!_currentSettings.ColorPalette.Contains(colorHex))
        {
            _logger.LogDebug("Color {Color} not found in palette", colorHex);
            return;
        }

        _logger.LogInformation("Removing color {Color} from palette", colorHex);
        _currentSettings.ColorPalette.Remove(colorHex);
        SaveSettings(_currentSettings);
        ColorPaletteChanged?.Invoke(this, new List<string>(_currentSettings.ColorPalette));
    }

    /// <summary>
    /// Updates the entire color palette
    /// </summary>
    public void SetColorPalette(List<string> colors)
    {
        if (colors == null || colors.Count == 0)
        {
            _logger.LogWarning("Cannot set empty color palette");
            return;
        }

        _logger.LogInformation("Setting color palette with {Count} colors", colors.Count);
        _currentSettings.ColorPalette = new List<string>(colors);
        SaveSettings(_currentSettings);
        ColorPaletteChanged?.Invoke(this, new List<string>(_currentSettings.ColorPalette));
    }

    /// <summary>
    /// Gets the current active tool
    /// </summary>
    public DrawTool GetActiveTool() => _currentSettings.ActiveTool;

    /// <summary>
    /// Sets the active drawing tool
    /// </summary>
    public void SetActiveTool(DrawTool tool)
    {
        _currentSettings.ActiveTool = tool;
        SaveSettings(_currentSettings);
        _logger.LogInformation("Active tool changed to {Tool}", tool);
    }

    /// <summary>
    /// Toggles between Pen and Line tools
    /// </summary>
    public DrawTool ToggleTool()
    {
        var newTool = _currentSettings.ActiveTool == DrawTool.Pen
            ? DrawTool.Line
            : DrawTool.Pen;
        SetActiveTool(newTool);
        return newTool;
    }

    /// <summary>
    /// Sets the screenshot save path
    /// </summary>
    public void SetScreenshotSavePath(string path)
    {
        _logger.LogInformation("Setting screenshot save path to: {Path}", path);
        _currentSettings.ScreenshotSavePath = path;
        SaveSettings(_currentSettings);
    }

    /// <summary>
    /// Sets whether to copy screenshot to clipboard
    /// </summary>
    public void SetCopyScreenshotToClipboard(bool copyToClipboard)
    {
        _logger.LogInformation("Setting copy screenshot to clipboard: {Value}", copyToClipboard);
        _currentSettings.CopyScreenshotToClipboard = copyToClipboard;
        SaveSettings(_currentSettings);
    }

    /// <summary>
    /// Sets whether to play shutter sound on screenshot
    /// </summary>
    public void SetPlayShutterSound(bool playSound)
    {
        _logger.LogInformation("Setting play shutter sound: {Value}", playSound);
        _currentSettings.PlayShutterSound = playSound;
        SaveSettings(_currentSettings);
    }

    /// <summary>
    /// Sets whether to open folder after screenshot
    /// </summary>
    public void SetOpenFolderAfterScreenshot(bool openFolder)
    {
        _logger.LogInformation("Setting open folder after screenshot: {Value}", openFolder);
        _currentSettings.OpenFolderAfterScreenshot = openFolder;
        SaveSettings(_currentSettings);
    }

    /// <summary>
    /// Resets all settings to default values
    /// </summary>
    public void ResetToDefaults()
    {
        _logger.LogInformation("Resetting all settings to defaults");
        _currentSettings = new AppSettings();
        SaveSettings(_currentSettings);
    }
}
