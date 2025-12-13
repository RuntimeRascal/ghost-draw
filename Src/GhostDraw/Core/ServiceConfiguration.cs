using System.IO;
using GhostDraw.Helpers;
using GhostDraw.Managers;
using GhostDraw.Services;
using GhostDraw.ViewModels;
using GhostDraw.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace GhostDraw.Core;

public static class ServiceConfiguration
{
    private static ServiceProvider? _serviceProvider;
    private static Serilog.Core.LoggingLevelSwitch _levelSwitch = new(LogEventLevel.Information);
    private static Microsoft.Extensions.Logging.ILogger? _configLogger;

    public static ServiceProvider ConfigureServices()
    {
        // Setup Serilog
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string logDirectory = Path.Combine(appData, "GhostDraw");
        Directory.CreateDirectory(logDirectory);

        string logFilePath = Path.Combine(logDirectory, "ghostdraw-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10_485_760) // 10 MB
            .CreateLogger();

        // Setup DI container
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Register settings store (file-based for production)
        services.AddSingleton<ISettingsStore, FileSettingsStore>();

        // Register application services (order matters for dependencies)
        services.AddSingleton<AppSettingsService>();
        services.AddSingleton<CursorHelper>();
        services.AddSingleton<ScreenshotService>();

        // Register drawing tools
        services.AddSingleton<GhostDraw.Tools.PenTool>();
        services.AddSingleton<GhostDraw.Tools.LineTool>();
        services.AddSingleton<GhostDraw.Tools.EraserTool>();
        services.AddSingleton<GhostDraw.Tools.RectangleTool>();
        services.AddSingleton<GhostDraw.Tools.CircleTool>();

        services.AddSingleton<OverlayWindow>();
        services.AddSingleton<IOverlayWindow>(sp => sp.GetRequiredService<OverlayWindow>());
        services.AddSingleton<GlobalKeyboardHook>();
        services.AddSingleton<DrawingManager>();
        services.AddSingleton<LoggingSettingsService>();

        // Register GlobalExceptionHandler AFTER its dependencies
        services.AddSingleton<GlobalExceptionHandler>();

        // Register ViewModels and Views
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Load saved log level from settings
        var appSettings = _serviceProvider.GetRequiredService<AppSettingsService>();
        if (Enum.TryParse<LogEventLevel>(appSettings.CurrentSettings.LogLevel, out var savedLevel))
        {
            _levelSwitch.MinimumLevel = savedLevel;
        }

        // Configure hotkey from settings
        var keyboardHook = _serviceProvider.GetRequiredService<GlobalKeyboardHook>();
        keyboardHook.Configure(appSettings.CurrentSettings.HotkeyVirtualKeys);

        // Subscribe to hotkey changes for real-time reconfiguration
        appSettings.HotkeyChanged += (sender, vks) =>
        {
            _configLogger?.LogInformation("Hotkey configuration changed, reconfiguring hook");
            keyboardHook.Configure(vks);
        };

        // Get logger for configuration logging
        _configLogger = _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Configuration");
        _configLogger.LogInformation("=== GhostDraw Started at {StartTime} ===", DateTime.Now);
        _configLogger.LogInformation("Log directory: {LogDirectory}", logDirectory);
        _configLogger.LogInformation("Current log level: {LogLevel}", _levelSwitch.MinimumLevel);
        _configLogger.LogInformation("Hotkey: {Hotkey}", appSettings.CurrentSettings.HotkeyDisplayName);

        return _serviceProvider;
    }

    public static void SetLogLevel(LogEventLevel level)
    {
        _levelSwitch.MinimumLevel = level;
        _configLogger?.LogInformation("Log level changed to: {LogLevel}", level);
    }

    public static LogEventLevel GetLogLevel() => _levelSwitch.MinimumLevel;

    public static string GetLogDirectory()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "GhostDraw");
    }

    public static void Shutdown()
    {
        _configLogger?.LogInformation("=== GhostDraw Shutdown at {StopTime} ===", DateTime.Now);
        _serviceProvider?.Dispose();
        Log.CloseAndFlush();
    }
}
