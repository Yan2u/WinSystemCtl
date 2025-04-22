using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core;

namespace WinSystemCtl.Data
{
    public class TaskGroup : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        private ObservableCollection<TaskInfo> _tasks;
        public ObservableCollection<TaskInfo> Tasks
        {
            get => _tasks;
            set => Set(ref _tasks, value);
        }
    }
}
