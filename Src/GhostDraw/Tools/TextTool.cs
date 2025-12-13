using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GhostDraw.Core;
using Microsoft.Extensions.Logging;
using MediaBrush = System.Windows.Media.Brush;
using Point = System.Windows.Point;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfCursors = System.Windows.Input.Cursors;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace GhostDraw.Tools;

/// <summary>
/// Text drawing tool - click to start typing, click outside the text area to commit.
/// </summary>
public class TextTool(ILogger<TextTool> logger, GlobalKeyboardHook keyboardHook) : IDrawingTool
{
    private readonly ILogger<TextTool> _logger = logger;
    private readonly GlobalKeyboardHook _keyboardHook = keyboardHook;

    private WpfTextBox? _activeEditor;
    private bool _isSessionActive;
    private string _currentColor = "#FF0000";
    private double _currentThickness = 3.0;

    private const double MinFontSize = 8.0;
    private const double MaxFontSize = 72.0;
    private const double FontScale = 6.0; // BrushThickness -> FontSize scaling factor

    public event EventHandler<DrawingActionCompletedEventArgs>? ActionCompleted;

    public void OnMouseDown(Point position, Canvas canvas)
    {
        if (!_isSessionActive)
        {
            StartSession(position, canvas);
            return;
        }

        if (_activeEditor == null)
        {
            _logger.LogDebug("Text session flagged active but editor missing; resetting.");
            ResetSession(canvas, removeEditor: false);
            return;
        }

        if (IsInsideEditor(position, _activeEditor))
        {
            _activeEditor.Focus();
            double left = Canvas.GetLeft(_activeEditor);
            double top = Canvas.GetTop(_activeEditor);
            var relativePoint = new Point(position.X - left, position.Y - top);
            _activeEditor.CaretIndex = _activeEditor.GetCharacterIndexFromPoint(relativePoint, true) switch
            {
                int idx when idx >= 0 => idx,
                _ => _activeEditor.Text.Length
            };
            return;
        }

        CommitText(canvas);
    }

    public void OnMouseMove(Point position, Canvas canvas, MouseButtonState leftButtonState)
    {
        // Text tool is click-click; no drag handling needed.
    }

    public void OnMouseUp(Point position, Canvas canvas)
    {
        // No-op for click-click model.
    }

    public void OnActivated()
    {
        _logger.LogDebug("Text tool activated");
    }

    public void OnDeactivated()
    {
        _logger.LogDebug("Text tool deactivated");
        // Ensure any in-progress editor is cleaned up when switching tools.
        if (_activeEditor != null)
        {
            var parent = VisualTreeHelper.GetParent(_activeEditor) as Canvas;
            ResetSession(parent, removeEditor: true);
        }
    }

    public void OnColorChanged(string colorHex)
    {
        _currentColor = colorHex;
        if (_activeEditor != null)
        {
            _activeEditor.Foreground = CreateBrush(colorHex);
        }
        _logger.LogDebug("Text color changed to {Color}", colorHex);
    }

    public void OnThicknessChanged(double thickness)
    {
        _currentThickness = thickness;
        if (_activeEditor != null)
        {
            _activeEditor.FontSize = ComputeFontSize(thickness);
        }
        _logger.LogDebug("Text size changed; thickness={Thickness}, fontSize={Font}", thickness, ComputeFontSize(thickness));
    }

    public void Cancel(Canvas canvas)
    {
        ResetSession(canvas, removeEditor: true);
    }

    private void StartSession(Point position, Canvas canvas)
    {
        try
        {
            _activeEditor = new WpfTextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Background = WpfBrushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = CreateBrush(_currentColor),
                FontSize = ComputeFontSize(_currentThickness),
                FontFamily = new WpfFontFamily("Segoe UI"),
                MinWidth = 80,
                MaxWidth = Math.Max(300, canvas.ActualWidth * 0.6),
                Padding = new Thickness(2),
                Cursor = WpfCursors.IBeam
            };

            Canvas.SetLeft(_activeEditor, position.X);
            Canvas.SetTop(_activeEditor, position.Y);

            canvas.Children.Add(_activeEditor);

            _isSessionActive = true;
            _keyboardHook.SetTextSessionActive(true);

            _activeEditor.Focus();
            _activeEditor.CaretIndex = _activeEditor.Text.Length;

            _logger.LogInformation("Text session started at ({X:F0},{Y:F0})", position.X, position.Y);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start text session");
            ResetSession(canvas, removeEditor: true);
        }
    }

    private void CommitText(Canvas canvas)
    {
        if (_activeEditor == null)
        {
            ResetSession(canvas, removeEditor: false);
            return;
        }

        string text = _activeEditor.Text;
        double left = Canvas.GetLeft(_activeEditor);
        double top = Canvas.GetTop(_activeEditor);
        double width = Math.Max(_activeEditor.ActualWidth, _activeEditor.MinWidth);

        canvas.Children.Remove(_activeEditor);
        _activeEditor = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Text session committed with empty text; nothing added");
            ResetSession(canvas, removeEditor: false);
            return;
        }

        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = CreateBrush(_currentColor),
            Background = WpfBrushes.Transparent,
            FontSize = ComputeFontSize(_currentThickness),
            FontFamily = new WpfFontFamily("Segoe UI"),
            Width = width,
            Padding = new Thickness(2)
        };

        Canvas.SetLeft(textBlock, left);
        Canvas.SetTop(textBlock, top);
        canvas.Children.Add(textBlock);

        ActionCompleted?.Invoke(this, new DrawingActionCompletedEventArgs(textBlock));
        _logger.LogInformation("Text committed at ({X:F0},{Y:F0})", left, top);

        ResetSession(canvas, removeEditor: false);
    }

    private void ResetSession(Canvas? canvas, bool removeEditor)
    {
        try
        {
            if (removeEditor && _activeEditor != null && canvas != null)
            {
                canvas.Children.Remove(_activeEditor);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove active text editor during reset");
        }
        finally
        {
            _activeEditor = null;
            _isSessionActive = false;
            _keyboardHook.SetTextSessionActive(false);
        }
    }

    private bool IsInsideEditor(Point position, WpfTextBox editor)
    {
        double left = Canvas.GetLeft(editor);
        double top = Canvas.GetTop(editor);
        double width = editor.ActualWidth > 0 ? editor.ActualWidth : editor.MinWidth;
        double height = editor.ActualHeight > 0 ? editor.ActualHeight : editor.FontSize * 1.5;

        var bounds = new Rect(left, top, width, height);
        return bounds.Contains(position);
    }

    private double ComputeFontSize(double thickness)
    {
        double size = thickness * FontScale;
        return Math.Max(MinFontSize, Math.Min(MaxFontSize, size));
    }

    private MediaBrush CreateBrush(string colorHex)
    {
        try
        {
            return (SolidColorBrush)(new BrushConverter().ConvertFromString(colorHex) ?? WpfBrushes.White);
        }
        catch
        {
            _logger.LogWarning("Failed to parse color {Color}, using white", colorHex);
            return WpfBrushes.White;
        }
    }
}
