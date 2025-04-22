using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WinSystemCtl.Core;
using WinSystemCtl.Core.Data;
using WinSystemCtl.Data;
using WinSystemCtl.Pages;

namespace WinSystemCtl
{
    public class MainWindowViewModel : RelatedViewModel<MainWindow>
    {
        private ObservableCollection<NavigationMenuItem> _menuItems;
        public ObservableCollection<NavigationMenuItem> MenuItems
        {
            get => _menuItems;
            set => Set(ref _menuItems, value);
        }

        private ObservableCollection<NavigationMenuItem> _footerItems;
        public ObservableCollection<NavigationMenuItem> FooterItems
        {
            get => _footerItems;
            set => Set(ref _footerItems, value);
        }

        private ObservableCollection<ToastInfoItem> _toasts;
        public ObservableCollection<ToastInfoItem> Toasts
        {
            get => _toasts;
            set => Set(ref _toasts, value);
        }

        private NavigationMenuItem _dashboardPageItem;
        private NavigationMenuItem _settingsPageItem;

        public void NavigateBack()
        {
            var frame = _relatedView.ContentFrame;
            if (frame.CanGoBack)
            {
                frame.GoBack();
                Debug.Print("Go back");
            }
        }

        public void NavigateTo(Type pageType)
        {
            _relatedView.ContentFrame.Navigate(pageType);
        }

        public void NavigateTo(Type pageType, object parameter)
        {
            _relatedView.ContentFrame.Navigate(pageType, parameter);
        }

        public void NavigationItemInvokeCmd(object sender, NavigationViewItemInvokedEventArgs e)
        {
            if (!e.IsSettingsInvoked)
            {
                var item = e.InvokedItemContainer.Tag as NavigationMenuItem;
                if (item != null)
                {
                    _relatedView.ContentFrame.Navigate(item.PageType, item.NavigationParameter);
                }
            }
        }

        public void NavigationBackCmd(object sender, NavigationViewBackRequestedEventArgs e)
        {
            NavigateBack();
        }

        public void DeleteToastInfo(ToastInfoItem item)
        {
            Toasts.Remove(item);
        }

        public MainWindowViewModel(MainWindow relatedView) : base(relatedView)
        {
            _dashboardPageItem = new NavigationMenuItem() { Name = App.GetString("lang_Dashboard"), Icon = new FontIcon { Glyph = "\uF246" }, PageType = typeof(DashboardPage) };
            _settingsPageItem = new NavigationMenuItem() { Name = App.GetString("lang_Settings"), Icon = new FontIcon { Glyph = "\ue713" }, PageType = typeof(SettingsPage) };

            MenuItems = [_dashboardPageItem];

            FooterItems = [_settingsPageItem];

            _relatedView.ContentFrame.Navigate(typeof(DashboardPage));

            Toasts = new ObservableCollection<ToastInfoItem>();
        }
    }
}
