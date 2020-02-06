using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MaterialKeyboardIndicator
{
    public static class BitmapExtension
    {
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
    }

    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;

        public const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        public const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        //Modifiers:
        private const uint MOD_NONE = 0x0000; //(none)
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
        //CAPS LOCK:
        private const uint VK_CAPITAL = 0x14;
        private const uint VK_NUMLOCK = 0x90;

        public MainWindow()
        {
            InitializeComponent();
            
            SetStartup();
        }

        private void SetStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.SetValue("MaterialKeyboardIndicator", Process.GetCurrentProcess().MainModule.FileName);
            Debug.WriteLine(Process.GetCurrentProcess().MainModule.FileName);
        }

        private IntPtr _windowHandle;
        private HwndSource _source;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, VK_CAPITAL); //CAPS_LOCK
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, VK_NUMLOCK); //NUM_LOCK

            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = (desktopWorkingArea.Right / 2) - (this.Width / 2);
            this.Top = desktopWorkingArea.Bottom - this.Height;
            Keyboard.ClearFocus();
        }

        public bool CanPlayAnim = true;
        public void TryPlayAnimation()
        {
            if (CanPlayAnim)
            {
                Storyboard sb = this.FindResource("Opening") as Storyboard;
                sb.Completed += this.Sb_Completed;
                sb.Begin(this);
                CanPlayAnim = false;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_CAPITAL)
                            {
                                TryPlayAnimation();
                                txtKey.Text = "CAPS LOCK";
                                txtState.Text = Keyboard.IsKeyToggled(Key.CapsLock) ? "ENABLED" : "DISABLED";
                                icon.Source = Keyboard.IsKeyToggled(Key.CapsLock) ? Properties.Resources.LockIcon.ToBitmapImage() : Properties.Resources.UnlockIcon.ToBitmapImage();
                                border.Background = Keyboard.IsKeyToggled(Key.CapsLock) ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x00, 0x87, 0x1D)) : new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xC7, 0, 0x2D));
                            }

                            if (vkey == VK_NUMLOCK)
                            {
                                TryPlayAnimation();
                                txtKey.Text = "NUM LOCK";
                                Debug.WriteLine(Keyboard.IsKeyToggled(Key.NumLock));
                                txtState.Text = Keyboard.IsKeyToggled(Key.NumLock) ? "ENABLED" : "DISABLED";
                                icon.Source = Keyboard.IsKeyToggled(Key.NumLock) ? Properties.Resources.LockIcon.ToBitmapImage() : Properties.Resources.UnlockIcon.ToBitmapImage();
                                border.Background = Keyboard.IsKeyToggled(Key.NumLock) ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x00, 0x87, 0x1D)) : new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xC7, 0, 0x2D));
                            }

                            handled = true;
                            break;
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        private void Sb_Completed(object sender, EventArgs e)
        {
            CanPlayAnim = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }
    }
}

