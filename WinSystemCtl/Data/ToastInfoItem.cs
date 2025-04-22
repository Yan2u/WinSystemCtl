using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinSystemCtl.Core;

namespace WinSystemCtl.Data
{
    public class ToastInfoItem : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        private string _message;
        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        private InfoBarSeverity _severity;
        public InfoBarSeverity Severity
        {
            get => _severity;
            set => Set(ref _severity, value);
        }

        private bool _isClosable;
        public bool IsClosable
        {
            get => _isClosable;
            set => Set(ref _isClosable, value);
        }

        // For event
        public MainWindowViewModel MainWindowViewModel { get; set; }

        public int _isDeleted = 0;

        private void deleteSelf()
        {
            if(Interlocked.CompareExchange(ref _isDeleted, 1, 0) == 0)
            {
                MainWindowViewModel.DeleteToastInfo(this);
            }
        }

        public async void DeleteAfter(int millisec)
        {
            await Task.Delay(millisec);
            deleteSelf();
        }

        public void Delete(InfoBar sender, object e)
        {
            deleteSelf();
        }
    }
}
