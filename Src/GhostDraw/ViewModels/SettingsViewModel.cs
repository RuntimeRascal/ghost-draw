using Microsoft.Extensions.Logging;
using GhostDraw.Services;
using System.Reflection;

namespace GhostDraw.ViewModels;

/// <summary>
/// ViewModel for the SettingsWindow that aggregates all required services.
/// This enables proper MVVM pattern with dependency injection while keeping
/// UserControls visible in the XAML designer.
/// </summary>
public class SettingsViewModel(
    AppSettingsService appSettings,
    LoggingSettingsService loggingSettings,
    ILoggerFactory loggerFactory)
{
    private const string DefaultVersion = "v1.0.0";

    /// <summary>
    /// Service for managing application settings (brush, hotkey, mode, etc.)
    /// </summary>
    public AppSettingsService AppSettings { get; } = appSettings;

    /// <summary>
    /// Service for managing logging configuration
    /// </summary>
    public LoggingSettingsService LoggingSettings { get; } = loggingSettings;

    /// <summary>
    /// Factory for creating loggers for child controls
    /// </summary>
    public ILoggerFactory LoggerFactory { get; } = loggerFactory;

    /// <summary>
    /// Gets the application version from the assembly
    /// </summary>
    public string Version
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : DefaultVersion;
        }
    }
}
