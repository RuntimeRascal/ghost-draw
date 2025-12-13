using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GhostDraw.Views.UserControls
{
    public partial class ClearCanvasConfirmationControl : WpfUserControl
    {
        private readonly DoubleAnimation _fadeIn;
        private readonly DoubleAnimation _fadeOut;

        public event EventHandler? Confirmed;
        public event EventHandler? Cancelled;

        public ClearCanvasConfirmationControl()
        {
            InitializeComponent();

            _fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(140)))
            {
                FillBehavior = FillBehavior.HoldEnd
            };

            _fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(180)))
            {
                FillBehavior = FillBehavior.Stop
            };

            _fadeOut.Completed += (_, _) =>
            {
                Root.Visibility = Visibility.Collapsed;
                Root.Opacity = 0;
            };

            ConfirmButton.Click += (_, _) => Confirmed?.Invoke(this, EventArgs.Empty);
            CancelButton.Click += (_, _) => Cancelled?.Invoke(this, EventArgs.Empty);
        }

        public void Show()
        {
            Root.Visibility = Visibility.Visible;
            Root.IsHitTestVisible = true;
            Root.Opacity = 1;
            Root.BeginAnimation(OpacityProperty, _fadeIn);
        }

        public void Hide()
        {
            Root.IsHitTestVisible = false;
            Root.BeginAnimation(OpacityProperty, _fadeOut);
        }
    }
}
