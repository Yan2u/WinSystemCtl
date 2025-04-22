using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml.Resources;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Core;
using System.Linq;
using System.Diagnostics;
using Microsoft.UI.Xaml.Data;
using Windows.UI.ViewManagement;
using Windows.Foundation;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.IO;
using WinSystemCtl.Core.Execution;
namespace WinSystemCtl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Microsoft.UI.Windowing.AppWindow _appWindow;
        private MainWindowViewModel _viewModel;

        public const int ICON_SMALL = 0;
        public const int ICON_BIG = 1;
        public const int ICON_SMALL2 = 2;

        public const int WM_GETICON = 0x007F;
        public const int WM_SETICON = 0x0080;

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

        public Frame ContentFrame => frame_Content;
        public MainWindowViewModel ViewModel => _viewModel;

        public event TypedEventHandler<AppWindow, AppWindowClosingEventArgs> Closing
        {
            add => _appWindow.Closing += value;
            remove => _appWindow.Closing -= value;
        }


        private static MainWindow _instance;
        public static MainWindow Instance => _instance;

        private void onWindowClosing(object sender, AppWindowClosingEventArgs e)
        {
            // Save the window position and size
            Settings.Instance.LastWindowPosX = _appWindow.Position.X;
            Settings.Instance.LastWindowPosY = _appWindow.Position.Y;
            Settings.Instance.LastWindowWidth = _appWindow.Size.Width;
            Settings.Instance.LastWindowHeight = _appWindow.Size.Height;

            // Clear .envlogs
            var envLogFolder = Path.Join(Environment.CurrentDirectory, Executor.EnvLogFileFolder);
            if (Directory.Exists(envLogFolder))
            {
                foreach (var logFile in Directory.EnumerateFiles(envLogFolder, "*"))
                {
                    File.Delete(logFile);
                }
            }

            Settings.Save();
        }

        public MainWindow()
        {
            InitializeComponent();
            _instance = this;

            // set titlebar
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

            // set icon
            string sExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            System.Drawing.Icon ico = System.Drawing.Icon.ExtractAssociatedIcon(sExe);

            SendMessage(hwnd, WM_SETICON, ICON_BIG, ico.Handle);

            _viewModel = new MainWindowViewModel(this);

            // Resize and move
            _appWindow.MoveAndResize(new Windows.Graphics.RectInt32()
            {
                X = Settings.Instance.LastWindowPosX,
                Y = Settings.Instance.LastWindowPosY,
                Width = Settings.Instance.LastWindowWidth,
                Height = Settings.Instance.LastWindowHeight
            });

            // Onexit
            _appWindow.Closing += onWindowClosing;

            SetTitleBar(border_TitleBar);

        }
    }
}