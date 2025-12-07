using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using WpfCursor = System.Windows.Input.Cursor;
using WpfCursors = System.Windows.Input.Cursors;

namespace GhostDraw.Helpers;

/// <summary>
/// Helper class for creating custom cursors with colored tips
/// </summary>
public class CursorHelper(ILogger<CursorHelper> logger) : IDisposable
{
    private readonly ILogger<CursorHelper> _logger = logger;
    private nint _currentCursorHandle = nint.Zero;
    private readonly object _cursorLock = new();
    private bool _disposed = false;

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

    /// <summary>
    /// Creates an eraser cursor
    /// </summary>
    /// <returns>Custom eraser cursor</returns>
    public WpfCursor CreateEraserCursor()
    {
        lock (_cursorLock)
        {
            if (_disposed)
            {
                _logger.LogWarning("CreateEraserCursor called on disposed CursorHelper");
                return WpfCursors.Cross;
            }

            try
            {
                _logger.LogDebug("Creating eraser cursor");

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

                    // Draw eraser shape (rectangle with slight perspective)
                    int eraserWidth = 16;
                    int eraserHeight = 12;
                    int eraserLeft = (size - eraserWidth) / 2;
                    int eraserTop = (size - eraserHeight) / 2 - 2;

                    // Draw eraser body (pink/beige color like a traditional eraser)
                    using (LinearGradientBrush eraserBrush = new LinearGradientBrush(
                        new Rectangle(eraserLeft, eraserTop, eraserWidth, eraserHeight),
                        Color.FromArgb(255, 220, 220),
                        Color.FromArgb(255, 180, 180),
                        45f))
                    {
                        g.FillRectangle(eraserBrush, eraserLeft, eraserTop, eraserWidth, eraserHeight);
                    }

                    // Draw eraser outline
                    using (Pen outlinePen = new Pen(Color.Black, 1.5f))
                    {
                        g.DrawRectangle(outlinePen, eraserLeft, eraserTop, eraserWidth, eraserHeight);
                    }

                    // Draw diagonal lines to give texture
                    using (Pen texturePen = new Pen(Color.FromArgb(100, 200, 150, 150), 1))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int offset = i * 5;
                            g.DrawLine(texturePen, 
                                eraserLeft + offset, eraserTop, 
                                eraserLeft + offset + 6, eraserTop + eraserHeight);
                        }
                    }

                    // Add highlight for depth
                    using (Pen highlight = new Pen(Color.White, 1))
                    {
                        g.DrawLine(highlight, eraserLeft + 2, eraserTop + 2, eraserLeft + eraserWidth - 4, eraserTop + 2);
                    }

                    // Draw small "eraser particles" below to indicate erasing action
                    using (SolidBrush particleBrush = new SolidBrush(Color.FromArgb(150, 180, 180, 180)))
                    {
                        g.FillEllipse(particleBrush, eraserLeft + 3, eraserTop + eraserHeight + 2, 2, 2);
                        g.FillEllipse(particleBrush, eraserLeft + 8, eraserTop + eraserHeight + 4, 2, 2);
                        g.FillEllipse(particleBrush, eraserLeft + 12, eraserTop + eraserHeight + 3, 2, 2);
                    }

                    // Convert bitmap to cursor with hotspot at center
                    nint hCursor = CreateCursorFromBitmap(bitmap, size / 2, size / 2);

                    if (hCursor != nint.Zero)
                    {
                        _currentCursorHandle = hCursor;
                        _logger.LogDebug("Successfully created eraser cursor (handle: {Handle})", hCursor);

                        return System.Windows.Interop.CursorInteropHelper.Create(new SafeCursorHandle(hCursor));
                    }
                }

