using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// LinkButton.xaml の相互作用ロジック
    /// </summary>
    public partial class LinkButton : UserControl
    {
        public LinkButton()
        {
            InitializeComponent();
            this.Foreground = Brushes.Transparent;

            DependencyPropertyDescriptor.FromProperty(ForegroundProperty, typeof(LinkButton))
                .AddValueChanged(this, ForegroundChanged);
        }

        private void ForegroundChanged(object sender, EventArgs e)
        {
            if (this.Foreground == Brushes.Transparent)
            {
                return;
            }
            this.link.Foreground = this.Foreground;
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(LinkButton), new PropertyMetadata(string.Empty, OnChangedText));

        

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(LinkButton), new PropertyMetadata(null, OnChangedCommand));

        private static void OnChangedCommand(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = d as LinkButton;
            if (@this == null)
            {
                return;
            }
            @this.link.Command = @this.Command;
        }

        private static void OnChangedText(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = d as LinkButton;
            if (@this == null)
            {
                return;
            }
            @this.text.Text = @this.Text;
        }
    }
}
