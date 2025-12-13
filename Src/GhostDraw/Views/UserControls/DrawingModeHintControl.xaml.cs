using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GhostDraw.Views.UserControls
{
    public partial class DrawingModeHintControl : WpfUserControl
    {
        private readonly DoubleAnimation _fadeIn;
        private readonly DoubleAnimation _fadeOut;
        private readonly DispatcherTimer _timer;

        public DrawingModeHintControl()
        {
            InitializeComponent();

            _fadeIn = new DoubleAnimation(0, 0.8, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                FillBehavior = FillBehavior.Stop
            };

            _fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(500)))
            {
                FillBehavior = FillBehavior.Stop
            };

            _fadeOut.Completed += (_, _) =>
            {
                Root.Visibility = Visibility.Collapsed;
                Root.Opacity = 0;
            };

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _timer.Tick += Timer_Tick;
        }

        public TimeSpan DisplayDuration
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public TimeSpan FadeOutDuration { get; set; } = TimeSpan.FromMilliseconds(500);

        public void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            HintText.Text = message;
            Root.Visibility = Visibility.Visible;
            Root.IsHitTestVisible = false;
            Root.Opacity = 1;
            _timer.Stop();
            Root.BeginAnimation(OpacityProperty, _fadeIn);
            _timer.Start();
        }

        public void HideImmediate()
        {
            _timer.Stop();
            Root.BeginAnimation(OpacityProperty, null);
            Root.Visibility = Visibility.Collapsed;
            Root.Opacity = 0;
            Root.IsHitTestVisible = false;
        }

        public void Hide()
        {
            _timer.Stop();
            _fadeOut.Duration = new Duration(FadeOutDuration);
            Root.BeginAnimation(OpacityProperty, _fadeOut);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            Hide();
        }
    }
}