                _logger.LogWarning("Failed to create eraser cursor, returning default");
                return WpfCursors.Cross;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating eraser cursor, using default");
                return WpfCursors.Cross;
            }
        }
    }

    /// <summary>
    /// Creates a cursor for the Rectangle tool with color indicator
    /// </summary>
    /// <param name="colorHex">Hex color for the rectangle (e.g., "#FF0000")</param>
    /// <returns>Custom cursor</returns>
    public WpfCursor CreateRectangleCursor(string colorHex)
    {
        lock (_cursorLock)
        {
            if (_disposed)
            {
                _logger.LogWarning("CreateRectangleCursor called on disposed CursorHelper");
                return WpfCursors.Cross;
            }

            try
            {
                _logger.LogDebug("Creating rectangle cursor with color {Color}", colorHex);

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

                    // Parse the rectangle color
                    Color rectColor = ColorTranslator.FromHtml(colorHex);

                    // Draw a small rectangle preview with corner markers
                    int rectSize = 12;
                    int rectLeft = 6;
                    int rectTop = (size - rectSize) / 2;

                    // Draw rectangle outline with the active color
                    using (Pen rectPen = new Pen(rectColor, 2))
                    {
                        g.DrawRectangle(rectPen, rectLeft, rectTop, rectSize, rectSize);
                    }

                    // Draw corner markers (small filled squares at corners)
                    int cornerSize = 3;
                    using (SolidBrush cornerBrush = new SolidBrush(Color.White))
                    {
                        // Top-left corner (this is the hotspot)
                        g.FillRectangle(cornerBrush, rectLeft - 1, rectTop - 1, cornerSize, cornerSize);
                        // Top-right corner
                        g.FillRectangle(cornerBrush, rectLeft + rectSize - 1, rectTop - 1, cornerSize, cornerSize);
                        // Bottom-left corner
                        g.FillRectangle(cornerBrush, rectLeft - 1, rectTop + rectSize - 1, cornerSize, cornerSize);
                        // Bottom-right corner
                        g.FillRectangle(cornerBrush, rectLeft + rectSize - 1, rectTop + rectSize - 1, cornerSize, cornerSize);
                    }

                    // Draw black outlines for corner markers
                    using (Pen cornerOutline = new Pen(Color.Black, 1))
                    {
                        g.DrawRectangle(cornerOutline, rectLeft - 1, rectTop - 1, cornerSize, cornerSize);
                        g.DrawRectangle(cornerOutline, rectLeft + rectSize - 1, rectTop - 1, cornerSize, cornerSize);
                        g.DrawRectangle(cornerOutline, rectLeft - 1, rectTop + rectSize - 1, cornerSize, cornerSize);
                        g.DrawRectangle(cornerOutline, rectLeft + rectSize - 1, rectTop + rectSize - 1, cornerSize, cornerSize);
                    }

                    // Convert bitmap to cursor with hotspot at top-left corner marker
                    nint hCursor = CreateCursorFromBitmap(bitmap, rectLeft, rectTop);

                    if (hCursor != nint.Zero)
                    {
                        _currentCursorHandle = hCursor;
                        _logger.LogDebug("Successfully created rectangle cursor (handle: {Handle})", hCursor);

                        return System.Windows.Interop.CursorInteropHelper.Create(new SafeCursorHandle(hCursor));
                    }
                }

                _logger.LogWarning("Failed to create rectangle cursor, returning default");
                return WpfCursors.Cross;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rectangle cursor, using default");
                return WpfCursors.Cross;
            }
        }
    }

    /// <summary>
    /// Creates a crosshair cursor with color indicator for the Line tool
    /// </summary>
    /// <param name="colorHex">Hex color for the line (e.g., "#FF0000")</param>
    /// <returns>Custom cursor</returns>
    public WpfCursor CreateLineCursor(string colorHex)
    {
        lock (_cursorLock)
        {
            if (_disposed)
            {
                _logger.LogWarning("CreateLineCursor called on disposed CursorHelper");
                return WpfCursors.Cross;
            }

            try
            {
                _logger.LogDebug("Creating line cursor with color {Color}", colorHex);

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

                    // Parse the line color
                    Color lineColor = ColorTranslator.FromHtml(colorHex);

                    // Draw two circles with a line connecting them
                    // Left circle is at the hotspot (where the mouse clicks)
                    int circleRadius = 4;
                    int circleSpacing = 20; // Increased spacing for better visibility
                    
                    // Position left circle at the hotspot (6 pixels from left edge for visual balance)
                    Point leftCircleCenter = new Point(6, size / 2);
                    Point rightCircleCenter = new Point(6 + circleSpacing, size / 2);

                    // Draw connecting line with the active color
                    using (Pen linePen = new Pen(lineColor, 2))
                    {
                        g.DrawLine(linePen, leftCircleCenter, rightCircleCenter);
                    }

                    // Draw left circle (outline) - this is where the line starts
                    using (Pen circlePen = new Pen(Color.White, 2))
                    {
                        g.DrawEllipse(circlePen, 
                            leftCircleCenter.X - circleRadius, 
                            leftCircleCenter.Y - circleRadius, 
                            circleRadius * 2, 
                            circleRadius * 2);
                    }
                    using (Pen circleOutline = new Pen(Color.Black, 1))
                    {
                        g.DrawEllipse(circleOutline, 
                            leftCircleCenter.X - circleRadius - 1, 
                            leftCircleCenter.Y - circleRadius - 1, 
                            circleRadius * 2 + 2, 
                            circleRadius * 2 + 2);
                    }

                    // Draw right circle (outline)
                    using (Pen circlePen = new Pen(Color.White, 2))
                    {
                        g.DrawEllipse(circlePen, 
                            rightCircleCenter.X - circleRadius, 
                            rightCircleCenter.Y - circleRadius, 
                            circleRadius * 2, 
                            circleRadius * 2);
                    }
                    using (Pen circleOutline = new Pen(Color.Black, 1))
                    {
                        g.DrawEllipse(circleOutline, 
                            rightCircleCenter.X - circleRadius - 1, 
                            rightCircleCenter.Y - circleRadius - 1, 
                            circleRadius * 2 + 2, 
                            circleRadius * 2 + 2);
                    }

                    // Convert bitmap to cursor with hotspot at the left circle center
                    nint hCursor = CreateCursorFromBitmap(bitmap, leftCircleCenter.X, leftCircleCenter.Y);

                    if (hCursor != nint.Zero)
                    {
                        _currentCursorHandle = hCursor;
                        _logger.LogDebug("Successfully created line cursor (handle: {Handle})", hCursor);

                        return System.Windows.Interop.CursorInteropHelper.Create(new SafeCursorHandle(hCursor));
                    }
                }

                _logger.LogWarning("Failed to create line cursor, returning default");
                return WpfCursors.Cross;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating line cursor, using default");
                return WpfCursors.Cross;
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
