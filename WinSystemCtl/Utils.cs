using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.System.Com;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;
using WinSystemCtl.Core.Data;
using System.IO;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using WinSystemCtl.Data;
using Windows.Devices.Geolocation;
using System.Diagnostics;

namespace WinSystemCtl
{
    internal static class Utils
    {
        public static string? PickSingleFile(in Window win, List<COMDLG_FILTERSPEC> filters, string? defaultFolderPath = null)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(win);
            defaultFolderPath ??= Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            int hr = PInvoke.CoCreateInstance<IFileOpenDialog>(
                typeof(FileOpenDialog).GUID,
                null,
                CLSCTX.CLSCTX_INPROC_SERVER,
                out var picker
               );
            if (hr < 0) { Marshal.ThrowExceptionForHR(hr); }

            picker.SetFileTypes(filters.ToArray());

            hr = PInvoke.SHCreateItemFromParsingName(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                null,
                typeof(IShellItem).GUID,
                out var directryShellItem
                );
            if (hr < 0) { Marshal.ThrowExceptionForHR(hr); }

            picker.SetDefaultFolder((IShellItem)directryShellItem);

            IShellItem? ppsi = null;
            try
            {
                picker.Show(new Windows.Win32.Foundation.HWND(hwnd));
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode != -2147023673) { throw; }
                else { return null; }
            }

            try
            {
                picker.GetResult(out ppsi);
                ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var fileName);

