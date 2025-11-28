using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.Brushes;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfMessageBox = System.Windows.MessageBox;

namespace GhostDraw
{
    public partial class SettingsWindow : Window
    {
        private readonly LoggingSettingsService _loggingSettings;
        private readonly AppSettingsService _appSettings;
        private readonly ILogger<SettingsWindow> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private bool _isUpdatingFromEvent = false; // Prevent recursive updates
        
        // Hotkey recording state
        private bool _isRecording = false;
        private HashSet<int> _recordedKeys = new();
        private GlobalKeyboardHook? _recorderHook;

        private int _updateNestingLevel = 0; // Track nesting depth of updates to prevent recursion

        public SettingsWindow(LoggingSettingsService loggingSettings, AppSettingsService appSettings, ILogger<SettingsWindow> logger, ILoggerFactory loggerFactory)
        {
            _loggingSettings = loggingSettings;
            _appSettings = appSettings;
            _logger = logger;
            _loggerFactory = loggerFactory;
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
                CurrentHotkeyText.Text = settings.HotkeyDisplayName;

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

        // ====================
        // Hotkey Recording
        // ====================

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            StartRecording();
        }

        private void StartRecording()
        {
            _isRecording = true;
            _recordedKeys.Clear();
            
            // Show recorder UI
            RecorderBox.Visibility = Visibility.Visible;
            RecordButton.Visibility = Visibility.Collapsed;
            CancelRecordButton.Visibility = Visibility.Visible;
            RecorderStatusText.Foreground = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#FF0080"));
            RecorderStatusText.Text = "?? RECORDING... Press your hotkey combination";
            RecorderPreviewText.Text = "Waiting for keys...";
            
            // Create temporary hook for recording
            var hookLogger = _loggerFactory.CreateLogger<GlobalKeyboardHook>();
            _recorderHook = new GlobalKeyboardHook(hookLogger);
            _recorderHook.KeyPressed += OnRecorderKeyPressed;
            _recorderHook.KeyReleased += OnRecorderKeyReleased;
            _recorderHook.EscapePressed += OnRecorderEscape;
            _recorderHook.Start();
            
            _logger.LogInformation("Started hotkey recording");
        }

        private void OnRecorderKeyPressed(object? sender, GlobalKeyboardHook.KeyEventArgs e)
        {
            _recordedKeys.Add(e.VirtualKeyCode);
            RecorderPreviewText.Text = Helpers.VirtualKeyHelper.GetCombinationDisplayName(_recordedKeys.ToList());
            _logger.LogDebug("Recorded key: VK {VK} ({Name})", e.VirtualKeyCode, Helpers.VirtualKeyHelper.GetFriendlyName(e.VirtualKeyCode));
        }

        private async void OnRecorderKeyReleased(object? sender, GlobalKeyboardHook.KeyEventArgs e)
        {
            // When user releases ALL keys, we have the complete combination
            _logger.LogDebug("Key released: VK {VK}, Recorded keys count: {Count}", e.VirtualKeyCode, _recordedKeys.Count);
            
            if (_recordedKeys.Count > 0)
            {
                // Small delay to allow OS to update key states
                await Task.Delay(50);
                
                if (!IsAnyKeyPressed())
                {
                    _logger.LogInformation("All keys released, stopping recording with {Count} keys", _recordedKeys.Count);
                    StopRecording(accepted: true);
                }
            }
        }

        private void OnRecorderEscape(object? sender, EventArgs e)
        {
            StopRecording(accepted: false);
        }

