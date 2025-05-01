using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core.Execution
{
    public class Config : ViewModelBase
    {
        private bool showAllOutput;

        /// <summary>
        /// Indicates whether to show all output from the <see cref="Data.Task"/>.
        /// <list type="bullet"> True: will invoke output callback during all stages. </list>
        /// <list type="bullet"> False: will invoke output callback only during <see cref="Stage.Target"/> stage </list>
        /// Default: false
        /// </summary>
        [DefaultValue(false)]
        public bool ShowAllOutput
        {
            get => showAllOutput;
            set => Set(ref showAllOutput, value);
        }

        private int _cacheOutputSize;

        /// <summary>
        /// Size of the output cache in bytes.
        /// </summary>
        [DefaultValue(65536)]
        public int CacheOutputSize
        {
            get => _cacheOutputSize;
            set => Set(ref _cacheOutputSize, value);
        }

    }
}