                return fileName.ToString();
            }
            catch (ArgumentException)
            {
                MessageBox(
                    App.GetString("lang_Error"),
                    App.GetString("lang_InvalidPickFileName"));
                return null;
            }
        }

        public static string? PickSingleFile(in Window win, string extDescription, string extName, string? defaultFolderPath = null)
        {
            List<COMDLG_FILTERSPEC> filters = new();
            unsafe
            {
                filters.Add(new COMDLG_FILTERSPEC
                {
                    pszName = (char*)Marshal.StringToHGlobalUni(extDescription),
                    pszSpec = (char*)Marshal.StringToHGlobalUni(extName)
                });
            }
            return PickSingleFile(win, filters, defaultFolderPath);
        }

        public static string? PickSingleFolder(in Window win, string? defaultFolderPath = null)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(win);
            defaultFolderPath ??= Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            int hr = PInvoke.CoCreateInstance<IFileOpenDialog>(
                typeof(FileOpenDialog).GUID,
                null,
                CLSCTX.CLSCTX_INPROC_SERVER,
                out var picker
               );
            if (hr < 0) { Marshal.ThrowExceptionForHR(hr); }

            picker.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

            hr = PInvoke.SHCreateItemFromParsingName(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                null,
                typeof(IShellItem).GUID,
                out var directryShellItem
                );
            if (hr < 0) { Marshal.ThrowExceptionForHR(hr); }

            picker.SetDefaultFolder((IShellItem)directryShellItem);

            IShellItem? ppsi = null;
            try
            {
                picker.Show(new Windows.Win32.Foundation.HWND(hwnd));
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode != -2147023673) { throw; }
                else { return null; }
            }

            try
            {
                picker.GetResult(out ppsi);
                ppsi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var fileName);
                return fileName.ToString();
            }
            catch (ArgumentException)
            {
                MessageBox(
                    App.GetString("lang_Error"),
                    App.GetString("lang_InvalidPickFileName"));
                return null;
            }
        }

        public static async void ShowContentDialog(XamlRoot root, string title, object content, Action<ContentDialogResult> callback)
        {
            var res = await new ContentDialog()
            {
                XamlRoot = root,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                PrimaryButtonText = App.GetString("lang_OK"),
                CloseButtonText = App.GetString("lang_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                Content = content
            }.ShowAsync();
            callback?.Invoke(res);
        }

        public static async void MessageBox(string title, string message, Action<ContentDialogResult> callback = null)
        {
            ShowContentDialog(MainWindow.Instance.Content.XamlRoot, title, message, callback);
        }
        public static async void MessageBox(XamlRoot root, string title, string message, Action<ContentDialogResult> callback = null)
        {
            ShowContentDialog(root, title, message, callback);
        }

        public static async void InputBox(XamlRoot root, string title, string placeholder, bool acceptsReturn = true, Action<ContentDialogResult, string> callback = null)
        {
            var tb = new TextBox()
            {
                PlaceholderText = placeholder,
                Style = Application.Current.Resources["DefaultTextBoxStyle"] as Style,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                AcceptsReturn = acceptsReturn
            };
            ShowContentDialog(root, title, tb, (res) =>
            {
                if (res == ContentDialogResult.Primary)
                {
                    callback?.Invoke(res, tb.Text);
                }
            });
        }

        public static async void InputBox(string title, string placeholder, bool acceptsReturn = true, Action<ContentDialogResult, string> callback = null)
        {
            InputBox(MainWindow.Instance.Content.XamlRoot, title, placeholder, acceptsReturn, callback);
        }

        public static async void RadioButtonBox(XamlRoot root, string title, string header, string[] options, Action<ContentDialogResult, string> callback = null)
        {
            if (options == null || options.Length < 1) { return; }
            var radioButtonPanel = new RadioButtons() { Header = header };
            foreach (var opt in options)
            {
                radioButtonPanel.Items.Add(new RadioButton()
                {
                    Content = opt,
                });
            }
            ((RadioButton)radioButtonPanel.Items[0]).IsChecked = true;
            ShowContentDialog(root, title, radioButtonPanel, (res) =>
            {
                foreach (var radioButton in radioButtonPanel.Items)
                {
                    if (radioButton is RadioButton rb && rb.IsChecked == true)
                    {
                        callback?.Invoke(res, rb.Content.ToString());
                        break;
                    }
                }
                callback?.Invoke(res, null);
            });
        }

        public static async void RadioButtonBox(string title, string header, string[] options, Action<ContentDialogResult, string> callback = null)
        {
            RadioButtonBox(MainWindow.Instance.Content.XamlRoot, title, header, options, callback);
        }

        public static async void CheckButtonBox(XamlRoot root, string title, string header, string[] options, Action<ContentDialogResult, string?[]> callback = null)
        {
            if (options == null || options.Length < 1) { return; }
            var rootCheckbox = new CheckBox()
            {
                Content = header,
                IsThreeState = true
            };
            var checkBoxes = options.Select(x => new CheckBox()
            {
                Content = x,
                IsThreeState = false,
                Margin = new Thickness(24, 0, 0, 0)
            }).ToList();

            rootCheckbox.Checked += (_, _) => checkBoxes.ForEach(x => x.IsChecked = true);
            rootCheckbox.Unchecked += (_, _) => checkBoxes.ForEach(x => x.IsChecked = false);

            Action updateCheckState = delegate
            {
                if (checkBoxes.TrueForAll(x => x.IsChecked == true))
                {
                    rootCheckbox.IsChecked = true;
                }
                else if (checkBoxes.TrueForAll(x => x.IsChecked == false))
                {
                    rootCheckbox.IsChecked = false;
                }
                else
                {
                    rootCheckbox.IsChecked = null;
                }
            };
            checkBoxes.ForEach(x =>
            {
                x.Checked += (_, _) => updateCheckState();
                x.Unchecked += (_, _) => updateCheckState();
            });

            var panel = new StackPanel() { Spacing = 8 };
            panel.Children.Add(rootCheckbox);
            checkBoxes.ForEach(x => panel.Children.Add(x));

            ShowContentDialog(root, title, panel, res =>
            {
                callback?.Invoke(res, checkBoxes.Where(x => x.IsChecked == true).Select(x => x.Content.ToString()).ToArray());
            });
        }

        public static async void CheckButtonBox(string title, string header, string[] options, Action<ContentDialogResult, string?[]> callback = null)
        {
            CheckButtonBox(MainWindow.Instance.Content.XamlRoot, title, header, options, callback);
        }

        public static T LoadTasksFromFile<T, U>(string path) where T : class, IEnumerable<U> where U : class
        {
            if (!File.Exists(path)) { return null; }
            using TextReader reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
            JsonSerializer serializer = new JsonSerializer();
            serializer.DefaultValueHandling = DefaultValueHandling.Populate;

            return serializer.Deserialize(reader, typeof(T)) as T;
        }

        public static void SaveTasksToFile<T>(string path, IEnumerable<T> collection)
        {
            using var writer = new StreamWriter(path, false);
            writer.Write(JsonConvert.SerializeObject(collection, Formatting.Indented));
        }

        public static void UpdateAllPublicProperties<T>(T src, T dst)
        {
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (property.CanWrite && property.CanRead)
                {
                    var value = property.GetValue(dst);
                    property.SetValue(src, value);
                }
            }
        }

        public static T GetDefaultInstance<T>() where T : class
        {
            T obj = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (property.CanWrite && property.CanRead)
                {
                    var defaultValueAttribute = property.GetCustomAttributes(typeof(DefaultValueAttribute), true).FirstOrDefault() as DefaultValueAttribute;
                    if (defaultValueAttribute != null)
                    {
                        var defaultValue = defaultValueAttribute.Value;
                        property.SetValue(obj, defaultValue);
                    }
                }
            }

            return obj;
        }

        // autoCloseDuration = 0: use default
        // autoCloseDuration < 0: never close
        public static void SendToast(string title, string message, InfoBarSeverity severity, bool isClosable = true, int autoCloseDuration = 0)
        {
            if (autoCloseDuration == 0) { autoCloseDuration = Settings.Instance.ToastAutoCloseTime; }

            var toast = new ToastInfoItem()
            {
                Title = title,
                Message = message,
                Severity = severity,
                IsClosable = isClosable,
                MainWindowViewModel = MainWindow.Instance.ViewModel,
            };
            if (autoCloseDuration > 0)
            {
                toast.DeleteAfter(autoCloseDuration);
            }
            MainWindow.Instance.ViewModel.Toasts.Add(toast);
            Debug.Print($"Toast, sent, current toast count: {MainWindow.Instance.ViewModel.Toasts.Count}");
        }

        public static void ListItemMove<T>(IList<T> list, int oldIdx, int newIdx)
        {
            if (oldIdx == newIdx) { return; }
            int idx = Math.Max(oldIdx, newIdx);
            int swapIdx = Math.Min(oldIdx, newIdx);
            (list[idx], list[swapIdx]) = (list[swapIdx], list[idx]);
        }

        public static TaskGroup GetDefaultTaskGroup()
        {
            return new TaskGroup()
            {
                Name = App.GetString("lang_DefaulNewTaskGroupName"),
                Tasks = new ObservableCollection<TaskInfo>()
            };
        }
    }
}
