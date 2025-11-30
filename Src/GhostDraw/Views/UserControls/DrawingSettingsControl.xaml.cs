using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using GhostDraw.Services;
using GhostDraw.Core;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.Brushes;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace GhostDraw.Views.UserControls;

public partial class DrawingSettingsControl : WpfUserControl
{
    private readonly AppSettingsService _appSettings;
    private readonly ILogger<DrawingSettingsControl> _logger;
    private int _updateNestingLevel = 0;

    public DrawingSettingsControl()
    {
        InitializeComponent();
    }

    public DrawingSettingsControl(AppSettingsService appSettings, ILogger<DrawingSettingsControl> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
        InitializeComponent();

        // Load initial settings
        LoadSettings();

        // Subscribe to settings change events
        _appSettings.BrushColorChanged += OnBrushColorChanged;
        _appSettings.BrushThicknessChanged += OnBrushThicknessChanged;
        _appSettings.BrushThicknessRangeChanged += OnBrushThicknessRangeChanged;

        // Unsubscribe when unloaded
        Unloaded += (s, e) => UnsubscribeFromEvents();
    }

    private void UnsubscribeFromEvents()
    {
        _appSettings.BrushColorChanged -= OnBrushColorChanged;
        _appSettings.BrushThicknessChanged -= OnBrushThicknessChanged;
        _appSettings.BrushThicknessRangeChanged -= OnBrushThicknessRangeChanged;
    }

    private void OnBrushColorChanged(object? sender, string colorHex)
    {
        Dispatcher.Invoke(() =>
        {
            _updateNestingLevel++;
            try
            {
                ColorPreview.Background = new SolidColorBrush(
                    (WpfColor)WpfColorConverter.ConvertFromString(colorHex));
            }
            catch
            {
                ColorPreview.Background = WpfBrush.Red;
            }
            finally
            {
                _updateNestingLevel--;
            }
        });
    }

    private void OnBrushThicknessChanged(object? sender, double thickness)
    {
        Dispatcher.Invoke(() =>
        {
            _updateNestingLevel++;
            try
            {
                if (ThicknessSlider != null && ThicknessValueText != null)
                {
                    ThicknessSlider.Value = thickness;
                    ThicknessValueText.Text = $"{thickness:F0} px";
                }
            }
            finally
            {
                _updateNestingLevel--;
            }
        });
    }

