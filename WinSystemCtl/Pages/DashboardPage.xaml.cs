using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinSystemCtl.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DashboardPage : PageBase
    {
        private DashboardPageViewModel _viewModel;

        public ItemsView AllTasksView => list_AllTasks;
        public Segmented SegmentedTabControl => seg_TabControl;

        public DashboardPage()
        {
            this.InitializeComponent();

            _viewModel = new DashboardPageViewModel(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _viewModel.Navigated(e);
            base.OnNavigatedTo(e);
        }
    }
}
