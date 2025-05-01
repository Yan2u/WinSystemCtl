using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Data;
using System.Collections.Specialized;
using Windows.Media.Capture;
using Windows.Win32;
using System.Data;
using System.Diagnostics;
using CommunityToolkit.WinUI;

namespace WinSystemCtl.Pages
{
    public class DashboardPageViewModel : RelatedViewModel<DashboardPage>
    {
        private const int GROUPS_IDX_START = 1;
        private bool _initialUpdate = false;

        public ObservableCollection<TaskGroup>? TaskGroups => TaskManager.Instance.TaskGroups;

        private TaskGroup _currentGroup;

        private ObservableCollection<TaskInfo> _tasks;
        public ObservableCollection<TaskInfo> Tasks
        {
            get => _tasks;
            set => Set(ref _tasks, value);
        }

        private void updateNavPanel(TaskGroup? changedGroup)
        {
            if (MainWindow.Instance.ViewModel == null) { return; }
            var navList = MainWindow.Instance.ViewModel.MenuItems;

            if (changedGroup == null)
            {
                // update all
                // clear
                for (int i = navList.Count - 1; i >= GROUPS_IDX_START; --i)
                {
                    navList[i].UnbindName();
                    navList[i].Tag = null;
                    if (navList[i].Children != null && navList[i].Children.Count > 0)
                    {
                        foreach (var child in navList[i].Children)
                        {
                            child.UnbindName();
                            child.Tag = null;
                        }
                    }
                    navList.RemoveAt(i);
                }

                // add
                foreach (var taskGroup in TaskGroups)
                {
                    var item = new NavigationMenuItem()
                    {
                        Name = taskGroup.Name,
                        Icon = new FontIcon() { Glyph = "\uEC26" },
                        PageType = typeof(DashboardPage),
                        NavigationParameter = taskGroup,
                        Tag = taskGroup,
                    };
                    item.BindName(taskGroup, "Name");

                    navList.Add(item);
                    if (taskGroup.Tasks != null && taskGroup.Tasks.Count > 0)
                    {
                        item.Children = new ObservableCollection<NavigationMenuItem>();
                        foreach (var taskInfo in taskGroup.Tasks)
                        {
                            var child = new NavigationMenuItem()
                            {
                                Name = taskInfo.Task.Name,
                                Icon = new FontIcon() { Glyph = "\uE7C4" },
                                PageType = typeof(TaskPage),
                                NavigationParameter = taskInfo,
                            };
                            child.BindName(taskInfo.Task, "Name");
                            item.Children.Add(child);
                        }
                    }
                }
            }
            else
            {
                // updated changed only
                for (int i = navList.Count - 1; i >= GROUPS_IDX_START; --i)
                {
                    if (navList[i].Tag == changedGroup)
                    {
                        // clear
                        navList[i].Children ??= new ObservableCollection<NavigationMenuItem>();
                        if (navList[i].Children.Count > 0)
                        {
                            foreach (var child in navList[i].Children)
                            {
                                child.UnbindName();
                                child.Tag = null;
                            }
                            navList[i].Children.Clear();
                        }

                        // add
                        foreach (var taskInfo in changedGroup.Tasks)
                        {
                            var child = new NavigationMenuItem()
                            {
                                Name = taskInfo.Task.Name,
                                Icon = new FontIcon() { Glyph = "\uE7C4" },
                                PageType = typeof(TaskPage),
                                NavigationParameter = taskInfo,
                            };
                            child.BindName(taskInfo.Task, "Name");
                            navList[i].Children.Add(child);
                        }
                    }
                }
            }
        }

        private void replaceCurrentTasks(ObservableCollection<TaskInfo> newTasks)
        {
            // must do these steps when removing items to avoid crashes
            // and to avoid updateToNavPanel() to crash
            _currentGroup.Tasks = newTasks;
            Tasks = _currentGroup.Tasks;
            updateNavPanel(_currentGroup);
        }

