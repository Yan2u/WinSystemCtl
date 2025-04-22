using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.SpeechRecognition;
using WinSystemCtl.Core.Data;
using WinSystemCtl.Data;

namespace WinSystemCtl.Pages
{
    public class TaskPageViewModel : RelatedViewModel<TaskPage>
    {
        private TaskInfo _task;
        public TaskInfo Task
        {
            get => _task;
            set => Set(ref _task, value);
        }

        public void MoveToTop(object sender, RoutedEventArgs e)
        {
            var list = (sender as FrameworkElement)?.Tag as ItemsView;
            var src = list.ItemsSource as ObservableCollection<SingleStep>;
            if (src == null || list == null)
            {
                return;
            }
            if (list.SelectedItems.Count != 1 || src.Count <= 1) { return; }
            var idx = src.IndexOf(list.SelectedItem as SingleStep);
            if (idx == 0) { return; }

            Utils.ListItemMove(src, idx, 0);
            list.Select(0);
        }

        public void MoveToBottom(object sender, RoutedEventArgs e)
        {
            var list = (sender as FrameworkElement)?.Tag as ItemsView;
            var src = list.ItemsSource as ObservableCollection<SingleStep>;
            if (src == null || list == null)
            {
                return;
            }
            if (list.SelectedItems.Count != 1 || src.Count <= 1) { return; }
            var idx = src.IndexOf(list.SelectedItem as SingleStep);
            if (idx == src.Count - 1) { return; }

            Utils.ListItemMove(src, idx, src.Count - 1);
            list.Select(src.Count - 1);
        }

        public void MoveUp(object sender, RoutedEventArgs e)
        {
            var list = (sender as FrameworkElement)?.Tag as ItemsView;
            var src = list.ItemsSource as ObservableCollection<SingleStep>;
            if (src == null || list == null)
            {
                return;
            }

            if (list.SelectedItems.Count != 1 || src.Count <= 1) { return; }
            var idx = src.IndexOf(list.SelectedItem as SingleStep);
            var swapIdx = Math.Max(idx - 1, 0);
            Utils.ListItemMove(src, idx, swapIdx);
            list.Select(swapIdx);
        }

        public void MoveDown(object sender, RoutedEventArgs e)
        {
            var list = (sender as FrameworkElement)?.Tag as ItemsView;
            var src = list.ItemsSource as ObservableCollection<SingleStep>;
            if (src == null || list == null)
            {
                return;
            }

            if (list.SelectedItems.Count != 1 || src.Count <= 1) { return; }
            var idx = src.IndexOf(list.SelectedItem as SingleStep);
            var swapIdx = Math.Min(idx + 1, src.Count - 1);
            if (idx == swapIdx) { return; }
            Utils.ListItemMove(src, idx, swapIdx);
            list.Select(swapIdx);
        }

        public void EditItem(object sender, RoutedEventArgs e)
        {
            var list = (sender as FrameworkElement)?.Tag as ItemsView;
            list ??= sender as ItemsView;
            if (list == null) { return; }
            if (list.SelectedItems.Count != 1) { return; }

            SingleStepPage.EditStep(list.SelectedItem as SingleStep);
        }

        public void EditTarget(object sender, RoutedEventArgs e)
        {
            SingleStepPage.EditStep(Task.Task.Target);
        }

        public void RemoveItem(object sender, RoutedEventArgs e)
        {
            var list = (sender as FrameworkElement)?.Tag as ItemsView;
            list ??= sender as ItemsView;
            if (list == null) { return; }
            var src = list.ItemsSource as ObservableCollection<SingleStep>;
            if (src == null) { return; }

            if (list.SelectedItems.Count < 1) { return; }
            while (list.SelectedItem != null)
            {
                src.Remove(list.SelectedItem as SingleStep);
            }
        }

        public void AddItem(object sender, RoutedEventArgs e)
        {
            var list = (sender as FrameworkElement)?.Tag as ItemsView;
            list ??= sender as ItemsView;
            if (list == null) { return; }
            list.ItemsSource ??= new ObservableCollection<SingleStep>();
            var src = list.ItemsSource as ObservableCollection<SingleStep>;

            SingleStepPage.CreateStep(step =>
            {
                src.Add(step);
            });
        }

        public void ClearItem(object sender, RoutedEventArgs e)
        {
            var list = (sender as FrameworkElement)?.Tag as ItemsView;
            list ??= sender as ItemsView;
            if (list == null) { return; }
            var src = list.ItemsSource as ObservableCollection<SingleStep>;
            if (src == null || src.Count < 1) { return; }

            Utils.MessageBox(MainWindow.Instance.Content.XamlRoot,
                             App.GetString("lang_Warning"),
                             App.GetString("lang_ConfirmClear"),
                             res =>
                             {
                                 if (res == ContentDialogResult.Primary)
                                 {
                                     src.Clear();
                                 }
                             });
        }

        public void StartTask(object sender, RoutedEventArgs e)
        {
            Task.Start();
        }

        public void StopTask(object sender, RoutedEventArgs e)
        {
            Task.Stop();
        }

        public void ResetTask(object sender, RoutedEventArgs e)
        {
            Task.Reset();
        }

        public void ClearLogTask(object sender, RoutedEventArgs e)
        {
            Task.Log = string.Empty;
        }

        public void SendEnterTask(object sender, RoutedEventArgs e)
        {
            Task.Send(Environment.NewLine);
        }

        public void SendCustomTextTask(object sender, RoutedEventArgs e)
        {
            var panel = new StackPanel() { Spacing = 8 };
            var tb = new TextBox()
            {
                AcceptsReturn = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            var cb = new CheckBox()
            {
                Content = App.GetString("lang_AppendNewLineToCustomText"),
                IsChecked = true,
            };
            panel.Children.Add(tb);
            panel.Children.Add(cb);
            Utils.ShowContentDialog(
                MainWindow.Instance.Content.XamlRoot,
                App.GetString("lang_SendCustomText"),
                panel,
                res =>
                {
                    if (res == ContentDialogResult.Primary)
                    {
                        Task.Send(tb.Text + (cb.IsChecked == true ? Environment.NewLine : ""));
                    }
                });
        }

        public void Navigated(NavigationEventArgs e)
        {
            if (e.Parameter is TaskInfo t)
            {
                Task = t;
            }
        }

        public string GenTitle(string? name)
        {
            return App.GetString("lang_Task") + (string.IsNullOrWhiteSpace(name) ? "" : $": {name}");
        }

        public TaskPageViewModel(TaskPage relatedView) : base(relatedView)
        {
            Core.Data.Task t = new();
            Task = new(t);
        }
    }
}
