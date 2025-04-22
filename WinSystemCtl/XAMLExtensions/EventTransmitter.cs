using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.XAMLExtensions
{
    public class EventTransmitter : DependencyObject
    {
        public string EventName
        {
            get { return (string)GetValue(EventNameProperty); }
            set { SetValue(EventNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EventName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EventNameProperty =
            DependencyProperty.Register("EventName", typeof(string), typeof(EventTransmitter), new PropertyMetadata(default));

        public Type InvokerType
        {
            get { return (Type)GetValue(InvokerTypeProperty); }
            set { SetValue(InvokerTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InvokerType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InvokerTypeProperty =
            DependencyProperty.Register("InvokerType", typeof(Type), typeof(EventTransmitter), new PropertyMetadata(default));

        public string InvokerPath
        {
            get { return (string)GetValue(InvokerPathProperty); }
            set { SetValue(InvokerPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InvokerPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InvokerPathProperty =
            DependencyProperty.Register("InvokerPath", typeof(string), typeof(EventTransmitter), new PropertyMetadata(default));


        public string MethodName
        {
            get { return (string)GetValue(MethodNameProperty); }
            set { SetValue(MethodNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MethodName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MethodNameProperty =
            DependencyProperty.Register("MethodName", typeof(string), typeof(EventTransmitter), new PropertyMetadata(default));

        internal bool IsBinded { get; set; }
    }
}
