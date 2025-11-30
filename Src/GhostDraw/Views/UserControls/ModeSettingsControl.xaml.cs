using System.Windows;
using GhostDraw.Services;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GhostDraw.Views.UserControls;

public partial class ModeSettingsControl : WpfUserControl
{
    private int _updateNestingLevel = 0;

    // DependencyProperty for AppSettings
    public static readonly DependencyProperty AppSettingsProperty =
        DependencyProperty.Register(
            nameof(AppSettings),
            typeof(AppSettingsService),
            typeof(ModeSettingsControl),
            new PropertyMetadata(null, OnAppSettingsChanged));

    public AppSettingsService? AppSettings
    {
        get => (AppSettingsService?)GetValue(AppSettingsProperty);
        set => SetValue(AppSettingsProperty, value);
    }

    private static void OnAppSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModeSettingsControl control && e.NewValue is AppSettingsService appSettings)
        {
            control.Initialize(appSettings);
        }
    }

    public ModeSettingsControl()
    {
        InitializeComponent();
    }

    private void Initialize(AppSettingsService appSettings)
    {
        LoadSettings(appSettings);
        appSettings.LockDrawingModeChanged += OnLockDrawingModeChanged;
        Unloaded += (s, e) => appSettings.LockDrawingModeChanged -= OnLockDrawingModeChanged;
    }

    private void OnLockDrawingModeChanged(object? sender, bool isLocked)
    {
        Dispatcher.Invoke(() =>
        {
            _updateNestingLevel++;
            try
            {
                if (LockModeCheckBox != null)
                    LockModeCheckBox.IsChecked = isLocked;
            }
            finally
            {
                _updateNestingLevel--;
            }
        });
    }

    private void LoadSettings(AppSettingsService appSettings)
    {
        var settings = appSettings.CurrentSettings;
        LockModeCheckBox.IsChecked = settings.LockDrawingMode;
    }

    private void LockModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_updateNestingLevel == 0 && LockModeCheckBox.IsChecked.HasValue && AppSettings != null)
        {
            AppSettings.SetLockDrawingMode(LockModeCheckBox.IsChecked.Value);
        }
    }
}
