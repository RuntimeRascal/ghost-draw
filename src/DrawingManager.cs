using System.Runtime.InteropServices;
using System.Windows;
using GhostDraw.Services;
using Microsoft.Extensions.Logging;

namespace GhostDraw
{
    public class DrawingManager
    {
        private readonly ILogger<DrawingManager> _logger;
        private readonly OverlayWindow _overlayWindow;
        private readonly AppSettingsService _appSettings;
        private bool _isDrawingLocked = false;

        public bool IsDrawingMode => _overlayWindow.IsVisible || _isDrawingLocked;

        public DrawingManager(ILogger<DrawingManager> logger, OverlayWindow overlayWindow, AppSettingsService appSettings)
        {
            _logger = logger;
            _overlayWindow = overlayWindow;
            _appSettings = appSettings;
            
            // Initialize lock mode state from saved settings
            _isDrawingLocked = _appSettings.CurrentSettings.LockDrawingMode;
            
            _logger.LogDebug("DrawingManager initialized - LockDrawingMode={LockMode}", _isDrawingLocked);
        }

        public void EnableDrawing()
        {
            try
            {
                var settings = _appSettings.CurrentSettings;
                
                if (settings.LockDrawingMode)
                {
                    // Toggle mode: switch between locked on/off
                    if (_isDrawingLocked)
                    {
                        _logger.LogInformation("Toggling drawing mode OFF (was locked)");
                        _isDrawingLocked = false;
                        _overlayWindow.DisableDrawing();
                        _overlayWindow.Hide();
                    }
                    else
                    {
                        _logger.LogInformation("Toggling drawing mode ON (locking)");
                        _isDrawingLocked = true;
                        _overlayWindow.EnableDrawing();
                        _overlayWindow.Show();
                        _overlayWindow.Activate();
                        _overlayWindow.Focus();
                    }
                }
                else
                {
                    // Hold mode: enable drawing only while hotkey is held
                    _logger.LogInformation("Enabling drawing mode (hold)");
                    _overlayWindow.EnableDrawing();
                    _overlayWindow.Show();
                    _overlayWindow.Activate();
                    _overlayWindow.Focus();
                }
                
                _logger.LogDebug("Overlay shown - IsVisible={IsVisible}, IsActive={IsActive}, IsFocused={IsFocused}", 
                    _overlayWindow.IsVisible, _overlayWindow.IsActive, _overlayWindow.IsFocused);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable drawing mode");
                // Try to ensure clean state
                try
                {
                    _overlayWindow.DisableDrawing();
                    _overlayWindow.Hide();
                    _isDrawingLocked = false;
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to cleanup after enable drawing error");
                }
                throw; // Re-throw so GlobalExceptionHandler can handle it
            }
        }

        public void DisableDrawing()
        {
            try
            {
                var settings = _appSettings.CurrentSettings;
                
                if (settings.LockDrawingMode)
                {
                    // In lock mode, DisableDrawing is only called from EnableDrawing toggle
                    // or from ESC key (which we'll handle separately)
                    // So we ignore hotkey release events
                    _logger.LogDebug("Ignoring disable request in lock mode (drawing is locked)");
                    return;
                }
                
                // Hold mode: disable when hotkey is released
                _logger.LogInformation("Disabling drawing mode (hold released)");
                _overlayWindow.DisableDrawing();
                _overlayWindow.Hide();
                _logger.LogDebug("Overlay hidden");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable drawing mode");
                // Try to ensure overlay is hidden
                try
                {
                    _overlayWindow.Hide();
                }
                catch (Exception hideEx)
                {
                    _logger.LogError(hideEx, "Failed to hide overlay after disable drawing error");
                }
                throw; // Re-throw so GlobalExceptionHandler can handle it
            }
        }

        public void ForceDisableDrawing()
        {
            try
            {
                _logger.LogInformation("Force disabling drawing mode (ESC pressed)");
                _isDrawingLocked = false;
                _overlayWindow.DisableDrawing();
                _overlayWindow.Hide();
                _logger.LogDebug("Drawing mode force disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to force disable drawing mode");
                // This is critical - make one last attempt
                try
                {
                    _isDrawingLocked = false;
                    _overlayWindow.Hide();
                }
                catch
                {
                    // Nothing more we can do
                    _logger.LogCritical("CRITICAL: Failed to hide overlay after force disable");
                }
                // Don't re-throw - this is emergency exit
            }
        }

        public void DisableDrawingMode()
        {
            // Public method for emergency state reset
            try
            {
                _logger.LogWarning("DisableDrawingMode called (likely from emergency reset)");
                _isDrawingLocked = false;
                _overlayWindow.DisableDrawing();
                _overlayWindow.Hide();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed in DisableDrawingMode");
                // Don't throw - this is for emergency cleanup
            }
        }
    }
}
