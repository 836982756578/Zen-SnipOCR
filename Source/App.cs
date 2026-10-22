// Zen Snip & OCR - Ekran Alıntısı, Resimden Yazı Kopyalama & Renk Damlalığı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace ZenSnip
{
    public class App : System.Windows.Application
    {
        private static Mutex _mutex;
        private NotifyIcon _trayIcon;
        private HwndSource _msgWindow;
        private HudNotification _hud;

        private const int HK_SNIP = 7001;
        private const int HK_COLOR = 7002;

        [STAThread]
        public static void Main()
        {
            bool createdNew;
            _mutex = new Mutex(true, "ZenSnip_SingleInstance_Mutex_SerkanSenturk_2026", out createdNew);

            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    "Zen Snip & OCR zaten arka planda çalışıyor!\n\n• Ekrandan yazı kopyalamak için: Ctrl + Shift + S\n• Renk kodu seçmek için: Ctrl + Shift + C\n\nGeliştirici: Serkan ŞENTÜRK",
                    "Zen Snip & OCR",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            App app = new App();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _hud = new HudNotification();

            CreateMessageWindow();
            SetupTrayIcon();

            // Periodic memory trimming
            System.Windows.Threading.DispatcherTimer trimTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            trimTimer.Tick += (s, ev) =>
            {
                try
                {
                    NativeMethods.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
                catch { }
            };
            trimTimer.Start();
        }

        private void CreateMessageWindow()
        {
            HwndSourceParameters p = new HwndSourceParameters("ZenSnipMessageWindow")
            {
                WindowStyle = 0,
                Width = 0,
                Height = 0
            };
            _msgWindow = new HwndSource(p);
            _msgWindow.AddHook(WndProc);

            IntPtr handle = _msgWindow.Handle;

            // Ctrl + Shift + S (VK_S = 0x53)
            NativeMethods.RegisterHotKey(handle, HK_SNIP, NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT, 0x53);

            // Ctrl + Shift + C (VK_C = 0x43)
            NativeMethods.RegisterHotKey(handle, HK_COLOR, NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT, 0x43);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HK_SNIP)
                {
                    StartSnip(false);
                    handled = true;
                }
                else if (id == HK_COLOR)
                {
                    StartSnip(true);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void StartSnip(bool isColorPicker)
        {
            SnipOverlay overlay = new SnipOverlay(isColorPicker, (msg, accent) =>
            {
                _hud.ShowHud(msg, accent);
            });
            overlay.Show();
            overlay.Activate();
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "Zen Snip & OCR | Serkan ŞENTÜRK";
            _trayIcon.Icon = LoadAppIcon();
            _trayIcon.Visible = true;

            _trayIcon.DoubleClick += (s, e) => StartSnip(false);

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem titleItem = new ToolStripMenuItem("☯️ Zen Snip & OCR");
            titleItem.Font = new System.Drawing.Font(titleItem.Font, System.Drawing.FontStyle.Bold);
            titleItem.Enabled = false;
            menu.Items.Add(titleItem);

            ToolStripMenuItem authorItem = new ToolStripMenuItem("👤 Geliştirici: Serkan ŞENTÜRK");
            authorItem.Font = new System.Drawing.Font(authorItem.Font.FontFamily, 8.5f, System.Drawing.FontStyle.Italic);
            authorItem.Enabled = false;
            menu.Items.Add(authorItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem snipItem = new ToolStripMenuItem("✂️ Ekran Alıntısı & Yazı Kopyala (Ctrl+Shift+S)");
            snipItem.Click += (s, e) => StartSnip(false);
            menu.Items.Add(snipItem);

            ToolStripMenuItem colorItem = new ToolStripMenuItem("🎨 Renk Damlalığı / Picker (Ctrl+Shift+C)");
            colorItem.Click += (s, e) => StartSnip(true);
            menu.Items.Add(colorItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem devProfileItem = new ToolStripMenuItem("🌐 Geliştirici Sayfası (GitHub)");
            devProfileItem.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
            };
            menu.Items.Add(devProfileItem);

            ToolStripMenuItem projectRepoItem = new ToolStripMenuItem("⭐ Zen Snip GitHub");
            projectRepoItem.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578/Zen-SnipOCR") { UseShellExecute = true }); } catch { }
            };
            menu.Items.Add(projectRepoItem);

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("ℹ️ Zen Snip Hakkında");
            aboutItem.Click += (s, e) =>
            {
                System.Windows.MessageBox.Show(
                    "Zen Snip & OCR v1.0.0\n\n" +
                    "🌟 Geliştirici: Serkan ŞENTÜRK\n" +
                    "Telif Hakkı: © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.\n\n" +
                    "Ekrandan kopyalanamayan resim, video ve PDF yazılarının üzerine\n" +
                    "dikdörtgen çizerek metin olarak kopyalamanızı (OCR) ve piksel renk\n" +
                    "kodlarını (HEX/RGB) tek tıkla almanızı sağlar.\n\n" +
                    "• 100% Yerel ve Çevrimdışı Türkçe OCR Desteği\n" +
                    "• Ctrl + Shift + S: Ekrandan Yazı ve Resim Kopyala\n" +
                    "• Ctrl + Shift + C: Renk Damlalığı (HEX/RGB)\n" +
                    "• Ultra Düşük Bellek (~2 MB RAM) ve Taşınabilir\n\n" +
                    "GitHub: https://github.com/836982756578/Zen-SnipOCR",
                    "Zen Snip Hakkında",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };
            menu.Items.Add(aboutItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("🚪 Çıkış");
            exitItem.Click += (s, e) => ExitApplication();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;
        }

        private System.Drawing.Icon LoadAppIcon()
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                {
                    return new System.Drawing.Icon(icoPath, SystemInformation.SmallIconSize);
                }
                return System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void ExitApplication()
        {
            if (_msgWindow != null)
            {
                NativeMethods.UnregisterHotKey(_msgWindow.Handle, HK_SNIP);
                NativeMethods.UnregisterHotKey(_msgWindow.Handle, HK_COLOR);
                _msgWindow.Dispose();
                _msgWindow = null;
            }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }

            Shutdown();
        }
    }
}
