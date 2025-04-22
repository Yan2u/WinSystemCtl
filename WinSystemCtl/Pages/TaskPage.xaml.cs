using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using WinSystemCtl.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinSystemCtl.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TaskPage : PageBase
    {
        private TaskPageViewModel _viewModel;

        public TaskPage()
        {
            this.InitializeComponent();

            _viewModel = new TaskPageViewModel(this);
        }

        public static void EditTask(TaskInfo taskInfo)
        {
            Debug.Print(System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
            MainWindow.Instance.ViewModel.NavigateTo(typeof(TaskPage), taskInfo);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _viewModel.Navigated(e);
            base.OnNavigatedTo(e);
        }
    }
}
