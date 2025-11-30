using System.Windows;
using System.Windows.Controls;
using Serilog.Events;
using GhostDraw.Services;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GhostDraw.Views.UserControls;

public partial class LoggingSettingsControl : WpfUserControl
{
    // DependencyProperty for LoggingSettings
    public static readonly DependencyProperty LoggingSettingsProperty =
        DependencyProperty.Register(
            nameof(LoggingSettings),
            typeof(LoggingSettingsService),
            typeof(LoggingSettingsControl),
            new PropertyMetadata(null, OnLoggingSettingsChanged));

    public LoggingSettingsService? LoggingSettings
    {
        get => (LoggingSettingsService?)GetValue(LoggingSettingsProperty);
        set => SetValue(LoggingSettingsProperty, value);
    }

    private static void OnLoggingSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoggingSettingsControl control && e.NewValue is LoggingSettingsService loggingSettings)
        {
            control.LoadSettings(loggingSettings);
        }
    }

    public LoggingSettingsControl()
    {
        InitializeComponent();
    }

    private void LoadSettings(LoggingSettingsService loggingSettings)
    {
        foreach (var level in LoggingSettingsService.GetAvailableLogLevels())
        {
            LogLevelComboBox.Items.Add(new LogLevelItem
            {
                Level = level,
                DisplayName = LoggingSettingsService.GetLogLevelDisplayName(level)
            });
        }

        var currentLevel = loggingSettings.CurrentLevel;
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
        if (LogLevelComboBox.SelectedItem is LogLevelItem selectedItem && LoggingSettings != null)
        {
            LoggingSettings.SetLogLevel(selectedItem.Level);
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
        if (LoggingSettings == null) return;

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", LoggingSettings.GetLogDirectory());
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
