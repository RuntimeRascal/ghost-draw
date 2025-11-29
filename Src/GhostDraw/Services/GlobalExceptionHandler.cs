using GhostDraw.Core;
using GhostDraw.Managers;
using Microsoft.Extensions.Logging;

namespace GhostDraw.Services
{
    /// <summary>
    /// Global exception handler that ensures system safety by resetting application state
    /// and releasing all hooks when unhandled exceptions occur.
    /// 
    /// CRITICAL SAFETY COMPONENT: This prevents the user from being locked out of their system
    /// if the application crashes while drawing mode is active or hooks are installed.
    /// </summary>
    public class GlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly DrawingManager _drawingManager;
        private readonly GlobalKeyboardHook _keyboardHook;
        private readonly AppSettingsService _settingsService;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            DrawingManager drawingManager,
            GlobalKeyboardHook keyboardHook,
            AppSettingsService settingsService)
        {
            _logger = logger;
            _drawingManager = drawingManager;
            _keyboardHook = keyboardHook;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Registers all global exception handlers
        /// </summary>
        public void RegisterHandlers()
        {
            _logger.LogInformation("Registering global exception handlers");

            // Handle exceptions on the UI thread
            System.Windows.Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Handle exceptions on background threads
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Handle unobserved task exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _logger.LogInformation("Global exception handlers registered successfully");
        }

        /// <summary>
        /// Unregisters all global exception handlers (called during shutdown)
        /// </summary>
        public void UnregisterHandlers()
        {
            try
            {
                System.Windows.Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

                _logger.LogInformation("Global exception handlers unregistered");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering exception handlers");
            }
        }

        /// <summary>
        /// Handles exceptions on the UI thread (Dispatcher)
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.LogCritical(e.Exception, "Unhandled exception on UI thread");

            try
            {
                // Emergency state reset
                EmergencyStateReset("UI thread exception");

                // Show user-friendly error message
                ShowErrorNotification(e.Exception);

                // Mark as handled to prevent application crash
                e.Handled = true;
            }
            catch (Exception resetException)
            {
                // If even the emergency reset fails, log it but don't throw
                _logger.LogCritical(resetException, "CRITICAL: Emergency state reset failed");

                // Don't mark as handled - let the application crash gracefully
                // This is safer than leaving hooks active
            }
        }

        /// <summary>
        /// Handles exceptions on background threads
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            _logger.LogCritical(exception, "Unhandled exception on background thread. IsTerminating: {IsTerminating}",
                e.IsTerminating);

            try
            {
                // Emergency state reset
                EmergencyStateReset("Background thread exception");

                // If we're not terminating, show notification
                if (!e.IsTerminating)
                {
                    System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
                    {
                        ShowErrorNotification(exception);
                    });
                }
            }
            catch (Exception resetException)
            {
                _logger.LogCritical(resetException, "CRITICAL: Emergency state reset failed in background thread handler");
            }
        }

        /// <summary>
        /// Handles unobserved task exceptions
        /// </summary>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "Unobserved task exception");

            try
            {
                // For task exceptions, just log and mark as observed
                // These are typically not critical to system safety
                e.SetObserved();

                // Log all inner exceptions
                foreach (var innerException in e.Exception.InnerExceptions)
                {
                    _logger.LogError(innerException, "Inner exception from unobserved task");
                }
            }
            catch (Exception logException)
            {
                // Even logging failed - just continue
                System.Diagnostics.Debug.WriteLine($"Failed to log unobserved task exception: {logException}");
            }
        }

        /// <summary>
        /// Performs emergency state reset to ensure system remains usable
        /// CRITICAL SAFETY METHOD - Must never throw exceptions
        /// </summary>
        public void EmergencyStateReset(string reason)
        {
            _logger.LogWarning("EMERGENCY STATE RESET initiated. Reason: {Reason}", reason);

            // Track what was reset for logging
            var resetActions = new System.Collections.Generic.List<string>();

            try
            {
                // 1. Disable drawing mode (most critical - releases mouse capture)
                try
                {
                    if (_drawingManager.IsDrawingMode)
                    {
                        _drawingManager.DisableDrawingMode();
                        resetActions.Add("Drawing mode disabled");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to disable drawing mode during emergency reset");
                }

                // 2. Release keyboard hooks (critical for system safety)
                try
                {
                    _keyboardHook.Dispose();
                    resetActions.Add("Keyboard hooks released");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to release keyboard hooks during emergency reset");
                }

                // 3. Reset lock mode if enabled
                try
                {
                    var currentSettings = _settingsService.CurrentSettings;
                    if (currentSettings.LockDrawingMode)
                    {
                        _settingsService.SetLockDrawingMode(false);
                        resetActions.Add("Lock mode disabled");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reset lock mode during emergency reset");
                }

                // 4. Ensure overlay is hidden (secondary safety)
                try
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow;
                        if (mainWindow != null)
                        {
                            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                            {
                                if (window is OverlayWindow overlay)
                                {
                                    overlay.Hide();
                                    resetActions.Add("Overlay window hidden");
                                }
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to hide overlay during emergency reset");
                }

                _logger.LogWarning("Emergency state reset completed. Actions taken: {Actions}",
                    string.Join(", ", resetActions));
            }
            catch (Exception ex)
            {
                // Even the emergency reset failed - this is very bad
                _logger.LogCritical(ex, "CRITICAL: Emergency state reset itself failed");

                // Last resort: try to log to Windows Event Log
                try
                {
                    System.Diagnostics.EventLog.WriteEntry("GhostDraw",
                        $"CRITICAL FAILURE: Emergency state reset failed. Exception: {ex.Message}",
                        System.Diagnostics.EventLogEntryType.Error);
                }
                catch
                {
                    // Nothing more we can do
                }
            }
        }

        /// <summary>
        /// Shows a user-friendly error notification
        /// </summary>
        private void ShowErrorNotification(Exception? exception)
        {
            try
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    var message = "GhostDraw encountered an unexpected error and has reset to a safe state.\n\n" +
                                  "• Drawing mode has been disabled\n" +
                                  "• All keyboard hooks have been released\n" +
                                  "• Your system should be fully responsive\n\n" +
                                  $"Error: {exception?.Message ?? "Unknown error"}\n\n" +
                                  "You can continue using GhostDraw, or restart the application for a fresh start.";

                    System.Windows.MessageBox.Show(message,
                        "GhostDraw - Error Recovery",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                });
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, "Failed to show error notification");

                // Try simple message box as fallback
                try
                {
                    System.Windows.MessageBox.Show(
                        "GhostDraw encountered an error and has been reset to a safe state.",
                        "GhostDraw - Error Recovery",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
                catch
                {
                    // Give up on notifications
                }
            }
        }

        /// <summary>
        /// Manual exception handler for critical operations
        /// Call this in try-catch blocks where you need consistent error handling
        /// </summary>
        public void HandleException(Exception exception, string context)
        {
            _logger.LogError(exception, "Exception in {Context}", context);

            // Determine if this is critical enough to warrant emergency reset
            if (IsSystemSafetyCritical(exception, context))
            {
                _logger.LogWarning("Exception is system-safety critical, performing emergency reset");
                EmergencyStateReset($"Critical exception in {context}");
                ShowErrorNotification(exception);
            }
        }

        /// <summary>
        /// Determines if an exception is critical enough to warrant emergency state reset
        /// </summary>
        private bool IsSystemSafetyCritical(Exception exception, string context)
        {
            // Critical contexts that could lock out the user
            var criticalContexts = new[]
            {
                "hook callback",
                "keyboard hook",
                "mouse hook",
                "overlay",
                "drawing mode",
                "input capture"
            };

            var contextLower = context.ToLowerInvariant();
            foreach (var critical in criticalContexts)
            {
                if (contextLower.Contains(critical))
                {
                    return true;
                }
            }

            // Critical exception types
            if (exception is OutOfMemoryException ||
                exception is System.Runtime.InteropServices.ExternalException ||
                exception is InvalidOperationException && contextLower.Contains("hook"))
            {
                return true;
            }

            return false;
        }
    }
}
