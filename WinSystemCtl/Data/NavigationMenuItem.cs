using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core;

namespace WinSystemCtl.Data
{
    public class NavigationMenuItem : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        private FontIcon _icon;
        public FontIcon Icon
        {
            get => _icon;
            set => Set(ref _icon, value);
        }

        private Type _pageType;
        public Type PageType
        {
            get => _pageType;
            set => Set(ref _pageType, value);
        }

        private object? _navgationParameter;
        public object? NavigationParameter
        {
            get => _navgationParameter;
            set => Set(ref _navgationParameter, value);
        }

        private ObservableCollection<NavigationMenuItem>? _children;
        public ObservableCollection<NavigationMenuItem>? Children
        {
            get => _children;
            set => Set(ref _children, value);
        }

        public object? Tag { get; set; } = null;

        private INotifyPropertyChanged? _nameDataSource;
        private string _nameDataSourcePropertyName;

        private void onNameDataSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _nameDataSourcePropertyName && sender != null)
            {
                var newValue = sender.GetType().GetProperty(_nameDataSourcePropertyName)?.GetValue(sender);
                if (newValue != null)
                {
                    Name = newValue as string ?? string.Empty;
                }
            }
        }

        public void BindName(INotifyPropertyChanged ic, string propertyName)
        {
            _nameDataSource = ic;
            _nameDataSourcePropertyName = propertyName;
            ic.PropertyChanged += onNameDataSourcePropertyChanged;
        }

        public void UnbindName()
        {
            if (_nameDataSource != null)
            {
                _nameDataSource.PropertyChanged -= onNameDataSourcePropertyChanged;
            }
            _nameDataSource = null;
        }

        ~NavigationMenuItem()
        {
            if (_nameDataSource != null)
            {
                _nameDataSource.PropertyChanged -= onNameDataSourcePropertyChanged;
            }
        }
    }
}
