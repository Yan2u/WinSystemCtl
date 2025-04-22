using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WinSystemCtl.XAMLExtensions
{
    public class EventCallInDataTemplateExtension
    {
        public static ObservableCollection<EventTransmitter> GetEventTransmitters(DependencyObject obj)
        {
            var collection = (ObservableCollection<EventTransmitter>)obj.GetValue(EventTransmittersProperty);
            if (collection == null)
            {   
                collection = new ObservableCollection<EventTransmitter>();
                if (obj is FrameworkElement element)
                {
                    element.Loaded += (_, _) =>
                    {
                        foreach (var transmitter in collection)
                        {
                            if (transmitter.IsBinded) { continue; }
                            transmitElementEvent(element, transmitter);
                        }
                    };
                }
            }
            return collection;
        }

        public static void SetEventTransmitters(DependencyObject obj, ObservableCollection<EventTransmitter> value)
        {
            obj.SetValue(EventTransmittersProperty, value);
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EventTransmittersProperty =
            DependencyProperty.RegisterAttached("EventTransmitters", typeof(ObservableCollection<EventTransmitter>), typeof(EventCallInDataTemplateExtension), new PropertyMetadata(null));



        private static object findTargetDataContext(DependencyObject parent, Type targetType, string path)
        {
            if (parent == null) { return null; }

            var field = parent.GetType().GetField(path, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType == targetType)
            {
                return field.GetValue(parent);
            }

            int n = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < n; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var res = findTargetDataContext(child, targetType, path);
                if (res != null) { return res; }
            }
            return null;
        }

        private static bool isMethodApplicable(MethodInfo method1, MethodInfo method2)
        {
            if (method1.ReturnType != method2.ReturnType) { return false; }
            var params1 = method1.GetParameters();
            var params2 = method2.GetParameters();

            if (params1.Length != params2.Length) { return false; }
            for (int i = 0; i < params1.Length; i++)
            {
                if (params1[i].ParameterType != params2[i].ParameterType) { return false; }
            }
            return true;
        }

        private static void transmitElementEvent(FrameworkElement element, EventTransmitter transmitter)
        {
            var eventInfo = element.GetType().GetEvent(transmitter.EventName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var invokerPath = transmitter.InvokerPath;
            var invokerMethodName = transmitter.MethodName;
            if (eventInfo == null) { return; }
            var targetType = transmitter.InvokerType;
            var dataContext = findTargetDataContext(MainWindow.Instance.ContentFrame, targetType, invokerPath);
            if (dataContext == null) { return; }
            var funcType = eventInfo.EventHandlerType?.GetMethod("Invoke");
            if (funcType == null) { return; }
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            foreach (var method in methods)
            {
                if (method.Name == invokerMethodName && isMethodApplicable(method, funcType))
                {
                    var dg = Delegate.CreateDelegate(eventInfo.EventHandlerType, dataContext, method);
                    eventInfo.AddEventHandler(element, dg);
                    transmitter.IsBinded = true;
                }
            }
        }
    }
}
