using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using GhostDraw.Services;
using Application = System.Windows.Application;
using GhostDraw.Managers;
using GhostDraw.Core;
using GhostDraw.Views;

namespace GhostDraw;

public partial class App : Application
{
    private NotifyIcon? _notifyIcon;
    private GlobalKeyboardHook? _keyboardHook;
    private DrawingManager? _drawingManager;
    private ServiceProvider? _serviceProvider;
    private ILogger<App>? _logger;
    private LoggingSettingsService? _loggingSettings;
    private AppSettingsService? _appSettings;
    private GlobalExceptionHandler? _exceptionHandler;
    private static Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Check for single instance
        const string mutexName = "GhostDraw_SingleInstance_Mutex_3F4A5B6C";
        _singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            System.Windows.MessageBox.Show(
                "GhostDraw is already running.\n\nCheck the system tray for the GhostDraw icon.",
                "GhostDraw",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        try
        {
            // Initialize DI container and logging
            _serviceProvider = ServiceConfiguration.ConfigureServices();
            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            _loggingSettings = _serviceProvider.GetRequiredService<LoggingSettingsService>();
            _appSettings = _serviceProvider.GetRequiredService<AppSettingsService>();

            // CRITICAL: Register global exception handler FIRST
            _exceptionHandler = _serviceProvider.GetRequiredService<GlobalExceptionHandler>();
            _exceptionHandler.RegisterHandlers();
            _logger.LogInformation("Global exception handler registered - system safety ensured");

            _logger.LogInformation("===== App.OnStartup() called =====");

            // Log loaded settings
            var settings = _appSettings.CurrentSettings;
            _logger.LogInformation("Loaded settings - Color: {Color}, Thickness: {Thickness}, Hotkey: {Hotkey}, LockMode: {LockMode}",
                settings.ActiveBrush, settings.BrushThickness,
                settings.HotkeyDisplayName,
                settings.LockDrawingMode);

            // Get services from DI container
            _drawingManager = _serviceProvider.GetRequiredService<DrawingManager>();
            _keyboardHook = _serviceProvider.GetRequiredService<GlobalKeyboardHook>();

            // Setup keyboard hook with exception handling
            _logger.LogDebug("Setting up keyboard hook events");
            _keyboardHook.HotkeyPressed += OnHotkeyPressed;
            _keyboardHook.HotkeyReleased += OnHotkeyReleased;
            _keyboardHook.EscapePressed += OnEscapePressed;
            _keyboardHook.ClearCanvasPressed += OnClearCanvasPressed;
            _keyboardHook.PenToolPressed += OnPenToolPressed;
            _keyboardHook.LineToolPressed += OnLineToolPressed;
            _keyboardHook.EraserToolPressed += OnEraserToolPressed;
            _keyboardHook.RectangleToolPressed += OnRectangleToolPressed;
            _keyboardHook.CircleToolPressed += OnCircleToolPressed;
            _keyboardHook.HelpPressed += OnHelpPressed;
            _keyboardHook.ScreenshotFullPressed += OnScreenshotFullPressed;
            _keyboardHook.Start();

            // Setup system tray icon
            _logger.LogDebug("Creating system tray icon");

            // Load icon from embedded resource
            var iconUri = new Uri("pack://application:,,,/Assets/favicon.ico");
            var streamResourceInfo = System.Windows.Application.GetResourceStream(iconUri);

            _notifyIcon = new NotifyIcon
            {
                Icon = streamResourceInfo != null
                    ? new System.Drawing.Icon(streamResourceInfo.Stream)
                    : System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "GhostDraw - Press Ctrl+Alt+D to draw"
            };

            var contextMenu = new ContextMenuStrip();

            // Add log level submenu
            var logLevelMenu = new ToolStripMenuItem("Log Level");
            foreach (var level in LoggingSettingsService.GetAvailableLogLevels())
            {
                var levelItem = new ToolStripMenuItem(LoggingSettingsService.GetLogLevelDisplayName(level))
                {
                    Checked = _loggingSettings.CurrentLevel == level
                };
                levelItem.Click += (s, args) =>
                {
                    _loggingSettings.SetLogLevel(level);
                    UpdateLogLevelMenuChecks(logLevelMenu, level);
                };
                logLevelMenu.DropDownItems.Add(levelItem);
            }
            contextMenu.Items.Add(logLevelMenu);

            contextMenu.Items.Add("Settings...", null, (s, args) =>
            {
                try
                {
                    _logger?.LogDebug("Opening settings window");
                    var settingsWindow = _serviceProvider!.GetRequiredService<SettingsWindow>();
                    settingsWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    _exceptionHandler?.HandleException(ex, "Settings window");
                }
            });

            contextMenu.Items.Add("Open Log Folder", null, (s, args) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", _loggingSettings!.GetLogDirectory());
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to open log folder");
                }
            });

            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, args) => Shutdown());
            _notifyIcon.ContextMenuStrip = contextMenu;

            _notifyIcon.DoubleClick += (s, args) =>
            {
                try
                {
                    _logger?.LogDebug("System tray icon double-clicked");
                    System.Windows.MessageBox.Show(
                        $"GhostDraw is running!\n\nPress and hold Ctrl+Alt+D, then click and drag with left mouse button to draw on screen.\nRelease Ctrl+Alt+D to clear the drawing.\n\nLog Level: {_loggingSettings?.CurrentLevel}\nLog Folder: {_loggingSettings?.GetLogDirectory()}",
                        "GhostDraw",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error showing info dialog");
                }
            };

            _logger.LogInformation("System tray icon created and configured");
            _logger.LogInformation("===== App initialization complete =====");
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "CRITICAL ERROR during startup");

            // Try emergency state reset even during startup failure
            try
            {
                _exceptionHandler?.EmergencyStateReset("Startup failure");
            }
            catch
            {
                // Ignore if emergency reset fails during startup
            }

            System.Windows.MessageBox.Show(
                $"Failed to start GhostDraw:\n\n{ex.Message}\n\nCheck logs at: {_loggingSettings?.GetLogDirectory()}",
                "GhostDraw Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void UpdateLogLevelMenuChecks(ToolStripMenuItem logLevelMenu, LogEventLevel selectedLevel)
    {
        foreach (ToolStripMenuItem item in logLevelMenu.DropDownItems)
        {
            if (item.Text != null)
            {
                item.Checked = item.Text.StartsWith(selectedLevel.ToString());
            }
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("Hotkey pressed - enabling drawing mode");
            _drawingManager?.EnableDrawing();
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Hotkey pressed handler");
        }
    }

    private void OnHotkeyReleased(object? sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("Hotkey released - disabling drawing mode");
            _drawingManager?.DisableDrawing();
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Hotkey released handler");
        }
    }

    private void OnEscapePressed(object? sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("ESC pressed - force disabling drawing mode");
            _drawingManager?.ForceDisableDrawing();
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Escape pressed handler");
        }
    }

    private void OnClearCanvasPressed(object? sender, EventArgs e)
    {
        try
        {
            // Only clear canvas if drawing mode is active
            if (_drawingManager?.IsDrawingMode == true)
            {
                _logger?.LogInformation("R pressed - clearing canvas");
                _drawingManager?.ClearCanvas();
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Clear canvas handler");
        }
    }

    private void OnPenToolPressed(object? sender, EventArgs e)
    {
        try
        {
            // Only switch to pen tool if drawing mode is active
            if (_drawingManager?.IsDrawingMode == true)
            {
                _logger?.LogInformation("P pressed - selecting pen tool");
                _drawingManager?.SetPenTool();
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Pen tool handler");
        }
    }

    private void OnLineToolPressed(object? sender, EventArgs e)
    {
        try
        {
            // Only switch to line tool if drawing mode is active
            if (_drawingManager?.IsDrawingMode == true)
            {
                _logger?.LogInformation("L pressed - selecting line tool");
                _drawingManager?.SetLineTool();
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Line tool handler");
        }
    }

    private void OnEraserToolPressed(object? sender, EventArgs e)
    {
        try
        {
            // Only switch to eraser tool if drawing mode is active
            if (_drawingManager?.IsDrawingMode == true)
            {
                _logger?.LogInformation("E pressed - selecting eraser tool");
                _drawingManager?.SetEraserTool();
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Eraser tool handler");
        }
    }

    private void OnRectangleToolPressed(object? sender, EventArgs e)
    {
        try
        {
            // Only switch to rectangle tool if drawing mode is active
            if (_drawingManager?.IsDrawingMode == true)
            {
                _logger?.LogInformation("U pressed - selecting rectangle tool");
                _drawingManager?.SetRectangleTool();
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Rectangle tool handler");
        }
    }

    private void OnCircleToolPressed(object? sender, EventArgs e)
    {
        try
        {
            // Only switch to circle tool if drawing mode is active
            if (_drawingManager?.IsDrawingMode == true)
            {
                _logger?.LogInformation("C pressed - selecting circle tool");
                _drawingManager?.SetCircleTool();
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Circle tool handler");
        }
    }

    private void OnHelpPressed(object? sender, EventArgs e)
    {
        try
        {
            // Show help overlay if drawing mode is active
            if (_drawingManager?.IsDrawingMode == true)
            {
                _logger?.LogInformation("F1 pressed - showing help");
                _drawingManager?.ShowHelp();
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler?.HandleException(ex, "Help pressed handler");
        }
    }

    private void OnScreenshotFullPressed(object? sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("====== OnScreenshotFullPressed CALLED ======");
            _logger?.LogInformation("DrawingManager null: {IsNull}", _drawingManager == null);
            _logger?.LogInformation("DrawingManager.IsDrawingMode: {IsDrawingMode}", _drawingManager?.IsDrawingMode);
            
            // Capture full screenshot if drawing mode is active
            if (_drawingManager?.IsDrawingMode == true)
            {
                _logger?.LogInformation("Ctrl+S pressed - capturing full screenshot (calling DrawingManager.CaptureFullScreenshot)");
                _drawingManager?.CaptureFullScreenshot();
                _logger?.LogInformation("DrawingManager.CaptureFullScreenshot call completed");
            }
            else
            {
                _logger?.LogWarning("Screenshot ignored - drawing mode is NOT active");
            }
            _logger?.LogInformation("====== OnScreenshotFullPressed COMPLETED ======");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception in OnScreenshotFullPressed");
            _exceptionHandler?.HandleException(ex, "Screenshot full pressed handler");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("Application exiting");

        try
        {
            // Unregister exception handlers first
            _exceptionHandler?.UnregisterHandlers();

            // Cleanup in proper order
            _keyboardHook?.Dispose();
            _notifyIcon?.Dispose();
            ServiceConfiguration.Shutdown();
            _serviceProvider?.Dispose();

            // Release single instance mutex
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during application exit");
        }

        base.OnExit(e);
    }
}
