using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core;

namespace WinSystemCtl.Data
{
    public class DependencyItem : ViewModelBase
    {
        private string _text;
        public string Text
        {
            get => _text;
            set => Set(ref _text, value);
        }

        private string _link;
        public string Link
        {
            get => _link;
            set => Set(ref _link, value);
        }
    }
}
