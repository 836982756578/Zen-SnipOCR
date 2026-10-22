// Zen Snip & OCR - Ekran Alıntısı, Resimden Yazı Kopyalama & Renk Damlalığı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ZenSnip
{
    public class SnipOverlay : Window
    {
        private readonly bool _isColorPickerMode;
        private readonly Action<string, System.Windows.Media.Color> _onCompleted;

        private System.Windows.Point _startPoint;
        private bool _isDragging = false;
        private System.Windows.Shapes.Rectangle _selectionRect;
        private Border _magnifierBubble;
        private TextBlock _magnifierText;
        private Border _colorSwatch;
        private Canvas _canvas;

        public SnipOverlay(bool isColorPickerMode, Action<string, System.Windows.Media.Color> onCompleted)
        {
            _isColorPickerMode = isColorPickerMode;
            _onCompleted = onCompleted;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 0, 0, 0));
            Topmost = true;
            ShowInTaskbar = false;
            Cursor = Cursors.Cross;

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            BuildUI();

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close();
                }
            };
        }

        private void BuildUI()
        {
            _canvas = new Canvas();

            // Selection rectangle
            _selectionRect = new System.Windows.Shapes.Rectangle
            {
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 230, 190)),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection(new double[] { 4, 2 }),
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0, 230, 190)),
                Visibility = Visibility.Collapsed
            };
            _canvas.Children.Add(_selectionRect);

            // Magnifier / Color picker bubble
            _magnifierBubble = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(240, 20, 24, 34)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 5, 8, 5),
                Visibility = Visibility.Visible
            };

            StackPanel bubbleStack = new StackPanel { Orientation = Orientation.Horizontal };
            _colorSwatch = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(3),
                BorderBrush = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _magnifierText = new TextBlock
            {
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            bubbleStack.Children.Add(_colorSwatch);
            bubbleStack.Children.Add(_magnifierText);
            _magnifierBubble.Child = bubbleStack;

            _canvas.Children.Add(_magnifierBubble);

            Content = _canvas;

            MouseMove += OnWindowMouseMove;
            MouseLeftButtonDown += OnWindowMouseDown;
            MouseLeftButtonUp += OnWindowMouseUp;
        }

        private void OnWindowMouseMove(object sender, MouseEventArgs e)
        {
            System.Windows.Point cur = e.GetPosition(_canvas);

            // Update magnifier position
            Canvas.SetLeft(_magnifierBubble, Math.Min(Width - 140, cur.X + 15));
            Canvas.SetTop(_magnifierBubble, Math.Min(Height - 40, cur.Y + 15));

            // Sample pixel color
            NativeMethods.POINT pt;
            NativeMethods.GetCursorPos(out pt);
            System.Drawing.Color c = NativeMethods.GetPixelColor(pt.X, pt.Y);
            string hex = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);

            _colorSwatch.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B));

            if (_isColorPickerMode)
            {
                _magnifierText.Text = string.Format("{0} • RGB({1},{2},{3})", hex, c.R, c.G, c.B);
            }
            else if (_isDragging)
            {
                double x = Math.Min(cur.X, _startPoint.X);
                double y = Math.Min(cur.Y, _startPoint.Y);
                double w = Math.Abs(cur.X - _startPoint.X);
                double h = Math.Abs(cur.Y - _startPoint.Y);

                Canvas.SetLeft(_selectionRect, x);
                Canvas.SetTop(_selectionRect, y);
                _selectionRect.Width = w;
                _selectionRect.Height = h;

                _magnifierText.Text = string.Format("{0:0} x {1:0} px", w, h);
            }
            else
            {
                _magnifierText.Text = "Alanı seçmek için sürükleyin";
            }
        }

        private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (_isColorPickerMode)
                {
                    // Copy color
                    NativeMethods.POINT pt;
                    NativeMethods.GetCursorPos(out pt);
                    System.Drawing.Color c = NativeMethods.GetPixelColor(pt.X, pt.Y);
                    string hex = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);

                    try { System.Windows.Clipboard.SetText(hex); } catch { }

                    Close();
                    _onCompleted("🎨 Renk Kopyalandı: " + hex, System.Windows.Media.Color.FromRgb(c.R, c.G, c.B));
                    return;
                }

                _startPoint = e.GetPosition(_canvas);
                _isDragging = true;
                _selectionRect.Width = 0;
                _selectionRect.Height = 0;
                _selectionRect.Visibility = Visibility.Visible;
            }
        }

        private void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging || _isColorPickerMode) return;
            _isDragging = false;

            System.Windows.Point endPoint = e.GetPosition(_canvas);
            int x = (int)Math.Min(endPoint.X, _startPoint.X) + (int)Left;
            int y = (int)Math.Min(endPoint.Y, _startPoint.Y) + (int)Top;
            int w = (int)Math.Abs(endPoint.X - _startPoint.X);
            int h = (int)Math.Abs(endPoint.Y - _startPoint.Y);

            Close();

            if (w < 8 || h < 8) return;

            // Capture screen area
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (Bitmap bmp = new Bitmap(w, h))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h));
                        }

                        // Copy image to clipboard on STA thread
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                System.Windows.Clipboard.SetDataObject(bmp, true);
                            }
                            catch { }
                        }));

                        // Run OCR
                        string recognized = OcrEngineRunner.RecognizeText(bmp);

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (!string.IsNullOrWhiteSpace(recognized))
                            {
                                try
                                {
                                    System.Windows.Clipboard.SetText(recognized);
                                }
                                catch { }

                                string snippet = recognized.Replace("\r", " ").Replace("\n", " ").Trim();
                                if (snippet.Length > 36) snippet = snippet.Substring(0, 33) + "...";

                                _onCompleted("📋 Yazı Kopyalandı: \"" + snippet + "\"", System.Windows.Media.Color.FromRgb(0, 230, 190));
                            }
                            else
                            {
                                _onCompleted("📷 Ekran Görüntüsü Panoya Kopyalandı", System.Windows.Media.Color.FromRgb(90, 180, 255));
                            }
                        }));
                    }
                }
                catch { }
            });
        }
    }
}
