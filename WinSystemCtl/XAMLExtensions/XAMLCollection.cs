using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.XAMLExtensions
{
    
    public class XAMLCollection<T> : ObservableCollection<T>
    {
        
        public XAMLCollection() : base() { }
        
        public XAMLCollection(IEnumerable<T> collection) : base(collection) { }
        
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IList<T> Content => this;
    }

    public class XAMLObjectCollection : XAMLCollection<object> { }
}
