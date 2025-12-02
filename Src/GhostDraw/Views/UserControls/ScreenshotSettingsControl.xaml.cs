using System.Windows;
using GhostDraw.Services;
using Microsoft.Win32;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GhostDraw.Views.UserControls;

public partial class ScreenshotSettingsControl : WpfUserControl
{
    private int _updateNestingLevel = 0;

    // DependencyProperty for AppSettings
    public static readonly DependencyProperty AppSettingsProperty =
        DependencyProperty.Register(
            nameof(AppSettings),
            typeof(AppSettingsService),
            typeof(ScreenshotSettingsControl),
            new PropertyMetadata(null, OnAppSettingsChanged));

    public AppSettingsService? AppSettings
    {
        get => (AppSettingsService?)GetValue(AppSettingsProperty);
        set => SetValue(AppSettingsProperty, value);
    }

    private static void OnAppSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScreenshotSettingsControl control && e.NewValue is AppSettingsService appSettings)
        {
            control.Initialize(appSettings);
        }
    }

    public ScreenshotSettingsControl()
    {
        InitializeComponent();
    }

    private void Initialize(AppSettingsService appSettings)
    {
        LoadSettings(appSettings);
    }

    private void LoadSettings(AppSettingsService appSettings)
    {
        var settings = appSettings.CurrentSettings;
        SavePathTextBox.Text = settings.ScreenshotSavePath;
        CopyToClipboardCheckBox.IsChecked = settings.CopyScreenshotToClipboard;
        OpenFolderCheckBox.IsChecked = settings.OpenFolderAfterScreenshot;
        PlaySoundCheckBox.IsChecked = settings.PlayShutterSound;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppSettings == null) return;

        var dialog = new OpenFolderDialog
        {
            Title = "Select Screenshot Save Location",
            InitialDirectory = AppSettings.CurrentSettings.ScreenshotSavePath
        };

        if (dialog.ShowDialog() == true)
        {
            var selectedPath = dialog.FolderName;
            SavePathTextBox.Text = selectedPath;
            AppSettings.SetScreenshotSavePath(selectedPath);
        }
    }

    private void CopyToClipboardCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_updateNestingLevel == 0 && CopyToClipboardCheckBox.IsChecked.HasValue && AppSettings != null)
        {
            AppSettings.SetCopyScreenshotToClipboard(CopyToClipboardCheckBox.IsChecked.Value);
        }
    }

    private void OpenFolderCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_updateNestingLevel == 0 && OpenFolderCheckBox.IsChecked.HasValue && AppSettings != null)
        {
            AppSettings.SetOpenFolderAfterScreenshot(OpenFolderCheckBox.IsChecked.Value);
        }
    }

    private void PlaySoundCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_updateNestingLevel == 0 && PlaySoundCheckBox.IsChecked.HasValue && AppSettings != null)
        {
            AppSettings.SetPlayShutterSound(PlaySoundCheckBox.IsChecked.Value);
        }
    }
}
