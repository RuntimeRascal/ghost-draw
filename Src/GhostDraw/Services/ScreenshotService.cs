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
    /// Captures a fullscreen screenshot including the overlay drawing.
    /// The capture is taken for the entire virtual screen and does not
    /// depend on a specific window instance.
    /// </summary>
    /// <returns>Path to saved screenshot file, or null if failed</returns>
    public string? CaptureFullScreen()
    {
        try
        {
            _logger.LogInformation("====== ScreenshotService.CaptureFullScreen CALLED ======");
            _logger.LogInformation("Capturing fullscreen screenshot");

            // Get screen dimensions
            var bounds = new System.Drawing.Rectangle(
                (int)SystemParameters.VirtualScreenLeft,
                (int)SystemParameters.VirtualScreenTop,
                (int)SystemParameters.VirtualScreenWidth,
                (int)SystemParameters.VirtualScreenHeight);

            _logger.LogInformation("Screen bounds: X={X}, Y={Y}, Width={Width}, Height={Height}",
                bounds.X, bounds.Y, bounds.Width, bounds.Height);

            // Create bitmap for screen capture
            _logger.LogInformation("Creating bitmap for screen capture");
            using var bitmap = new System.Drawing.Bitmap(bounds.Width, bounds.Height);

            _logger.LogInformation("Copying screen to bitmap using Graphics.CopyFromScreen");
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                // Copy screen to bitmap
                graphics.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
            }
            _logger.LogInformation("Screen copied to bitmap successfully");

            // Save the screenshot
            _logger.LogInformation("Calling SaveScreenshot to save bitmap to file");
            var filePath = SaveScreenshot(bitmap);
            _logger.LogInformation("SaveScreenshot returned: {FilePath}", filePath ?? "(null)");

            if (filePath != null)
            {
                var settings = _appSettings.CurrentSettings;
                _logger.LogInformation("Screenshot settings - CopyToClipboard: {Copy}, OpenFolder: {Open}, PlaySound: {Sound}",
                    settings.CopyScreenshotToClipboard, settings.OpenFolderAfterScreenshot, settings.PlayShutterSound);

                // Copy to clipboard if enabled
                if (settings.CopyScreenshotToClipboard)
                {
                    _logger.LogInformation("Copying screenshot to clipboard");
                    CopyToClipboard(bitmap);
                }

                // Open folder if enabled
                if (settings.OpenFolderAfterScreenshot)
                {
                    _logger.LogInformation("Opening folder containing screenshot");
                    OpenFolder(filePath);
                }

                // Play shutter sound if enabled
                if (settings.PlayShutterSound)
                {
                    _logger.LogInformation("Playing shutter sound");
                    PlayShutterSound();
                }

                _logger.LogInformation("Screenshot saved successfully to: {FilePath}", filePath);
            }
            else
            {
                _logger.LogError("Screenshot file path is NULL - save failed");
            }

            _logger.LogInformation("====== ScreenshotService.CaptureFullScreen COMPLETED ======");
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EXCEPTION in CaptureFullScreen: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            return null;
        }
    }

    private string? SaveScreenshot(System.Drawing.Bitmap bitmap)
    {
        try
        {
            _logger.LogInformation("====== SaveScreenshot CALLED ======");
            var settings = _appSettings.CurrentSettings;
            var savePath = settings.ScreenshotSavePath;

            _logger.LogInformation("Screenshot save path from settings: {SavePath}", savePath);
            _logger.LogInformation("Bitmap dimensions: {Width}x{Height}", bitmap.Width, bitmap.Height);

            // Ensure directory exists
            if (!Directory.Exists(savePath))
            {
                _logger.LogInformation("Directory does not exist, creating: {Path}", savePath);
                Directory.CreateDirectory(savePath);
                _logger.LogInformation("Created screenshot directory successfully");
            }
            else
            {
                _logger.LogInformation("Directory already exists: {Path}", savePath);
            }

            // Generate filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"GhostDraw_{timestamp}.png";
            var filePath = Path.Combine(savePath, fileName);

            _logger.LogInformation("Generated filename: {FileName}", fileName);
            _logger.LogInformation("Full file path: {FilePath}", filePath);

            // Save as PNG
            _logger.LogInformation("Saving bitmap to file as PNG...");
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            _logger.LogInformation("Bitmap saved successfully!");

            // Verify file exists
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                _logger.LogInformation("Screenshot file verified - Size: {Size} bytes", fileInfo.Length);
            }
            else
            {
                _logger.LogError("Screenshot file does NOT exist after save!");
            }

            _logger.LogInformation("====== SaveScreenshot COMPLETED ======");
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EXCEPTION in SaveScreenshot: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
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
