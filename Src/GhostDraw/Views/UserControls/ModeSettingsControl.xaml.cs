using System.Windows;
using Microsoft.Extensions.Logging;
using GhostDraw.Services;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GhostDraw.Views.UserControls;

public partial class ModeSettingsControl : WpfUserControl
{
    private readonly AppSettingsService _appSettings;
    private readonly ILogger<ModeSettingsControl> _logger;
    private int _updateNestingLevel = 0;

    public ModeSettingsControl()
    {
        InitializeComponent();
    }

    public ModeSettingsControl(AppSettingsService appSettings, ILogger<ModeSettingsControl> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
        InitializeComponent();

        LoadSettings();

        _appSettings.LockDrawingModeChanged += OnLockDrawingModeChanged;
        Unloaded += (s, e) => _appSettings.LockDrawingModeChanged -= OnLockDrawingModeChanged;
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

    private void LoadSettings()
    {
        var settings = _appSettings.CurrentSettings;
        LockModeCheckBox.IsChecked = settings.LockDrawingMode;
    }

    private void LockModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_updateNestingLevel == 0 && LockModeCheckBox.IsChecked.HasValue)
        {
            _appSettings.SetLockDrawingMode(LockModeCheckBox.IsChecked.Value);
        }
    }
}
