using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core.Data;
using WinSystemCtl.Data;

namespace WinSystemCtl.Pages
{
    public class EditTaskGroupsPageViewModel : RelatedViewModel<EditTaskGroupsPage>
    {
        public ObservableCollection<TaskGroup> TaskGroups => TaskManager.Instance.TaskGroups;

        private int getSelectedTaskInfoIndex()
        {
            if (_relatedView.TaskGroupsListView.SelectedItem == null) { return -1; }
            return TaskGroups.IndexOf(_relatedView.TaskGroupsListView.SelectedItem as TaskGroup);
        }

        private List<int>? getSelectedTaskInfoIndexes()
        {
            if (_relatedView.TaskGroupsListView.SelectedItems.Count == 0) { return null; }
            return _relatedView.TaskGroupsListView.SelectedItems.Select(x => TaskGroups.IndexOf(x as TaskGroup))
                                                          .ToList();
        }

        public void TaskGroupAdd(object sender, RoutedEventArgs e)
        {
            var group = new TaskGroup()
            {
                Name = App.GetString("lang_DefaulNewTaskGroupName"),
                Tasks = []
            };
            TaskGroups.Add(group);
        }

        public void TaskGroupRemove(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null || idxes.Count < 1) { return; }
            idxes.Sort((x, y) => -x.CompareTo(y)); // reverse order
            var allGroupNames = string.Join(", ", idxes.Select(x => TaskGroups[x].Name));

            Utils.MessageBox(
                App.GetString("lang_Warning"),
                string.Format(App.GetString("lang_ConfirmRemoveTaskGroup"), allGroupNames),
                (res) =>
                {
                    if (res == ContentDialogResult.Primary)
                    {
                        for (int i = 0; i < idxes.Count; i++)
                        {
                            TaskGroups[idxes[i]].StopAllTasks();
                            TaskGroups.RemoveAt(idxes[i]);
                        }
                        if (TaskGroups.Count == 0)
                        {
                            TaskGroups.Add(Utils.GetDefaultTaskGroup());
                        }
                    }
                });
        }

        public void TaskGroupRemoveAll(object sender, RoutedEventArgs e)
        {
            Utils.MessageBox(
                App.GetString("lang_Warning"),
                App.GetString("lang_ConfirmClear"),
                (res) =>
                {
                    if (res == ContentDialogResult.Primary)
                    {
                        foreach (var group in TaskGroups)
                        {
                            group.StopAllTasks();
                        }
                        TaskGroups.Clear();
                        TaskGroups.Add(Utils.GetDefaultTaskGroup());
                    }
                });
        }

        public void TaskGroupRename(object sender, RoutedEventArgs e)
        {
            var idx = getSelectedTaskInfoIndex();
            if (idx < 0) { return; }
            Utils.InputBox(
                string.Format(App.GetString("lang_GroupRenameModalTitle"), TaskGroups[idx].Name),
                TaskGroups[idx].Name,
                false,
                (res, str) =>
                {
                    if (res == ContentDialogResult.Primary)
                    {
                        TaskGroups[idx].Name = str;
                    }
                });
        }

        public void TaskGroupMoveToTop(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null || idxes.Count < 1) { return; }
            idxes.Sort();
            for (int i = 0; i < idxes.Count; ++i)
            {
                if (idxes[i] == i) { continue; }
                Utils.ListItemMove(TaskGroups, idxes[i], i);
                _relatedView.TaskGroupsListView.Select(i);
            }
        }

        public void TaskGroupMoveUp(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null || idxes.Count < 1) { return; }
            idxes.Sort();
            for (int i = 0; i < idxes.Count; ++i)
            {
                var swapIdx = Math.Max(idxes[i] - 1, i);
                if (idxes[i] == swapIdx) { continue; }
                Utils.ListItemMove(TaskGroups, idxes[i], swapIdx);
                _relatedView.TaskGroupsListView.Select(swapIdx);
            }
        }

        public void TaskGroupMoveDown(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null || idxes.Count < 1) { return; }
            idxes.Sort((x, y) => -x.CompareTo(y));
            for (int i = 0; i < idxes.Count; ++i)
            {
                var reverseI = TaskGroups.Count - i - 1;
                var swapIdx = Math.Min(idxes[i] + 1, reverseI);
                if (idxes[i] == swapIdx) { continue; }
                Utils.ListItemMove(TaskGroups, idxes[i], swapIdx);
                _relatedView.TaskGroupsListView.Select(swapIdx);
            }
        }

        public void TaskGroupMoveToBottom(object sender, RoutedEventArgs e)
        {
            var idxes = getSelectedTaskInfoIndexes();
            if (idxes == null || idxes.Count < 1) { return; }
            idxes.Sort((x, y) => -x.CompareTo(y));
            for (int i = 0; i < idxes.Count; ++i)
            {
                if (idxes[i] == TaskGroups.Count - 1 - i) { continue; }
                Utils.ListItemMove(TaskGroups, idxes[i], TaskGroups.Count - 1 - i);
                _relatedView.TaskGroupsListView.Select(TaskGroups.Count - 1 - i);
            }
        }


        public EditTaskGroupsPageViewModel(EditTaskGroupsPage relatedView) : base(relatedView)
        {
        }
    }
}
