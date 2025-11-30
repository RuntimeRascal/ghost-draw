using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using GhostDraw.Core;
using GhostDraw.Services;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfMessageBox = System.Windows.MessageBox;

namespace GhostDraw.Views.UserControls;

public partial class HotkeySettingsControl : WpfUserControl
{
    private readonly AppSettingsService _appSettings = null!;
    private readonly ILogger<HotkeySettingsControl> _logger = null!;
    private readonly ILoggerFactory _loggerFactory = null!;
    
    private readonly HashSet<int> _recordedKeys = new();
    private GlobalKeyboardHook? _recorderHook;

    public HotkeySettingsControl()
    {
        InitializeComponent();
    }

    public HotkeySettingsControl(AppSettingsService appSettings, ILogger<HotkeySettingsControl> logger, ILoggerFactory loggerFactory)
    {
        _appSettings = appSettings;
        _logger = logger;
        _loggerFactory = loggerFactory;
        InitializeComponent();

        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _appSettings.CurrentSettings;
        CurrentHotkeyText.Text = settings.HotkeyDisplayName;
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        StartRecording();
    }

    private void StartRecording()
    {
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
        _logger.LogDebug("Key released: VK {VK}, Recorded keys count: {Count}", e.VirtualKeyCode, _recordedKeys.Count);

        if (_recordedKeys.Count > 0)
        {
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
        _recorderHook?.Stop();
        _recorderHook?.Dispose();
        _recorderHook = null;

        if (accepted && ValidateRecordedKeys())
        {
            RecorderStatusText.Text = "? Hotkey captured! Click APPLY to save";
            RecorderStatusText.Foreground = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#00FF00"));

            CancelRecordButton.Visibility = Visibility.Collapsed;
            RecordButton.Visibility = Visibility.Visible;
            RecordButton.Content = "?? RECORD AGAIN";
            ApplyHotkeyButton.Visibility = Visibility.Visible;

            _logger.LogInformation("Hotkey recorded successfully: {Hotkey}", Helpers.VirtualKeyHelper.GetCombinationDisplayName(_recordedKeys.ToList()));
        }
        else
        {
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
        if (_recordedKeys.Count < 2)
        {
            WpfMessageBox.Show(
                "Hotkey must include at least one modifier (Ctrl, Alt, Shift, Win) and one regular key.",
                "Invalid Hotkey",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        if (!_recordedKeys.Any(vk => Helpers.VirtualKeyHelper.IsModifierKey(vk)))
        {
            WpfMessageBox.Show(
                "Hotkey must include at least one modifier key (Ctrl, Alt, Shift, Win).",
                "Invalid Hotkey",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

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
        var hasCtrl = vks.Any(vk => vk is 0xA2 or 0xA3);
        var hasAlt = vks.Any(vk => vk is 0xA4 or 0xA5);
        var hasWin = vks.Any(vk => vk is 0x5B or 0x5C);

        if (hasCtrl && hasAlt && vks.Contains(0x2E)) return true;
        if (hasWin && vks.Contains(0x4C)) return true;
        if (hasAlt && vks.Contains(0x73)) return true;
        if (hasAlt && vks.Contains(0x09)) return true;

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
        _appSettings.SetHotkey(_recordedKeys.ToList());
        CurrentHotkeyText.Text = RecorderPreviewText.Text;

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
