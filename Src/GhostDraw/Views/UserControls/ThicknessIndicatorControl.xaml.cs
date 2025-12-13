using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GhostDraw.Views.UserControls;

public partial class ThicknessIndicatorControl : WpfUserControl
{
    private readonly DispatcherTimer _timer;

    public ThicknessIndicatorControl()
    {
        InitializeComponent();
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1.5);
        _timer.Tick += Timer_Tick;
    }

    public TimeSpan DisplayDuration
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public TimeSpan FadeOutDuration { get; set; } = TimeSpan.FromMilliseconds(300);

    public void Show(double thickness)
    {
        ThicknessIndicatorText.Text = $"{thickness:0} px";
        _timer.Stop();
        Root.BeginAnimation(OpacityProperty, null);
        Root.Visibility = Visibility.Visible;
        Root.IsHitTestVisible = false;
        Root.Opacity = 1.0;
        _timer.Start();
    }

    public void HideImmediate()
    {
        _timer.Stop();
        Root.BeginAnimation(OpacityProperty, null);
        Root.Visibility = Visibility.Collapsed;
        Root.Opacity = 0;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timer.Stop();

        var fadeOutAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(FadeOutDuration),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        fadeOutAnimation.Completed += (_, _) =>
        {
            Root.Visibility = Visibility.Collapsed;
            Root.Opacity = 0;
        };

        Root.BeginAnimation(OpacityProperty, fadeOutAnimation);
    }
}
