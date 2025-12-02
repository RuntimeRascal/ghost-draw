using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GhostDraw.Core;
using Microsoft.Extensions.Logging;

namespace GhostDraw.Services;

/// <summary>
/// Service for capturing screenshots of the overlay and desktop
/// </summary>
public class ScreenshotService
{
    private readonly ILogger<ScreenshotService> _logger;
    private readonly AppSettingsService _appSettings;

    public ScreenshotService(ILogger<ScreenshotService> logger, AppSettingsService appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Captures a fullscreen screenshot including the overlay drawing
    /// </summary>
    /// <param name="overlayWindow">The overlay window containing the drawing</param>
    /// <returns>Path to saved screenshot file, or null if failed</returns>
    public string? CaptureFullScreen(Window overlayWindow)
    {
        try
        {
            _logger.LogInformation("Capturing fullscreen screenshot");

            // Get screen dimensions
            var bounds = new System.Drawing.Rectangle(
                (int)SystemParameters.VirtualScreenLeft,
                (int)SystemParameters.VirtualScreenTop,
                (int)SystemParameters.VirtualScreenWidth,
                (int)SystemParameters.VirtualScreenHeight);

            _logger.LogDebug("Screen bounds: {Bounds}", bounds);

            // Create bitmap for screen capture
            using var bitmap = new System.Drawing.Bitmap(bounds.Width, bounds.Height);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                // Copy screen to bitmap
                graphics.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
            }

            // Save the screenshot
            var filePath = SaveScreenshot(bitmap);

            if (filePath != null)
            {
                // Copy to clipboard if enabled
                if (_appSettings.CurrentSettings.CopyScreenshotToClipboard)
                {
                    CopyToClipboard(bitmap);
                }

                // Open folder if enabled
                if (_appSettings.CurrentSettings.OpenFolderAfterScreenshot)
                {
                    OpenFolder(filePath);
                }

                // Play shutter sound if enabled
                if (_appSettings.CurrentSettings.PlayShutterSound)
                {
                    PlayShutterSound();
                }

                _logger.LogInformation("Screenshot saved to: {FilePath}", filePath);
            }

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture fullscreen screenshot");
            return null;
        }
    }

    /// <summary>
    /// Opens the Windows Snipping Tool for custom area capture
    /// </summary>
    public void OpenSnippingTool()
    {
        try
        {
            _logger.LogInformation("Opening Windows Snipping Tool");

            // Try SnippingTool.exe first (Windows 10)
            var snippingToolPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "SnippingTool.exe");

            if (File.Exists(snippingToolPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = snippingToolPath,
                    UseShellExecute = true
                });
                _logger.LogDebug("Started SnippingTool.exe");
            }
            else
            {
                // Try Snip & Sketch (Windows 10/11) - ms-screenclip: protocol
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-screenclip:",
                    UseShellExecute = true
                });
                _logger.LogDebug("Started Snip & Sketch via ms-screenclip:");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open snipping tool");
        }
    }

    private string? SaveScreenshot(System.Drawing.Bitmap bitmap)
    {
        try
        {
            var settings = _appSettings.CurrentSettings;
            var savePath = settings.ScreenshotSavePath;

            // Ensure directory exists
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                _logger.LogInformation("Created screenshot directory: {Path}", savePath);
            }

            // Generate filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"GhostDraw_{timestamp}.png";
            var filePath = Path.Combine(savePath, fileName);

            // Save as PNG
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

            _logger.LogInformation("Screenshot saved: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save screenshot");
            return null;
        }
    }

    private void CopyToClipboard(System.Drawing.Bitmap bitmap)
    {
        try
        {
            // Convert System.Drawing.Bitmap to WPF BitmapSource
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            // Determine WPF pixel format based on bitmap format
            PixelFormat pixelFormat = bitmap.PixelFormat switch
            {
                System.Drawing.Imaging.PixelFormat.Format32bppArgb => PixelFormats.Bgra32,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb => PixelFormats.Bgr24,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb => PixelFormats.Bgr32,
                _ => PixelFormats.Bgra32 // Default fallback
            };

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                96, 96,
                pixelFormat,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            // Copy to clipboard
            System.Windows.Clipboard.SetImage(bitmapSource);
            _logger.LogDebug("Screenshot copied to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy screenshot to clipboard");
        }
    }

    private void OpenFolder(string filePath)
    {
        try
        {
            // Open folder and select the file
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            _logger.LogDebug("Opened folder with file selected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder");
        }
    }

    private void PlayShutterSound()
    {
        try
        {
            // Play system asterisk sound as shutter sound
            System.Media.SystemSounds.Asterisk.Play();
            _logger.LogDebug("Played shutter sound");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play shutter sound");
        }
    }
}
