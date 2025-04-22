using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Converters
{
    public class CollectionToStringCtr : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Collection -> string
            if (value is ICollection<string> c)
            {
                string sep = Environment.NewLine;
                if (parameter != null && parameter is string sep2) sep = sep2;
                return string.Join(sep, c);
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // string -> Collection
            if (value is string s)
            {
                ICollection<string>? c = targetType.GetConstructor([]).Invoke([]) as ICollection<string>;
                if (c == null) { return null; }
                foreach (var line in s.Split('\r'))
                {
                    if (string.IsNullOrWhiteSpace(line)) { continue; }
                    c.Add(line);
                }
                return c;
            }
            else
            {
                return null;
            }
        }
    }
}
