using GhostDraw.Services;
using Serilog.Events;

namespace GhostDraw
{
    public class LoggingSettingsService
    {
        private readonly AppSettingsService _appSettings;

        public LoggingSettingsService(AppSettingsService appSettings)
        {
            _appSettings = appSettings;
        }

        public LogEventLevel CurrentLevel => ServiceConfiguration.GetLogLevel();

        public void SetLogLevel(LogEventLevel level)
        {
            ServiceConfiguration.SetLogLevel(level);
            
            // Save to disk via AppSettingsService
            _appSettings.SetLogLevel(level.ToString());
        }

        public string GetLogDirectory() => ServiceConfiguration.GetLogDirectory();

        public LogEventLevel[] GetAvailableLogLevels() => new[]
        {
            LogEventLevel.Verbose,
            LogEventLevel.Debug,
            LogEventLevel.Information,
            LogEventLevel.Warning,
            LogEventLevel.Error,
            LogEventLevel.Fatal
        };

        public string GetLogLevelDisplayName(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => "Verbose (Most detailed)",
            LogEventLevel.Debug => "Debug (Detailed)",
            LogEventLevel.Information => "Information (Normal)",
            LogEventLevel.Warning => "Warning",
            LogEventLevel.Error => "Error",
            LogEventLevel.Fatal => "Fatal (Errors only)",
            _ => level.ToString()
        };
    }
}
