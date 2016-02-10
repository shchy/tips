using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Tips.Desktop.Views.Behaviors
{
    public class FirstInputFocus
    {
        public static bool GetIsEnable(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnableProperty);
        }

        public static void SetIsEnable(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnableProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsEnable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEnableProperty =
            DependencyProperty.RegisterAttached("IsEnable", typeof(bool), typeof(FirstInputFocus), new PropertyMetadata(false, OnChange));

        private static void OnChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null)
            {
                return;
            }

            var isEnable = GetIsEnable(d);
            if (isEnable && element.IsLoaded)
            {
                TrySetFocus(element);
            }
            else if (isEnable)
            {
                var handler = null as RoutedEventHandler;
                handler = (s, ev) =>
                {
                    TrySetFocus(s as FrameworkElement);
                    element.Loaded -= handler;
                };
                element.Loaded += handler;
            }
        }
        

        private static void TrySetFocus(FrameworkElement element)
        {
            var query =
               from c in element.FindVisualChildren<TextBox>()
               select c;
            var input = query.FirstOrDefault();
            if (input == null)
            {
                return;
            }

            input.Focus();
            Keyboard.Focus(input);
        }
    }

    public static class DependencyObjectExtension
    {
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
