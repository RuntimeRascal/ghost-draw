using GhostDraw.Core;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace GhostDraw.Services;

/// <summary>
/// File-based implementation of settings storage
/// </summary>
public class FileSettingsStore : ISettingsStore
{
    private readonly ILogger<FileSettingsStore> _logger;
    private readonly string _settingsFilePath;

    public FileSettingsStore(ILogger<FileSettingsStore> logger)
    {
        _logger = logger;

        // Store settings in LocalApplicationData folder
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string settingsDirectory = Path.Combine(appData, "GhostDraw");
        Directory.CreateDirectory(settingsDirectory);

        _settingsFilePath = Path.Combine(settingsDirectory, "settings.json");
        _logger.LogInformation("Settings file path: {SettingsPath}", _settingsFilePath);
    }

    public string Location => _settingsFilePath;

    public AppSettings? Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("Loading settings from {Path}", _settingsFilePath);
                string json = File.ReadAllText(_settingsFilePath);

                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings != null)
                {
                    // Migrate old settings format if needed
                    settings = MigrateSettings(json, settings);

                    _logger.LogInformation("Settings loaded successfully");
                    _logger.LogDebug("Active Brush: {Color}, Thickness: {Thickness}, Tool: {Tool}",
                        settings.ActiveBrush, settings.BrushThickness, settings.ActiveTool);

                    return settings;
                }
            }

            _logger.LogInformation("Settings file not found or invalid");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}", _settingsFilePath);
            return null;
        }
    }

    public void Save(AppSettings settings)
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
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to migrate settings, using loaded values as-is");
        }

        return settings;
    }
}
