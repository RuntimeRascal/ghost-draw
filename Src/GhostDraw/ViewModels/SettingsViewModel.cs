using Microsoft.Extensions.Logging;
using GhostDraw.Services;

namespace GhostDraw.ViewModels;

/// <summary>
/// ViewModel for the SettingsWindow that aggregates all required services.
/// This enables proper MVVM pattern with dependency injection while keeping
/// UserControls visible in the XAML designer.
/// </summary>
public class SettingsViewModel
{
    /// <summary>
    /// Service for managing application settings (brush, hotkey, mode, etc.)
    /// </summary>
    public AppSettingsService AppSettings { get; }
    
    /// <summary>
    /// Service for managing logging configuration
    /// </summary>
    public LoggingSettingsService LoggingSettings { get; }
    
    /// <summary>
    /// Factory for creating loggers for child controls
    /// </summary>
    public ILoggerFactory LoggerFactory { get; }

    public SettingsViewModel(
        AppSettingsService appSettings,
        LoggingSettingsService loggingSettings,
        ILoggerFactory loggerFactory)
    {
        AppSettings = appSettings;
        LoggingSettings = loggingSettings;
        LoggerFactory = loggerFactory;
    }
}
