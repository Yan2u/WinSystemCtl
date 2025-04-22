using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? propertyChanged;
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => propertyChanged += value;
            remove => propertyChanged -= value;
        }

        public void RaisePropertyChanged(string propertyName)
        {
            propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            RaisePropertyChanged(propertyName);
        }
    }
}
