using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT;

namespace WinSystemCtl.XAMLExtensions
{
    public static class ScrollViewerExtensions
    {
        public static readonly DependencyProperty AutoScrollToEndProperty = DependencyProperty.RegisterAttached("AutoScrollToEnd", typeof(bool), typeof(ScrollViewerExtensions),
                new PropertyMetadata(false, onAutoScrollToEndChanged));

        public static bool GetAutoScrollToEnd(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToEndProperty);
        }

        public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToEndProperty, value);
        }

        private static bool isScrollAtBottom(ScrollViewer scrollViewer, double deltaHeight)
        {
            double verticalOffset = scrollViewer.VerticalOffset;
            double viewportHeight = scrollViewer.ViewportHeight;
            double extentHeight = scrollViewer.ExtentHeight;
            double delta = Math.Abs(verticalOffset + viewportHeight + deltaHeight - extentHeight);
            Debug.Print($"Delta: {delta:f2}");
            return delta <= 10.0;
        }

        private static void onPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Panel pane && pane.Parent is ScrollViewer sv)
            {
                if (isScrollAtBottom(sv, Math.Max(0.0, e.NewSize.Height - e.PreviousSize.Height)))
                {
                    sv.ChangeView(null, sv.ScrollableHeight, null, true);
                }
            }
        }

        private static void onAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Panel pane)
            {
                if ((bool)e.NewValue)
                {
                    pane.SizeChanged += onPanelSizeChanged;
                }
                else
                {
                    if (!(bool)e.OldValue)
                    {
                        pane.SizeChanged -= onPanelSizeChanged;
                    }
                }
            }
        }
    }
}
