using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinSystemCtl.Converters
{
    class TaskStateToCardBackgroundCtr : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TaskState state)
            {
                SolidColorBrush? defaultBrush = App.Current.Resources["CardBackgroundFillColorDefaultBrush"] as SolidColorBrush;
                return state switch
                {
                    TaskState.None => defaultBrush,
                    TaskState.Running => new SolidColorBrush(Color.FromArgb(12, 2, 131, 134)),
                    TaskState.Finished => new SolidColorBrush(Color.FromArgb(12, 13, 119, 13)),
                    TaskState.Error => new SolidColorBrush(Color.FromArgb(12, 189, 41, 28)),
                    _ => defaultBrush
                };
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
