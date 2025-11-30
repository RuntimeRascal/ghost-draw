using System.Windows;
using System.Windows.Controls;
using Serilog.Events;
using GhostDraw.Services;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GhostDraw.Views.UserControls;

public partial class LoggingSettingsControl : WpfUserControl
{
    private readonly LoggingSettingsService _loggingSettings = null!;

    public LoggingSettingsControl()
    {
        InitializeComponent();
    }

    public LoggingSettingsControl(LoggingSettingsService loggingSettings)
    {
        _loggingSettings = loggingSettings;
        InitializeComponent();

        LoadSettings();
    }

    private void LoadSettings()
    {
        foreach (var level in LoggingSettingsService.GetAvailableLogLevels())
        {
            LogLevelComboBox.Items.Add(new LogLevelItem
            {
                Level = level,
                DisplayName = LoggingSettingsService.GetLogLevelDisplayName(level)
            });
        }

        var currentLevel = _loggingSettings.CurrentLevel;
        foreach (LogLevelItem item in LogLevelComboBox.Items)
        {
            if (item.Level == currentLevel)
            {
                LogLevelComboBox.SelectedItem = item;
                break;
            }
        }

        UpdateLogLevelDescription(currentLevel);
    }

    private void LogLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LogLevelComboBox.SelectedItem is LogLevelItem selectedItem)
        {
            _loggingSettings.SetLogLevel(selectedItem.Level);
            UpdateLogLevelDescription(selectedItem.Level);
        }
    }

    private void UpdateLogLevelDescription(LogEventLevel level)
    {
        LogLevelDescription.Text = level switch
        {
            LogEventLevel.Verbose => "Shows all logs including very detailed trace information. Use for deep debugging.",
            LogEventLevel.Debug => "Shows detailed debugging information. Good for troubleshooting issues.",
            LogEventLevel.Information => "Shows normal operational messages. Recommended for regular use.",
            LogEventLevel.Warning => "Shows only warnings and errors. Minimal logging.",
            LogEventLevel.Error => "Shows only errors and critical failures.",
            LogEventLevel.Fatal => "Shows only critical failures that cause application termination.",
            _ => string.Empty
        };
    }

    private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", _loggingSettings.GetLogDirectory());
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to open log folder:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private class LogLevelItem
    {
        public LogEventLevel Level { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public override string ToString() => DisplayName;
    }
}
