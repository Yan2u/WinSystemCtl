using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core;

namespace WinSystemCtl
{
    public class RelatedViewModel<T> : ViewModelBase
        where T : class
    {
        protected T _relatedView;

        public RelatedViewModel(T relatedView)
        {
            _relatedView = relatedView;
        }
    }
}