        public void PageLoaded(object sender, RoutedEventArgs e)
        {
            if (!_initialUpdate)
            {
                updateNavPanel(null);
                _initialUpdate = true;
                MainWindow.Instance.Closing += (_, _) =>
                {
                    TaskManager.Instance.Save();
                };
            }
        }

        public void PageUnloaded(object sender, RoutedEventArgs e)
        {
            TaskManager.Instance.Save();
        }

        // events

        public void TaskGroupSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_relatedView.SegmentedTabControl.SelectedIndex < 0) { return; }
            _currentGroup = TaskGroups[_relatedView.SegmentedTabControl.SelectedIndex];
            Tasks = _currentGroup.Tasks;
        }

        public void TaskItemAdd(object sender, RoutedEventArgs e)
        {
            Core.Data.Task t = new()
            {
                Name = App.GetString("lang_Task"),
                Description = App.GetString("lang_TaskDescription"),
                Target = Utils.GetDefaultInstance<Core.Data.SingleStep>(),
                Encoding = "utf-8"
            };
            Tasks.Add(new TaskInfo(t));
            updateNavPanel(_currentGroup);
            TaskPage.EditTask(Tasks.Last());
        }

        public void TaskGroupRename(object sender, RoutedEventArgs e)
        {
            Utils.InputBox(
                string.Format(App.GetString("lang_GroupRenameModalTitle"), _currentGroup.Name),
                _currentGroup.Name,
                false,
                (res, str) =>
                {
                    if (res == ContentDialogResult.Primary)
                    {
                        _currentGroup.Name = str;
                    }
                });
        }

        public void TaskGroupAdd(object sender, RoutedEventArgs e)
        {
            var group = new TaskGroup()
            {
                Name = App.GetString("lang_DefaulNewTaskGroupName"),
                Tasks = []
            };
            TaskManager.Instance.TaskGroups.Add(group);
            updateNavPanel(null);
        }

        public void TaskGroupRemove(object sender, RoutedEventArgs e)
        {
            Utils.MessageBox(
                App.GetString("lang_Warning"),
                string.Format(App.GetString("lang_ConfirmRemoveTaskGroup"), _currentGroup.Name),
                (res) =>
                {
                    if (res == ContentDialogResult.Primary)
                    {
                        _currentGroup.StopAllTasks();
                        TaskManager.Instance.TaskGroups.Remove(_currentGroup);
                        if (TaskManager.Instance.TaskGroups.Count == 0)
                        {
                            TaskManager.Instance.TaskGroups.Add(Utils.GetDefaultTaskGroup());
                        }
                        _relatedView.SegmentedTabControl.SelectedIndex = 0;
                        updateNavPanel(null);
                    }
                });
        }

        public void TaskGroupEdit(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ViewModel.NavigateTo(typeof(EditTaskGroupsPage));
        }

        private int getSelectedTaskInfoIndex()
        {
            if (_relatedView.AllTasksView.SelectedItem == null) { return -1; }
            return Tasks.IndexOf(_relatedView.AllTasksView.SelectedItem as TaskInfo);
        }

        private List<int>? getSelectedTaskInfoIndexes()
        {
            if (_relatedView.AllTasksView.SelectedItems.Count == 0) { return null; }
            return _relatedView.AllTasksView.SelectedItems.Select(x => Tasks.IndexOf(x as TaskInfo))
                                                          .ToList();
        }

        public void TaskItemStart(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null) { return; }
            idxes.ForEach(x => { Tasks?[x]?.Start(); });
        }

        public void TaskItemReset(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null) { return; }
            idxes.ForEach(x => Tasks?[x]?.Reset());
        }

        public void TaskItemStop(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null) { return; }
            idxes.ForEach(x => Tasks?[x]?.Stop());
        }

        public void TaskItemEdit(object sender, RoutedEventArgs e)
        {
            var idx = getSelectedTaskInfoIndex();
            if (idx == -1) { return; }
            TaskPage.EditTask(Tasks[idx]);
        }

        public void TaskItemRemove(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null) { return; }
            foreach (var idx in idxes)
            {
                if (_currentGroup.Tasks[idx].State == TaskState.Running)
                {
                    _currentGroup.Tasks[idx].Stop();
                }
            }
            replaceCurrentTasks([.. _currentGroup.Tasks.Except(idxes.Select(x => _currentGroup.Tasks[x]))]);
        }

        public void TaskItemMoveToTop(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null || idxes.Count < 1) { return; }
            idxes.Sort();
            for (int i = 0; i < idxes.Count; i++)
            {
                if (idxes[i] == i) { continue; }
                Utils.ListItemMove(Tasks, idxes[i], i);
                _relatedView.AllTasksView.Select(i);
            }
        }

        public void TaskItemMoveUp(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null || idxes.Count < 1) { return; }
            idxes.Sort();
            for (int i = 0; i < idxes.Count; i++)
            {
                var swapIdx = Math.Max(idxes[i] - 1, i);
                if (idxes[i] == swapIdx) { continue; }
                Utils.ListItemMove(Tasks, idxes[i], swapIdx);
                _relatedView.AllTasksView.Select(swapIdx);
            }
        }

        public void TaskItemMoveDown(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null || idxes.Count < 1) { return; }
            idxes.Sort((x, y) => -x.CompareTo(y));
            for (int i = 0; i < idxes.Count; i++)
            {
                var swapIdx = Math.Min(idxes[i] + 1, Tasks.Count - 1 - i);
                if (idxes[i] == swapIdx) { continue; }
                Utils.ListItemMove(Tasks, idxes[i], swapIdx);
                _relatedView.AllTasksView.Select(swapIdx);
            }
        }

        public void TaskItemMoveToBottom(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null || idxes.Count < 1) { return; }
            idxes.Sort((x, y) => -x.CompareTo(y));
            for (int i = 0; i < idxes.Count; i++)
            {
                if (idxes[i] == Tasks.Count - 1 - i) { continue; }
                Utils.ListItemMove(Tasks, idxes[i], Tasks.Count - 1 - i);
                _relatedView.AllTasksView.Select(Tasks.Count - 1 - i);
            }
        }

        public void TaskItemMoveToGroup(object sender, RoutedEventArgs e)
        {
            Utils.RadioButtonBox(
                App.GetString("lang_MoveToGroup"),
                App.GetString("lang_Groups"),
                TaskGroups.Select(x => x.Name).ToArray(),
                (res, opt) =>
                {
                    if (res == ContentDialogResult.Primary)
                    {
                        var idxes = getSelectedTaskInfoIndexes();
                        if (idxes == null || idxes.Count < 1) { return; }
                        idxes.Sort((x, y) => -x.CompareTo(y));
                        foreach (var group in TaskGroups)
                        {
                            if (group.Name == opt && group != _currentGroup)
                            {
                                for (int i = 0; i < idxes.Count; ++i)
                                {
                                    group.Tasks.Add(_currentGroup.Tasks[idxes[i]]);
                                }
                                updateNavPanel(group);
                                replaceCurrentTasks([.. _currentGroup.Tasks.Except(idxes.Select(x => _currentGroup.Tasks[x]))]);
                                break;
                            }
                        }
                    }
                });
        }

        public void TaskItemCopyToGroup(object sender, RoutedEventArgs e)
        {
            Utils.CheckButtonBox(
                App.GetString("lang_CopyToGroup"),
                App.GetString("lang_Groups"),
                TaskGroups.Select(x => x.Name).ToArray(),
                (res, opts) =>
                {
                    if (res == ContentDialogResult.Primary)
                    {
                        var idxes = getSelectedTaskInfoIndexes();
                        if (idxes == null || idxes.Count < 1) { return; }
                        foreach (var group in TaskGroups)
                        {
                            foreach (var opt in opts)
                            {
                                if (group.Name == opt)
                                {
                                    for (int i = 0; i < idxes.Count; ++i)
                                    {
                                        var newTask = new Core.Data.Task();
                                        newTask.UpdateInplace(_currentGroup.Tasks[idxes[i]].Task);
                                        group.Tasks.Add(new TaskInfo(newTask));
                                    }
                                    updateNavPanel(group);
                                    break;
                                }
                            }
                        }
                    }
                });
        }

        public void TaskItemStartAll(object sender, RoutedEventArgs e)
        {
            Tasks.AsParallel()
                 .Where(x => x.State == TaskState.None)
                 .ToList()
                 .ForEach(x => x.Start());
        }

        public void TaskItemStopAll(object sender, RoutedEventArgs e)
        {
            Tasks.AsParallel()
                 .Where(x => x.State == TaskState.Running)
                 .ToList()
                 .ForEach(x => x.Stop());
        }

        public void TaskItemResetAll(object sender, RoutedEventArgs e)
        {
            Tasks.AsParallel()
                 .ToList()
                 .ForEach(x => x.Reset());
        }

        public void TaskItemStartScheduled(object sender, RoutedEventArgs e)
        {
            Tasks.AsParallel()
                 .Where(x => x.State == TaskState.None && x.IsScheduled)
                 .ToList()
                 .ForEach(x => x.Start());
        }

        public void TaskItemStopScheduled(object sender, RoutedEventArgs e)
        {
            Tasks.AsParallel()
                 .Where(x => x.State == TaskState.Running && x.IsScheduled)
                 .ToList()
                 .ForEach(x => x.Stop());
        }

        public void TaskItemResetScheduled(object sender, RoutedEventArgs e)
        {
            Tasks.AsParallel()
                 .Where(x => x.IsScheduled)
                 .ToList()
                 .ForEach(x => x.Reset());
        }

        public void TaskItemScheduleAll(object sender, RoutedEventArgs e)
        {
            Tasks.AsParallel()
                 .ToList()
                 .ForEach(x => x.IsScheduled = true);
        }

        public void TaskItemUnScheduleAll(object sender, RoutedEventArgs e)
        {
            Tasks.AsParallel()
                 .ToList()
                 .ForEach(x => x.IsScheduled = false);
        }

        public void TaskItemReverseScheduled(object sender, RoutedEventArgs e)
        {
            Tasks.AsParallel()
                 .ToList()
                 .ForEach(x => x.IsScheduled = !x.IsScheduled);
        }

        public void TaskItemClear(object sender, RoutedEventArgs e)
        {
            Utils.MessageBox(
                App.GetString("lang_Warning"),
                App.GetString("lang_ConfirmClear"),
                (result) =>
                {
                    if (result == ContentDialogResult.Primary)
                    {
                        foreach (var task in Tasks)
                        {
                            if (task.State == TaskState.Running)
                            {
                                task.Stop();
                            }
                        }
                        Tasks.Clear();
                        updateNavPanel(_currentGroup);
                    }
                });
        }

        public void Navigated(NavigationEventArgs e)
        {
            if (e.Parameter is TaskGroup group)
            {
                var idx = TaskGroups.IndexOf(group);
                if (idx >= 0)
                {
                    _relatedView.SegmentedTabControl.SelectedIndex = idx;
                }
            }

            if (_relatedView.SegmentedTabControl.SelectedIndex < 0 && TaskGroups.Count > 0)
            {
                _relatedView.SegmentedTabControl.SelectedIndex = 0;
            }
        }

        public DashboardPageViewModel(DashboardPage relatedView) : base(relatedView)
        {
            TaskManager.Instance.TaskGroups.CollectionChanged += (_, _) => updateNavPanel(null);
        }
    }
}