    private void OnBrushThicknessRangeChanged(object? sender, (double min, double max) range)
    {
        Dispatcher.Invoke(() =>
        {
            _updateNestingLevel++;
            try
            {
                if (MinThicknessTextBox == null || MaxThicknessTextBox == null || ThicknessSlider == null)
                    return;

                MinThicknessTextBox.Text = range.min.ToString("F0");
                MaxThicknessTextBox.Text = range.max.ToString("F0");

                var currentValue = ThicknessSlider.Value;
                ThicknessSlider.Minimum = range.min;
                ThicknessSlider.Maximum = range.max;

                if (currentValue < range.min)
                    ThicknessSlider.Value = range.min;
                else if (currentValue > range.max)
                    ThicknessSlider.Value = range.max;
                else
                    ThicknessSlider.Value = currentValue;
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
        _updateNestingLevel++;

        try
        {
            // Load brush color
            try
            {
                ColorPreview.Background = new SolidColorBrush(
                    (WpfColor)WpfColorConverter.ConvertFromString(settings.BrushColor));
            }
            catch
            {
                ColorPreview.Background = WpfBrush.Red;
            }

            // Load min/max range
            MinThicknessTextBox.Text = settings.MinBrushThickness.ToString("F0");
            MaxThicknessTextBox.Text = settings.MaxBrushThickness.ToString("F0");

            // Load brush thickness and slider
            ThicknessSlider.Minimum = settings.MinBrushThickness;
            ThicknessSlider.Maximum = settings.MaxBrushThickness;
            ThicknessSlider.Value = settings.BrushThickness;
            ThicknessValueText.Text = $"{settings.BrushThickness:F0} px";
        }
        finally
        {
            _updateNestingLevel--;
        }
    }

    private void ChooseColorButton_Click(object sender, RoutedEventArgs e)
    {
        var colorDialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.ColorTranslator.FromHtml(_appSettings.CurrentSettings.BrushColor)
        };

        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var color = colorDialog.Color;
            string hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            _appSettings.SetBrushColor(hexColor);
        }
    }

    private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ThicknessValueText != null && _updateNestingLevel == 0)
        {
            double value = e.NewValue;
            ThicknessValueText.Text = $"{value:F0} px";
            _appSettings.SetBrushThickness(value);
        }
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void MinUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(MinThicknessTextBox.Text, out double value))
        {
            var maxValue = _appSettings.CurrentSettings.MaxBrushThickness;
            if (value < maxValue - 1)
                MinThicknessTextBox.Text = (value + 1).ToString("F0");
        }
    }

    private void MinDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(MinThicknessTextBox.Text, out double value))
        {
            if (value > 1)
                MinThicknessTextBox.Text = (value - 1).ToString("F0");
        }
    }

    private void MaxUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(MaxThicknessTextBox.Text, out double value))
        {
            if (value < 100)
                MaxThicknessTextBox.Text = (value + 1).ToString("F0");
        }
    }

    private void MaxDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(MaxThicknessTextBox.Text, out double value))
        {
            var minValue = _appSettings.CurrentSettings.MinBrushThickness;
            if (value > minValue + 1)
                MaxThicknessTextBox.Text = (value - 1).ToString("F0");
        }
    }

    private void MinThicknessTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (MinThicknessTextBox == null || MaxThicknessTextBox == null || ThicknessSlider == null)
            return;

        if (_updateNestingLevel > 0)
            return;

        if (double.TryParse(MinThicknessTextBox.Text, out double minValue) && minValue > 0)
        {
            var settings = _appSettings.CurrentSettings;
            double maxValue = settings.MaxBrushThickness;

            if (double.TryParse(MaxThicknessTextBox.Text, out double parsedMax))
                maxValue = parsedMax;

            if (minValue < maxValue)
            {
                double preservedValue = ThicknessSlider.Value;
                _updateNestingLevel++;
                try
                {
                    _appSettings.SetBrushThicknessRange(minValue, maxValue);
                    ThicknessSlider.Minimum = minValue;
                    ThicknessSlider.Maximum = maxValue;

                    if (preservedValue < minValue)
                    {
                        ThicknessSlider.Value = minValue;
                        _appSettings.SetBrushThickness(minValue);
                    }
                    else if (preservedValue > maxValue)
                    {
                        ThicknessSlider.Value = maxValue;
                        _appSettings.SetBrushThickness(maxValue);
                    }
                    else
                    {
                        ThicknessSlider.Value = preservedValue;
                    }
                }
                finally
                {
                    _updateNestingLevel--;
                }
            }
        }
    }

    private void MaxThicknessTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (MinThicknessTextBox == null || MaxThicknessTextBox == null || ThicknessSlider == null)
            return;

        if (_updateNestingLevel > 0)
            return;

        if (double.TryParse(MaxThicknessTextBox.Text, out double maxValue) && maxValue > 0)
        {
            var settings = _appSettings.CurrentSettings;
            double minValue = settings.MinBrushThickness;

            if (double.TryParse(MinThicknessTextBox.Text, out double parsedMin))
                minValue = parsedMin;

            if (maxValue > minValue)
            {
                double preservedValue = ThicknessSlider.Value;
                _updateNestingLevel++;
                try
                {
                    _appSettings.SetBrushThicknessRange(minValue, maxValue);
                    ThicknessSlider.Minimum = minValue;
                    ThicknessSlider.Maximum = maxValue;

                    if (preservedValue < minValue)
                    {
                        ThicknessSlider.Value = minValue;
                        _appSettings.SetBrushThickness(minValue);
                    }
                    else if (preservedValue > maxValue)
                    {
                        ThicknessSlider.Value = maxValue;
                        _appSettings.SetBrushThickness(maxValue);
                    }
                    else
                    {
                        ThicknessSlider.Value = preservedValue;
                    }
                }
                finally
                {
                    _updateNestingLevel--;
                }
            }
        }
    }
}
