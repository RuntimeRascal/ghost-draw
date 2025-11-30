using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using GhostDraw.Services;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.Brushes;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace GhostDraw.Views.UserControls;

public partial class DrawingSettingsControl : WpfUserControl
{
    private int _updateNestingLevel = 0;

    // DependencyProperty for AppSettings to enable XAML binding
    public static readonly DependencyProperty AppSettingsProperty =
        DependencyProperty.Register(
            nameof(AppSettings),
            typeof(AppSettingsService),
            typeof(DrawingSettingsControl),
            new PropertyMetadata(null, OnAppSettingsChanged));

    public AppSettingsService? AppSettings
    {
        get => (AppSettingsService?)GetValue(AppSettingsProperty);
        set => SetValue(AppSettingsProperty, value);
    }

    private static void OnAppSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DrawingSettingsControl control && e.NewValue is AppSettingsService appSettings)
        {
            control.Initialize(appSettings);
        }
    }

    public DrawingSettingsControl()
    {
        InitializeComponent();
    }

    private void Initialize(AppSettingsService appSettings)
    {
        // Load initial settings
        LoadSettings(appSettings);

        // Subscribe to settings change events
        appSettings.BrushColorChanged += OnBrushColorChanged;
        appSettings.BrushThicknessChanged += OnBrushThicknessChanged;
        appSettings.BrushThicknessRangeChanged += OnBrushThicknessRangeChanged;
        appSettings.ColorPaletteChanged += OnColorPaletteChanged;

        // Unsubscribe when unloaded
        Unloaded += (s, e) => UnsubscribeFromEvents(appSettings);
    }

    private void UnsubscribeFromEvents(AppSettingsService appSettings)
    {
        appSettings.BrushColorChanged -= OnBrushColorChanged;
        appSettings.BrushThicknessChanged -= OnBrushThicknessChanged;
        appSettings.BrushThicknessRangeChanged -= OnBrushThicknessRangeChanged;
        appSettings.ColorPaletteChanged -= OnColorPaletteChanged;
    }

    private void OnBrushColorChanged(object? sender, string colorHex)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateActiveColorIndicators();
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

    private void OnColorPaletteChanged(object? sender, List<string> palette)
    {
        Dispatcher.Invoke(() =>
        {
            LoadColorPalette(palette);
        });
    }

    private void LoadColorPalette(List<string> palette)
    {
        PaletteColorsItemsControl.ItemsSource = new List<string>(palette);
    }

    private void PaletteColorsItemsControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Update indicators after ItemsControl is fully loaded
        // Use multiple priority levels to ensure it happens
        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateActiveColorIndicators();
        }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        
        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateActiveColorIndicators();
        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void ColorSquare_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (AppSettings == null) return;
        
        if (sender is Border border && border.Tag is string colorHex)
        {
            AppSettings.SetActiveBrush(colorHex);
            
            // Update immediately since visual tree is already loaded
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateActiveColorIndicators();
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }
    }

    private void UpdateActiveColorIndicators()
    {
        if (AppSettings == null) return;
        
        var activeColor = AppSettings.CurrentSettings.ActiveBrush;
        
        // Try StackPanel first (default for ItemsControl without ItemsPanel specified)
        var itemsPanel = FindVisualChild<StackPanel>(PaletteColorsItemsControl);
        if (itemsPanel == null)
        {
            // Try again after a short delay if visual tree isn't ready
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateActiveColorIndicatorsInternal(activeColor);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            return;
        }

        UpdateActiveColorIndicatorsInternal(activeColor);
    }

    private void UpdateActiveColorIndicatorsInternal(string activeColor)
    {
        // Try to find StackPanel (default ItemsControl panel)
        var itemsPanel = FindVisualChild<StackPanel>(PaletteColorsItemsControl);
        if (itemsPanel == null)
            return;

        int position = 1;
        foreach (var child in itemsPanel.Children)
        {
            // WPF wraps DataTemplate items in ContentPresenter, so we need to look inside
            Grid? grid = null;
            
            if (child is ContentPresenter contentPresenter)
            {
                // Find the Grid inside the ContentPresenter
                grid = FindVisualChild<Grid>(contentPresenter);
            }
            else if (child is Grid directGrid)
            {
                grid = directGrid;
            }
            
            if (grid != null && grid.Children.Count > 0 && grid.Children[0] is Border colorBorder)
            {
                var colorHex = colorBorder.Tag as string;
                var isActive = colorHex == activeColor;

                // Update border style for active color
                colorBorder.BorderBrush = isActive 
                    ? new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#FF0080"))
                    : new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#00FFFF"));
                colorBorder.BorderThickness = isActive ? new Thickness(3) : new Thickness(2);

                // Update position number and checkmark inside the Border's Grid
                if (VisualTreeHelper.GetChildrenCount(colorBorder) > 0)
                {
                    var innerGrid = VisualTreeHelper.GetChild(colorBorder, 0);
                    if (innerGrid is Grid contentGrid)
                    {
                        // Find position number badge (first child - Border containing TextBlock)
                        if (contentGrid.Children.Count > 0 && contentGrid.Children[0] is Border badge)
                        {
                            var positionText = FindVisualChild<TextBlock>(badge);
                            if (positionText != null)
                            {
                                positionText.Text = position.ToString();
                            }
                        }
                        
                        // Find checkmark (second child - Viewbox containing TextBlock)
                        if (contentGrid.Children.Count > 1 && contentGrid.Children[1] is Viewbox vb && vb.Child is TextBlock checkmark)
                        {
                            checkmark.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }

                // Replace the drop shadow effect with a new one (can't modify frozen effects)
                var shadowColor = isActive 
                    ? (WpfColor)WpfColorConverter.ConvertFromString("#FF0080")
                    : (WpfColor)WpfColorConverter.ConvertFromString("#00FFFF");
                var shadowOpacity = isActive ? 0.8 : 0.4;
                
                colorBorder.Effect = new DropShadowEffect
                {
                    Color = shadowColor,
                    Opacity = shadowOpacity,
                    BlurRadius = 8,
                    ShadowDepth = 0
                };
                
                position++;
            }
        }
    }

    // Helper method to find visual child
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;
            
            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private void AddColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppSettings == null) return;

        var colorDialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.Color.White
        };

        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var color = colorDialog.Color;
            string hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            AppSettings.AddColorToPalette(hexColor);
        }
    }

    private void RemoveColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppSettings == null) return;
        
        if (sender is System.Windows.Controls.Button button && button.Tag is string colorHex)
        {
            // Show confirmation dialog
            var result = System.Windows.MessageBox.Show(
                $"Remove color {colorHex} from palette?",
                "Remove Color",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AppSettings.RemoveColorFromPalette(colorHex);
            }
        }
    }

    private void MoveColorUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppSettings == null) return;
        
        if (sender is System.Windows.Controls.Button button && button.Tag is string colorHex)
        {
            var palette = new List<string>(AppSettings.CurrentSettings.ColorPalette);
            var currentIndex = palette.IndexOf(colorHex);
            
            if (currentIndex < 0)
                return;
            
            // Remove from current position
            palette.RemoveAt(currentIndex);
            
            // If at the top, wrap to bottom
            if (currentIndex == 0)
            {
                palette.Add(colorHex);
            }
            else
            {
                // Move up one position
                palette.Insert(currentIndex - 1, colorHex);
            }
            
            AppSettings.SetColorPalette(palette);
        }
    }

    private void MoveColorDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppSettings == null) return;
        
        if (sender is System.Windows.Controls.Button button && button.Tag is string colorHex)
        {
            var palette = new List<string>(AppSettings.CurrentSettings.ColorPalette);
            var currentIndex = palette.IndexOf(colorHex);
            
            if (currentIndex < 0)
                return;
            
            // Remove from current position
            palette.RemoveAt(currentIndex);
            
            // If at the bottom, wrap to top
            if (currentIndex >= palette.Count)
            {
                palette.Insert(0, colorHex);
            }
            else
            {
                // Move down one position
                palette.Insert(currentIndex + 1, colorHex);
            }
            
            AppSettings.SetColorPalette(palette);
        }
    }

    private void LoadSettings(AppSettingsService appSettings)
    {
        var settings = appSettings.CurrentSettings;
        _updateNestingLevel++;

        try
        {
            // Load color palette
            LoadColorPalette(settings.ColorPalette);

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

    private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ThicknessValueText != null && _updateNestingLevel == 0 && AppSettings != null)
        {
            double value = e.NewValue;
            ThicknessValueText.Text = $"{value:F0} px";
            AppSettings.SetBrushThickness(value);
        }
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void MinUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppSettings == null) return;
        
        if (double.TryParse(MinThicknessTextBox.Text, out double value))
        {
            var maxValue = AppSettings.CurrentSettings.MaxBrushThickness;
            if (value < maxValue - 1)
                MinThicknessTextBox.Text = (value + 1).ToString("F0");
        }
    }

    private void MinDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppSettings == null) return;
        
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
        if (AppSettings == null) return;
        
        if (double.TryParse(MaxThicknessTextBox.Text, out double value))
        {
            var minValue = AppSettings.CurrentSettings.MinBrushThickness;
            if (value > minValue + 1)
                MaxThicknessTextBox.Text = (value - 1).ToString("F0");
        }
    }

    private void MinThicknessTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (MinThicknessTextBox == null || MaxThicknessTextBox == null || ThicknessSlider == null || AppSettings == null)
            return;

        if (_updateNestingLevel > 0)
            return;

        if (double.TryParse(MinThicknessTextBox.Text, out double minValue) && minValue > 0)
        {
            var settings = AppSettings.CurrentSettings;
            double maxValue = settings.MaxBrushThickness;

            if (double.TryParse(MaxThicknessTextBox.Text, out double parsedMax))
                maxValue = parsedMax;

            if (minValue < maxValue)
            {
                double preservedValue = ThicknessSlider.Value;
                _updateNestingLevel++;
                try
                {
                    AppSettings.SetBrushThicknessRange(minValue, maxValue);
                    ThicknessSlider.Minimum = minValue;
                    ThicknessSlider.Maximum = maxValue;

                    if (preservedValue < minValue)
                    {
                        ThicknessSlider.Value = minValue;
                        AppSettings.SetBrushThickness(minValue);
                    }
                    else if (preservedValue > maxValue)
                    {
                        ThicknessSlider.Value = maxValue;
                        AppSettings.SetBrushThickness(maxValue);
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
        if (MinThicknessTextBox == null || MaxThicknessTextBox == null || ThicknessSlider == null || AppSettings == null)
            return;

        if (_updateNestingLevel > 0)
            return;

        if (double.TryParse(MaxThicknessTextBox.Text, out double maxValue) && maxValue > 0)
        {
            var settings = AppSettings.CurrentSettings;
            double minValue = settings.MinBrushThickness;

            if (double.TryParse(MinThicknessTextBox.Text, out double parsedMin))
                minValue = parsedMin;

            if (maxValue > minValue)
            {
                double preservedValue = ThicknessSlider.Value;
                _updateNestingLevel++;
                try
                {
                    AppSettings.SetBrushThicknessRange(minValue, maxValue);
                    ThicknessSlider.Minimum = minValue;
                    ThicknessSlider.Maximum = maxValue;

                    if (preservedValue < minValue)
                    {
                        ThicknessSlider.Value = minValue;
                        AppSettings.SetBrushThickness(minValue);
                    }
                    else if (preservedValue > maxValue)
                    {
                        ThicknessSlider.Value = maxValue;
                        AppSettings.SetBrushThickness(maxValue);
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
