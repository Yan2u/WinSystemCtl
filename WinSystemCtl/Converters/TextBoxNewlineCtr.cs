using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Converters
{
    public class TextBoxNewlineCtr : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string str)
            {
                return str.Replace(Environment.NewLine, "\r");
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string str)
            {
                return str.Replace("\r", Environment.NewLine);
            }
            else
            {
                return null;
            }
        }
    }
}
