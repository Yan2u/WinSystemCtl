using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Windows.ApplicationModel.Resources;
using WinSystemCtl.Core.PseudoConsole;

namespace WinSystemCtl
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param _name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Load settings
            Settings.LoadFromFile();

            // PseudoConsoleCore.Initialize();
            PseudoConsoleCore.Initialize();
            PseudoConsoleCore.RegisterErrorCallback(onPseudoConsoleCoreError);
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                PseudoConsoleCore.Dispose();
            };

            // Load i18n
            var langName = Settings.Instance.Language.ToString().Replace("_", "-");
            Application.Current.Resources.MergedDictionaries[0].Source =
                new Uri($"ms-appx:///Strings/{langName}.xaml", UriKind.RelativeOrAbsolute);

            // mainwindow
            m_window = new MainWindow();
            m_window.Activate();

            Debug.Print(Environment.CurrentDirectory);
        }

        private Window? m_window;

        private static void onPseudoConsoleCoreError(int errorCode, int line, string file)
        {
            throw new Win32Exception(errorCode, $"Line: {line}, File: {file}");
        }

        public static string GetString(string key)
        {
            if (Current.Resources[key] is string value)
            {
                return value;
            }
            else
            {
                throw new ArgumentException($"Key '{key}' not found in resources.");
            }
        }
    }
}
