using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using WpfCursor = System.Windows.Input.Cursor;
using WpfCursors = System.Windows.Input.Cursors;

namespace GhostDraw.Helpers
{
    /// <summary>
    /// Helper class for creating custom cursors with colored tips
    /// </summary>
    public class CursorHelper : IDisposable
    {
        private readonly ILogger<CursorHelper> _logger;
        private nint _currentCursorHandle = nint.Zero;
        private readonly object _cursorLock = new object();
        private bool _disposed = false;

        public CursorHelper(ILogger<CursorHelper> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a pencil cursor with a colored tip
        /// </summary>
        /// <param name="tipColorHex">Hex color for the pencil tip (e.g., "#FF0000")</param>
        /// <returns>Custom cursor</returns>
        public WpfCursor CreateColoredPencilCursor(string tipColorHex)
        {
            lock (_cursorLock)
            {
                if (_disposed)
                {
                    _logger.LogWarning("CreateColoredPencilCursor called on disposed CursorHelper");
                    return WpfCursors.Pen;
                }

                try
                {
                    _logger.LogDebug("Creating colored pencil cursor with tip color {Color}", tipColorHex);

                    // Destroy previous cursor handle to prevent leaks
                    if (_currentCursorHandle != nint.Zero)
                    {
                        try
                        {
                            DestroyCursor(_currentCursorHandle);
                            _logger.LogDebug("Destroyed previous cursor handle");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to destroy previous cursor handle");
                        }
                        _currentCursorHandle = nint.Zero;
                    }

                    // Create a bitmap for the cursor (32x32 pixels)
                    int size = 32;
                    using (Bitmap bitmap = new Bitmap(size, size))
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.Clear(Color.Transparent);

                        // Parse the tip color
                        Color tipColor = ColorTranslator.FromHtml(tipColorHex);

                        // Draw pencil body (gray with slight gradient)
                        using (LinearGradientBrush pencilBrush = new LinearGradientBrush(
                            new Rectangle(8, 2, 10, 20),
                            Color.FromArgb(180, 180, 180),
                            Color.FromArgb(120, 120, 120),
                            45f))
                        {
                            // Pencil shaft (slightly angled)
                            PointF[] pencilBody = new PointF[]
                            {
                                new PointF(14, 2),   // Top left
                                new PointF(18, 2),   // Top right
                                new PointF(16, 18),  // Bottom right
                                new PointF(12, 18)   // Bottom left
                            };
                            g.FillPolygon(pencilBrush, pencilBody);

                            // Pencil outline
                            using (Pen outlinePen = new Pen(Color.Black, 1))
                            {
                                g.DrawPolygon(outlinePen, pencilBody);
                            }
                        }

                        // Draw eraser at top (pink)
                        using (SolidBrush eraserBrush = new SolidBrush(Color.FromArgb(255, 192, 203)))
                        {
                            g.FillRectangle(eraserBrush, 13, 0, 6, 3);
                        }

                        // Draw metal ferrule
                        using (SolidBrush metalBrush = new SolidBrush(Color.FromArgb(192, 192, 192)))
                        {
                            g.FillRectangle(metalBrush, 12, 3, 8, 2);
                        }

                        // Draw colored pencil tip (triangular point)
                        PointF[] pencilTip = new PointF[]
                        {
                            new PointF(12, 18),  // Top left of tip
                            new PointF(16, 18),  // Top right of tip
                            new PointF(14, 24)   // Sharp point
                        };

                        using (SolidBrush tipBrush = new SolidBrush(tipColor))
                        {
                            g.FillPolygon(tipBrush, pencilTip);
                        }

                        // Tip outline
                        using (Pen tipOutline = new Pen(Color.Black, 1.5f))
                        {
                            g.DrawPolygon(tipOutline, pencilTip);
                        }

                        // Add a small white highlight for depth
                        using (Pen highlight = new Pen(Color.White, 1))
                        {
                            g.DrawLine(highlight, 15, 4, 15, 16);
                        }

                        // Convert bitmap to cursor
                        nint hCursor = CreateCursorFromBitmap(bitmap, 14, 24); // Hotspot at tip

                        if (hCursor != nint.Zero)
                        {
                            _currentCursorHandle = hCursor;
                            _logger.LogDebug("Successfully created custom cursor (handle: {Handle})", hCursor);

                            // CRITICAL FIX: Use SafeCursorHandle instead of SafeFileHandle
                            // A cursor handle is NOT a file handle!
                            return System.Windows.Interop.CursorInteropHelper.Create(new SafeCursorHandle(hCursor));
                        }
                    }

                    _logger.LogWarning("Failed to create custom cursor, returning default");
                    return WpfCursors.Pen;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating colored pencil cursor, using default");
                    return WpfCursors.Pen;
                }
            }
        }

        private nint CreateCursorFromBitmap(Bitmap bitmap, int hotspotX, int hotspotY)
        {
            nint hIcon = nint.Zero;
            ICONINFO iconInfo = new ICONINFO();

            try
            {
                // Get the icon handle from the bitmap
                hIcon = bitmap.GetHicon();

                // Create ICONINFO structure
                if (!GetIconInfo(hIcon, ref iconInfo))
                {
                    _logger.LogError("GetIconInfo failed");
                    return nint.Zero;
                }

                // Set as cursor (not icon)
                iconInfo.fIcon = false;
                iconInfo.xHotspot = hotspotX;
                iconInfo.yHotspot = hotspotY;

                // Create cursor from icon info
                nint hCursor = CreateIconIndirect(ref iconInfo);

                return hCursor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create cursor from bitmap");
                return nint.Zero;
            }
            finally
            {
                // Cleanup temporary resources
                try
                {
                    if (iconInfo.hbmMask != nint.Zero)
                    {
                        DeleteObject(iconInfo.hbmMask);
                    }
                    if (iconInfo.hbmColor != nint.Zero)
                    {
                        DeleteObject(iconInfo.hbmColor);
                    }
                    if (hIcon != nint.Zero)
                    {
                        DestroyIcon(hIcon);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Error during cursor creation cleanup");
                }
            }
        }

        public void Dispose()
        {
            lock (_cursorLock)
            {
                if (_disposed)
                {
                    return;
                }

                _logger.LogDebug("Disposing CursorHelper");
                _disposed = true;

                // Destroy current cursor handle
                if (_currentCursorHandle != nint.Zero)
                {
                    try
                    {
                        DestroyCursor(_currentCursorHandle);
                        _logger.LogDebug("Destroyed cursor handle on dispose");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to destroy cursor handle during dispose");
                    }
                    _currentCursorHandle = nint.Zero;
                }
            }
        }

        #region Win32 API

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public nint hbmMask;
            public nint hbmColor;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetIconInfo(nint hIcon, ref ICONINFO pIconInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint CreateIconIndirect(ref ICONINFO icon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(nint hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyCursor(nint hCursor);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(nint hObject);

        #endregion
    }

    /// <summary>
    /// SafeHandle for cursor handles (not file handles!)
    /// </summary>
    internal class SafeCursorHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeCursorHandle(nint handle) : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            // Use DestroyCursor, not CloseHandle!
            return DestroyCursor(handle);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyCursor(nint hCursor);
    }
}
