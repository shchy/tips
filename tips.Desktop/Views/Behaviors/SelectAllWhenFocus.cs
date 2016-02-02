using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tips.Desktop.Views.Behaviors
{
    public class SelectAllWhenFocus
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
            DependencyProperty.RegisterAttached("IsEnable", typeof(bool), typeof(SelectAllWhenFocus), new PropertyMetadata(false, OnChanged));

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as TextBox;
            if (element == null)
            {
                return;
            }

            var isEnable = GetIsEnable(d);
            if (isEnable)
            {
                element.GotKeyboardFocus += SelectAll;
                element.GotFocus += SelectAll;
            }
            else
            {
                element.GotKeyboardFocus -= SelectAll;
                element.GotFocus -= SelectAll;
            }
        }

        private static void SelectAll(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }
    }
}
