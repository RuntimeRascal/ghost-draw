using GhostDraw.Core;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace GhostDraw.Services;

/// <summary>
/// Service for loading, saving, and managing application settings
/// </summary>
public class AppSettingsService
{
    private readonly ILogger<AppSettingsService> _logger;
    private readonly string _settingsFilePath;
    private AppSettings _currentSettings;

    // Events for real-time UI updates
    public event EventHandler<string>? BrushColorChanged;
    public event EventHandler<double>? BrushThicknessChanged;
    public event EventHandler<bool>? LockDrawingModeChanged;
    public event EventHandler<(double min, double max)>? BrushThicknessRangeChanged;
    public event EventHandler<List<int>>? HotkeyChanged;
    public event EventHandler<List<string>>? ColorPaletteChanged;

    public AppSettingsService(ILogger<AppSettingsService> logger)
    {
        _logger = logger;

        // Store settings in LocalApplicationData folder
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string settingsDirectory = Path.Combine(appData, "GhostDraw");
        Directory.CreateDirectory(settingsDirectory);

        _settingsFilePath = Path.Combine(settingsDirectory, "settings.json");
        _logger.LogInformation("Settings file path: {SettingsPath}", _settingsFilePath);

        // Load or create default settings
        _currentSettings = LoadSettings();
    }

    /// <summary>
    /// Gets the current settings (read-only copy)
    /// </summary>
    public AppSettings CurrentSettings => _currentSettings.Clone();

    /// <summary>
    /// Loads settings from disk, or creates default settings if file doesn't exist
    /// </summary>
    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("Loading settings from {Path}", _settingsFilePath);
                string json = File.ReadAllText(_settingsFilePath);
                
                // Try to deserialize as current format
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings != null)
                {
                    // Migrate old settings format if needed
                    settings = MigrateSettings(json, settings);
                    
                    _logger.LogInformation("Settings loaded successfully");
                    _logger.LogDebug("Active Brush: {Color}, Thickness: {Thickness}, Hotkey: {Hotkey}",
                        settings.ActiveBrush, settings.BrushThickness, settings.HotkeyDisplayName);
                    
                    // Save migrated settings to update file format
                    SaveSettings(settings);
                    return settings;
                }
            }

            _logger.LogInformation("Settings file not found or invalid, creating default settings");
            var defaultSettings = new AppSettings();
            SaveSettings(defaultSettings);
            return defaultSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}, using defaults", _settingsFilePath);
            return new AppSettings();
        }
    }

    /// <summary>
    /// Migrates settings from old format to new format
    /// </summary>
    private AppSettings MigrateSettings(string json, AppSettings settings)
    {
        try
        {
            // Parse as JsonDocument to check for old properties
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Migrate old "brushColor" to new "activeBrush"
            if (root.TryGetProperty("brushColor", out var brushColorProp))
            {
                var oldBrushColor = brushColorProp.GetString();
                if (!string.IsNullOrEmpty(oldBrushColor))
                {
                    settings.ActiveBrush = oldBrushColor;
                    _logger.LogInformation("Migrated 'brushColor' to 'activeBrush': {Color}", oldBrushColor);
                }
            }

            // Future migrations can be added here
            // Example: if (root.TryGetProperty("oldProperty", out var oldProp)) { ... }

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to migrate settings, using loaded values as-is");
        }

        return settings;
    }

    /// <summary>
    /// Saves settings to disk
    /// </summary>
    private void SaveSettings(AppSettings settings)
    {
        try
        {
            _logger.LogDebug("Saving settings to {Path}", _settingsFilePath);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsFilePath, json);

            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsFilePath);
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
        _logger.LogDebug("Brush thickness range saved to disk: Min={Min}, Max={Max}", min, max);

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
    /// Resets all settings to default values
    /// </summary>
    public void ResetToDefaults()
    {
        _logger.LogInformation("Resetting all settings to defaults");
        _currentSettings = new AppSettings();
        SaveSettings(_currentSettings);
    }
}
