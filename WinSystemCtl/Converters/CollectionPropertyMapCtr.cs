using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Reflection;

namespace WinSystemCtl.Converters
{
    public class CollectionPropertyMapCtr : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ICollection collection && parameter is string str && !string.IsNullOrWhiteSpace(str))
            {
                Collection<object> result = new();
                foreach (var item in collection)
                {
                    var prop = item.GetType().GetProperty(str);
                    if (prop == null) { return null; }
                    result.Add(prop.GetValue(item));
                }

                return result;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
