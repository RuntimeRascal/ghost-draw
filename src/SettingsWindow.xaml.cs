using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Serilog.Events;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.Brushes;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace GhostDraw
{
    public partial class SettingsWindow : Window
    {
        private readonly LoggingSettingsService _loggingSettings;
        private readonly AppSettingsService _appSettings;
        private int _updateNestingLevel = 0; // Track nesting depth of updates to prevent recursion

        public SettingsWindow(LoggingSettingsService loggingSettings, AppSettingsService appSettings)
        {
            _loggingSettings = loggingSettings;
            _appSettings = appSettings;
            InitializeComponent();

            // Load settings BEFORE subscribing to events
            LoadSettings();

            // Subscribe to settings change events for real-time UI updates AFTER loading
            _appSettings.BrushColorChanged += OnBrushColorChanged;
            _appSettings.BrushThicknessChanged += OnBrushThicknessChanged;
            _appSettings.LockDrawingModeChanged += OnLockDrawingModeChanged;
            _appSettings.BrushThicknessRangeChanged += OnBrushThicknessRangeChanged;

            // Unsubscribe when window closes
            Closed += (s, e) => UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            _appSettings.BrushColorChanged -= OnBrushColorChanged;
            _appSettings.BrushThicknessChanged -= OnBrushThicknessChanged;
            _appSettings.LockDrawingModeChanged -= OnLockDrawingModeChanged;
            _appSettings.BrushThicknessRangeChanged -= OnBrushThicknessRangeChanged;
        }

        private void OnBrushColorChanged(object? sender, string colorHex)
        {
            // Update UI on the dispatcher thread
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
            // Update UI on the dispatcher thread
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

        private void OnLockDrawingModeChanged(object? sender, bool isLocked)
        {
            // Update UI on the dispatcher thread
            Dispatcher.Invoke(() =>
            {
                _updateNestingLevel++;
                try
                {
                    if (LockModeCheckBox != null)
                    {
                        LockModeCheckBox.IsChecked = isLocked;
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
            // Update UI on the dispatcher thread
            Dispatcher.Invoke(() =>
            {
                _updateNestingLevel++;
                try
                {
                    if (MinThicknessTextBox == null || MaxThicknessTextBox == null || ThicknessSlider == null)
                        return;

                    MinThicknessTextBox.Text = range.min.ToString("F0");
                    MaxThicknessTextBox.Text = range.max.ToString("F0");

                    // Preserve current slider value
                    var currentValue = ThicknessSlider.Value;
                    ThicknessSlider.Minimum = range.min;
                    ThicknessSlider.Maximum = range.max;

                    // Only change slider value if it's out of the new range
                    if (currentValue < range.min)
                    {
                        ThicknessSlider.Value = range.min;
                    }
                    else if (currentValue > range.max)
                    {
                        ThicknessSlider.Value = range.max;
                    }
                    else
                    {
                        // Restore the value (changing min/max can cause WPF to reset it)
                        ThicknessSlider.Value = currentValue;
                    }
                }
                finally
                {
                    _updateNestingLevel--;
                }
            });
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void LoadSettings()
        {
            var settings = _appSettings.CurrentSettings;

            System.Diagnostics.Debug.WriteLine("========== LoadSettings START ==========");
            System.Diagnostics.Debug.WriteLine($"Settings from file: Min={settings.MinBrushThickness}, Max={settings.MaxBrushThickness}, Current={settings.BrushThickness}");

            // Increment nesting level to prevent event handlers from triggering logic
            _updateNestingLevel++;

            try
            {
                // Load brush color
                try
                {
                    ColorPreview.Background = new SolidColorBrush(
                        (WpfColor)WpfColorConverter.ConvertFromString(settings.BrushColor));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Color load error: {ex.Message}");
                    ColorPreview.Background = WpfBrush.Red;
                }

                // Load min/max - use F0 format to preserve precision while displaying as integers
                MinThicknessTextBox.Text = settings.MinBrushThickness.ToString("F0");
                MaxThicknessTextBox.Text = settings.MaxBrushThickness.ToString("F0");

                System.Diagnostics.Debug.WriteLine($"Set TextBoxes: MinText='{MinThicknessTextBox.Text}', MaxText='{MaxThicknessTextBox.Text}'");

                // Load brush thickness and slider
                ThicknessSlider.Minimum = settings.MinBrushThickness;
                ThicknessSlider.Maximum = settings.MaxBrushThickness;
                ThicknessSlider.Value = settings.BrushThickness;
                ThicknessValueText.Text = $"{settings.BrushThickness:F0} px";

                System.Diagnostics.Debug.WriteLine($"Slider set: Min={ThicknessSlider.Minimum}, Max={ThicknessSlider.Maximum}, Value={ThicknessSlider.Value}");

                // Load hotkey display
                CurrentHotkeyText.Text = $"{settings.HotkeyModifier1} + {settings.HotkeyModifier2} + {settings.HotkeyKey}";

                // Load lock mode
                LockModeCheckBox.IsChecked = settings.LockDrawingMode;

                // Populate log level combo box
                foreach (var level in _loggingSettings.GetAvailableLogLevels())
                {
                    LogLevelComboBox.Items.Add(new LogLevelItem
                    {
                        Level = level,
                        DisplayName = _loggingSettings.GetLogLevelDisplayName(level)
                    });
                }

                // Set current log level
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

                System.Diagnostics.Debug.WriteLine("========== LoadSettings END ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSettings ERROR: {ex}");
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
                // UI update will happen via event
            }
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Only update settings if this change was NOT triggered by programmatic update
            if (ThicknessValueText != null && _updateNestingLevel == 0)
            {
                double value = e.NewValue;
                ThicknessValueText.Text = $"{value:F0} px";
                _appSettings.SetBrushThickness(value);
                // UI update will happen via event, but we already updated the text
            }
        }

        private void LockModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Only update settings if this change was NOT triggered by programmatic update
            if (_updateNestingLevel == 0 && LockModeCheckBox.IsChecked.HasValue)
            {
                _appSettings.SetLockDrawingMode(LockModeCheckBox.IsChecked.Value);
                // UI update will happen via event
            }
        }

        private void LogLevelComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
                LoadSettings();
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

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow numeric input
            e.Handled = !IsTextNumeric(e.Text);
        }

        private bool IsTextNumeric(string text)
        {
            return int.TryParse(text, out _);
        }

        private void MinUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(MinThicknessTextBox.Text, out double value))
            {
                var maxValue = _appSettings.CurrentSettings.MaxBrushThickness;
                if (value < maxValue - 1) // Ensure there's room to increment
                {
                    MinThicknessTextBox.Text = (value + 1).ToString("F0");
                }
            }
        }

        private void MinDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(MinThicknessTextBox.Text, out double value))
            {
                if (value > 1) // Don't go below 1
                {
                    MinThicknessTextBox.Text = (value - 1).ToString("F0");
                }
            }
        }

        private void MaxUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(MaxThicknessTextBox.Text, out double value))
            {
                if (value < 100) // Reasonable upper limit
                {
                    MaxThicknessTextBox.Text = (value + 1).ToString("F0");
                }
            }
        }

        private void MaxDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(MaxThicknessTextBox.Text, out double value))
            {
                var minValue = _appSettings.CurrentSettings.MinBrushThickness;
                if (value > minValue + 1) // Ensure there's room to decrement
                {
                    MaxThicknessTextBox.Text = (value - 1).ToString("F0");
                }
            }
        }

        private void MinThicknessTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (MinThicknessTextBox == null || MaxThicknessTextBox == null || ThicknessSlider == null)
                return;

            // Skip if we're in a programmatic update
            if (_updateNestingLevel > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[MinTextChanged] Skipping (nesting level={_updateNestingLevel})");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[MinTextChanged] Processing: Text={MinThicknessTextBox.Text}");

            if (double.TryParse(MinThicknessTextBox.Text, out double minValue) && minValue > 0)
            {
                var settings = _appSettings.CurrentSettings;
                double maxValue = settings.MaxBrushThickness;

                if (double.TryParse(MaxThicknessTextBox.Text, out double parsedMax))
                {
                    maxValue = parsedMax;
                }

                // Ensure min is less than max
                if (minValue < maxValue)
                {
                    // Save current slider value BEFORE changing anything
                    double preservedValue = ThicknessSlider.Value;
                    System.Diagnostics.Debug.WriteLine($"[MinTextChanged] Preserved slider value: {preservedValue}");

                    // Increment nesting level to protect all operations
                    _updateNestingLevel++;
                    try
                    {
                        // Save the range (this will trigger event, but it will respect our nesting level)
                        _appSettings.SetBrushThicknessRange(minValue, maxValue);

                        // Update slider range
                        ThicknessSlider.Minimum = minValue;
                        ThicknessSlider.Maximum = maxValue;

                        // Only change slider value if it's out of the new range
                        if (preservedValue < minValue)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MinTextChanged] Adjusting slider: {preservedValue} < {minValue}, setting to {minValue}");
                            ThicknessSlider.Value = minValue;
                            _appSettings.SetBrushThickness(minValue);
                        }
                        else if (preservedValue > maxValue)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MinTextChanged] Adjusting slider: {preservedValue} > {maxValue}, setting to {maxValue}");
                            ThicknessSlider.Value = maxValue;
                            _appSettings.SetBrushThickness(maxValue);
                        }
                        else
                        {
                            // Value is still valid - keep it exactly where it was
                            System.Diagnostics.Debug.WriteLine($"[MinTextChanged] Keeping slider at: {preservedValue}");
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

        private void MaxThicknessTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (MinThicknessTextBox == null || MaxThicknessTextBox == null || ThicknessSlider == null)
                return;

            // Skip if we're in a programmatic update
            if (_updateNestingLevel > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[MaxTextChanged] Skipping (nesting level={_updateNestingLevel})");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[MaxTextChanged] Processing: Text={MaxThicknessTextBox.Text}");

            if (double.TryParse(MaxThicknessTextBox.Text, out double maxValue) && maxValue > 0)
            {
                var settings = _appSettings.CurrentSettings;
                double minValue = settings.MinBrushThickness;

                if (double.TryParse(MinThicknessTextBox.Text, out double parsedMin))
                {
                    minValue = parsedMin;
                }

                // Ensure max is greater than min
                if (maxValue > minValue)
                {
                    // Save current slider value BEFORE changing anything
                    double preservedValue = ThicknessSlider.Value;
                    System.Diagnostics.Debug.WriteLine($"[MaxTextChanged] Preserved slider value: {preservedValue}");

                    // Increment nesting level to protect all operations
                    _updateNestingLevel++;
                    try
                    {
                        // Save the range (this will trigger event, but it will respect our nesting level)
                        _appSettings.SetBrushThicknessRange(minValue, maxValue);

                        // Update slider range
                        ThicknessSlider.Minimum = minValue;
                        ThicknessSlider.Maximum = maxValue;

                        // Only change slider value if it's out of the new range
                        if (preservedValue < minValue)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MaxTextChanged] Adjusting slider: {preservedValue} < {minValue}, setting to {minValue}");
                            ThicknessSlider.Value = minValue;
                            _appSettings.SetBrushThickness(minValue);
                        }
                        else if (preservedValue > maxValue)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MaxTextChanged] Adjusting slider: {preservedValue} > {maxValue}, setting to {maxValue}");
                            ThicknessSlider.Value = maxValue;
                            _appSettings.SetBrushThickness(maxValue);
                        }
                        else
                        {
                            // Value is still valid - keep it exactly where it was
                            System.Diagnostics.Debug.WriteLine($"[MaxTextChanged] Keeping slider at: {preservedValue}");
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

        private class LogLevelItem
        {
            public LogEventLevel Level { get; set; }
            public string DisplayName { get; set; } = string.Empty;

            public override string ToString() => DisplayName;
        }
    }
}
