// Zen Snip & OCR - Ekran Alıntısı, Resimden Yazı Kopyalama & Renk Damlalığı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace ZenSnip
{
    public class HudNotification : Window
    {
        private readonly TextBlock _textBlock;
        private readonly Border _pillBorder;
        private readonly DispatcherTimer _timer;

        public HudNotification()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            Focusable = false;
            SizeToContent = SizeToContent.WidthAndHeight;

            _pillBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(246, 22, 24, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 0, 220, 180)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(18, 9, 20, 9),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    ShadowDepth = 5,
                    Opacity = 0.65
                }
            };

            _textBlock = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            _pillBorder.Child = _textBlock;
            Content = _pillBorder;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2200) };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                DoubleAnimation fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(300));
                fade.Completed += (s2, e2) => Hide();
                BeginAnimation(OpacityProperty, fade);
            };
        }

        public void ShowHud(string message, Color borderAccent)
        {
            _timer.Stop();
            _textBlock.Text = message;
            _pillBorder.BorderBrush = new SolidColorBrush(borderAccent);

            UpdateLayout();
            Left = (SystemParameters.PrimaryScreenWidth - ActualWidth) / 2;
            Top = 45;

            Opacity = 1.0;
            Show();
            _timer.Start();
        }
    }
}
