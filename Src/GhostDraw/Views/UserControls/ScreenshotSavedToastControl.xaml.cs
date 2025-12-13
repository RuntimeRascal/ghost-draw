using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GhostDraw.Views.UserControls
{
    public partial class ScreenshotSavedToastControl : WpfUserControl
    {
        private readonly DoubleAnimation _fadeIn;
        private readonly DoubleAnimation _fadeOut;
        private readonly DispatcherTimer _timer;

        public ScreenshotSavedToastControl()
        {
            InitializeComponent();

            _fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(120)))
            {
                FillBehavior = FillBehavior.Stop
            };

            _fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300)))
            {
                FillBehavior = FillBehavior.Stop
            };

            _fadeOut.Completed += (_, _) =>
            {
                Root.Visibility = Visibility.Collapsed;
                Root.Opacity = 0;
            };

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _timer.Tick += Timer_Tick;
        }

        public TimeSpan DisplayDuration
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public TimeSpan FadeOutDuration
        {
            get => _fadeOut.Duration.TimeSpan;
            set => _fadeOut.Duration = new Duration(value);
        }

        public void Show(string title, string path)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            TitleText.Text = title;
            PathText.Text = path ?? string.Empty;
            PathText.Visibility = string.IsNullOrWhiteSpace(path) ? Visibility.Collapsed : Visibility.Visible;
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
        }

        public void Hide()
        {
            _timer.Stop();
            Root.BeginAnimation(OpacityProperty, _fadeOut);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            Hide();
        }
    }
}
