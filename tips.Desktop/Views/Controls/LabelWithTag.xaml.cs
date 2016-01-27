using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace tips.Desktop.Views.Controls
{
    /// <summary>
    /// LabelWithTag.xaml の相互作用ロジック
    /// </summary>
    public partial class LabelWithTag : UserControl
    {
        public LabelWithTag()
        {
            InitializeComponent();
        }

        static readonly Thickness LabelDefaultPadding = GetDefaultPadding();

        private static Thickness GetDefaultPadding()
        {
            var canvas = new Canvas { Width = 100, Height = 100, Margin = new System.Windows.Thickness(0) };
            var label = new Label();
            canvas.Children.Add(label);
            return label.Padding;
        }

        

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(LabelWithTag), new PropertyMetadata(Orientation.Vertical, OnChangedVertical));



        public object TagContent
        {
            get { return (object)GetValue(TagContentProperty); }
            set { SetValue(TagContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TagContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TagContentProperty =
            DependencyProperty.Register("TagContent", typeof(object), typeof(LabelWithTag), new PropertyMetadata(null, OnChangedTagContent));




        public object TextContent
        {
            get { return (object)GetValue(TextContentProperty); }
            set { SetValue(TextContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextContentProperty =
            DependencyProperty.Register("TextContent", typeof(object), typeof(LabelWithTag), new PropertyMetadata(null,OnChangedTextContent));



        private static void OnChangedTextContent(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LabelWithTag).text.Content = e.NewValue;
        }

        private static void OnChangedTagContent(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LabelWithTag).tag.Content = e.NewValue;
        }

        private static void OnChangedVertical(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = (d as LabelWithTag);
            if (@this.Orientation == Orientation.Vertical)
            {
                @this.tag.Padding = new Thickness(LabelDefaultPadding.Left, LabelDefaultPadding.Top, LabelDefaultPadding.Right, 0);
                @this.text.Padding = new Thickness(LabelDefaultPadding.Left, 0, LabelDefaultPadding.Right, LabelDefaultPadding.Bottom);
            }
            else
            {
                @this.tag.Padding = LabelDefaultPadding;
                @this.text.Padding = LabelDefaultPadding;
            }

            @this.stack.Orientation = @this.Orientation;
        }
    }
}
