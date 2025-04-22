using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Data;

namespace WinSystemCtl.Converters
{
    public class TaskStateToSeverityCtr : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TaskState state)
            {
                return state switch
                {
                    TaskState.None => InfoBarSeverity.Warning,
                    TaskState.Running => InfoBarSeverity.Informational,
                    TaskState.Finished => InfoBarSeverity.Success,
                    TaskState.Error => InfoBarSeverity.Error,
                    _ => InfoBarSeverity.Informational,
                };
            }
            else
            {
                return InfoBarSeverity.Informational;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
