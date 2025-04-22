using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Converters
{
    public class EnumEqualToVisibilityCtr : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var valueType = value?.GetType();
            if (valueType != null && parameter is string name && valueType.IsEnum
                && value.ToString() == name)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
