using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Data;

namespace WinSystemCtl.Converters
{
    public class TaskStateToInfoBadgeStyleCtr : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TaskState state)
            {
                return state switch
                {
                    TaskState.None => App.Current.Resources["InformationalIconInfoBadgeStyle"] as Style,
                    TaskState.Running => App.Current.Resources["AttentionIconInfoBadgeStyle"] as Style,
                    TaskState.Finished => App.Current.Resources["SuccessIconInfoBadgeStyle"] as Style,
                    TaskState.Error => App.Current.Resources["CriticalIconInfoBadgeStyle"] as Style,
                    _ => App.Current.Resources["AttentionIconInfoBadgeStyle"] as Style,
                };
            }
            else
            {
                return App.Current.Resources["InformationalIconInfoBadgeStyle"] as Style;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
