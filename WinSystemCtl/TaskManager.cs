using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core;
using WinSystemCtl.Data;

namespace WinSystemCtl
{
    public class TaskManager : ViewModelBase
    {
        private static readonly string FILENAME = "tasks.json";

        private static TaskManager _instance;

        public static TaskManager Instance => _instance;

        private ObservableCollection<TaskGroup>? _taskGroups;
        public ObservableCollection<TaskGroup>? TaskGroups
        {
            get => _taskGroups;
            set => Set(ref _taskGroups, value);
        }

        public void Save()
        {
            Utils.SaveTasksToFile(FILENAME, TaskGroups);
        }

        static TaskManager()
        {
            _instance = new TaskManager();
            _instance.TaskGroups = Utils.LoadTasksFromFile<ObservableCollection<TaskGroup>, TaskGroup>(FILENAME);
            if (_instance.TaskGroups == null)
            {
                _instance.TaskGroups = new ObservableCollection<TaskGroup>();
            }
            if (_instance.TaskGroups.Count == 0)
            {
                _instance.TaskGroups.Add(Utils.GetDefaultTaskGroup());
            }
        }
    }
}