        private void StopRecording(bool accepted)
        {
            _isRecording = false;
            
            // Cleanup recorder hook
            _recorderHook?.Stop();
            _recorderHook?.Dispose();
            _recorderHook = null;
            
            if (accepted && ValidateRecordedKeys())
            {
                // Show success state - keep recorder visible with success message
                RecorderStatusText.Text = "? Hotkey captured! Click APPLY to save";
                RecorderStatusText.Foreground = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#00FF00"));
                
                // Update buttons - hide cancel, show record again and apply
                CancelRecordButton.Visibility = Visibility.Collapsed;
                RecordButton.Visibility = Visibility.Visible;
                RecordButton.Content = "?? RECORD AGAIN";
                ApplyHotkeyButton.Visibility = Visibility.Visible;
                
                _logger.LogInformation("Hotkey recorded successfully: {Hotkey}", Helpers.VirtualKeyHelper.GetCombinationDisplayName(_recordedKeys.ToList()));
            }
            else
            {
                // Cancel - hide recorder completely
                RecorderBox.Visibility = Visibility.Collapsed;
                RecordButton.Visibility = Visibility.Visible;
                RecordButton.Content = "?? RECORD NEW HOTKEY";
                CancelRecordButton.Visibility = Visibility.Collapsed;
                ApplyHotkeyButton.Visibility = Visibility.Collapsed;
                
                _logger.LogInformation("Hotkey recording cancelled");
            }
        }

        private bool ValidateRecordedKeys()
        {
            // Must have at least 2 keys
            if (_recordedKeys.Count < 2)
            {
                WpfMessageBox.Show(
                    "Hotkey must include at least one modifier (Ctrl, Alt, Shift, Win) and one regular key.",
                    "Invalid Hotkey", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return false;
            }
            
            // Must have at least one modifier
            if (!_recordedKeys.Any(vk => Helpers.VirtualKeyHelper.IsModifierKey(vk)))
            {
                WpfMessageBox.Show(
                    "Hotkey must include at least one modifier key (Ctrl, Alt, Shift, Win).",
                    "Invalid Hotkey", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return false;
            }
            
            // Check for reserved combinations
            if (IsReservedCombo(_recordedKeys.ToList()))
            {
                var result = WpfMessageBox.Show(
                    $"?? {Helpers.VirtualKeyHelper.GetCombinationDisplayName(_recordedKeys.ToList())} is a system hotkey that may not work reliably.\n\nDo you want to use it anyway?",
                    "System Hotkey Warning", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                
                return result == MessageBoxResult.Yes;
            }
            
            return true;
        }

        private bool IsReservedCombo(List<int> vks)
        {
            // Check common reserved combinations
            var hasCtrl = vks.Any(vk => vk is 0xA2 or 0xA3);
            var hasAlt = vks.Any(vk => vk is 0xA4 or 0xA5);
            var hasWin = vks.Any(vk => vk is 0x5B or 0x5C);
            
            // Ctrl+Alt+Delete
            if (hasCtrl && hasAlt && vks.Contains(0x2E))
                return true;
            
            // Win+L
            if (hasWin && vks.Contains(0x4C))
                return true;
            
            // Alt+F4
            if (hasAlt && vks.Contains(0x73))
                return true;
            
            // Alt+Tab
            if (hasAlt && vks.Contains(0x09))
                return true;
            
            return false;
        }

        private bool IsAnyKeyPressed()
        {
            foreach (var vk in _recordedKeys)
            {
                short keyState = GetAsyncKeyState(vk);
                bool isPressed = (keyState & 0x8000) != 0;
                _logger.LogTrace("VK {VK} state: {State} (raw: {Raw})", vk, isPressed ? "PRESSED" : "released", keyState);
                
                if (isPressed)
                {
                    _logger.LogDebug("Key VK {VK} is still pressed", vk);
                    return true;
                }
            }
            _logger.LogDebug("All keys released");
            return false;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void ApplyHotkey_Click(object sender, RoutedEventArgs e)
        {
            // Save to settings
            _appSettings.SetHotkey(_recordedKeys.ToList());
            
            // Update display
            CurrentHotkeyText.Text = RecorderPreviewText.Text;
            
            // Reset UI
            RecorderBox.Visibility = Visibility.Collapsed;
            RecordButton.Visibility = Visibility.Visible;
            RecordButton.Content = "?? RECORD NEW HOTKEY";
            ApplyHotkeyButton.Visibility = Visibility.Collapsed;
            
            WpfMessageBox.Show(
                "? Hotkey updated successfully!\n\nChanges take effect immediately.",
                "Success", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            
            _logger.LogInformation("Hotkey updated to: {Hotkey}", _appSettings.CurrentSettings.HotkeyDisplayName);
        }

        private void CancelRecord_Click(object sender, RoutedEventArgs e)
        {
            StopRecording(accepted: false);
        }
    }
}
