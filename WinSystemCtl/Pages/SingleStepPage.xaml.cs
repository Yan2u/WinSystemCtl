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
using WinSystemCtl.Core.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinSystemCtl.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SingleStepPage : PageBase
    {
        private SingleStepPageViewModel _viewModel;

        public static void CreateStep(Action<SingleStep> callback)
        {
            MainWindow.Instance.ViewModel.NavigateTo(typeof(SingleStepPage), new FormPageParam<SingleStep>(callback));
        }

        public static void EditStep(SingleStep step, Action? callback = null)
        {
            MainWindow.Instance.ViewModel.NavigateTo(typeof(SingleStepPage), new FormPageParam<SingleStep>(step, callback));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _viewModel.Navigated(e);
            base.OnNavigatedTo(e);
        }

        public SingleStepPage()
        {
            this.InitializeComponent();

            _viewModel = new SingleStepPageViewModel(this);
        }
    }
}
