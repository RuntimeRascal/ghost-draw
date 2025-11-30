using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using GhostDraw.Services;
using GhostDraw.Views.UserControls;

namespace GhostDraw.Views;

public partial class SettingsWindow : Window
{
    private readonly LoggingSettingsService _loggingSettings;
    private readonly AppSettingsService _appSettings;
    private readonly ILogger<SettingsWindow> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public SettingsWindow(LoggingSettingsService loggingSettings, AppSettingsService appSettings, ILogger<SettingsWindow> logger, ILoggerFactory loggerFactory)
    {
        _loggingSettings = loggingSettings;
        _appSettings = appSettings;
        _logger = logger;
        _loggerFactory = loggerFactory;
        
        InitializeComponent();
        
        // Initialize UserControls with dependencies after InitializeComponent
        LoadUserControls();
    }

    private void LoadUserControls()
    {
        // Find the SettingsStackPanel by name
        var stackPanel = (StackPanel)this.FindName("SettingsStackPanel");
        if (stackPanel == null)
        {
            _logger.LogError("Could not find SettingsStackPanel");
            return;
        }

        stackPanel.Children.Clear();

        // Create and add Drawing Settings
        var drawingControl = new DrawingSettingsControl(_appSettings);
        stackPanel.Children.Add(drawingControl);

        // Create and add Hotkey Settings
        var hotkeyLogger = _loggerFactory.CreateLogger<HotkeySettingsControl>();
        var hotkeyControl = new HotkeySettingsControl(_appSettings, hotkeyLogger, _loggerFactory);
        stackPanel.Children.Add(hotkeyControl);

        // Create and add Mode Settings
        var modeControl = new ModeSettingsControl(_appSettings);
        stackPanel.Children.Add(modeControl);

        // Create and add Logging Settings
        var loggingControl = new LoggingSettingsControl(_loggingSettings);
        stackPanel.Children.Add(loggingControl);

        // Create and add Action Buttons Grid
        var actionGrid = CreateActionButtonsGrid();
        stackPanel.Children.Add(actionGrid);
    }

    private Grid CreateActionButtonsGrid()
    {
        var grid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var resetButton = new System.Windows.Controls.Button
        {
            Content = "RESET",
            Margin = new Thickness(0, 0, 12, 0),
            Padding = new Thickness(20, 12, 20, 12),
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#808080")),
            BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#404040")),
            BorderThickness = new Thickness(1),
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        resetButton.Click += ResetButton_Click;
        Grid.SetColumn(resetButton, 1);
        grid.Children.Add(resetButton);

        var saveButton = new System.Windows.Controls.Button
        {
            Content = "SAVE & CLOSE",
            Cursor = System.Windows.Input.Cursors.Hand
        };
        saveButton.SetResourceReference(System.Windows.Controls.Button.StyleProperty, "ModernButton");
        saveButton.Click += CloseButton_Click;
        Grid.SetColumn(saveButton, 2);
        grid.Children.Add(saveButton);

        return grid;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            this.DragMove();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to reset all settings to defaults?",
            "Reset Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _appSettings.ResetToDefaults();
            
            // Reload UserControls
            LoadUserControls();
            
            System.Windows.MessageBox.Show(
                "Settings have been reset to defaults.",
                "Reset Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
