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

namespace Tips.Desktop.Views.Controls
{
    /// <summary>
    /// UserIcon.xaml の相互作用ロジック
    /// </summary>
    public partial class UserIcon : Image
    {
        public UserIcon()
        {
            InitializeComponent();

            DependencyPropertyDescriptor.FromProperty(SourceProperty, typeof(UserIcon))
                .AddValueChanged(this, UserIcon_SourceUpdated);
            DependencyPropertyDescriptor.FromProperty(ActualHeightProperty, typeof(UserIcon))
                .AddValueChanged(this, UserIcon_SourceUpdated);
            DependencyPropertyDescriptor.FromProperty(ActualWidthProperty, typeof(UserIcon))
                .AddValueChanged(this, UserIcon_SourceUpdated);
        }

        private void UserIcon_SourceUpdated(object sender, EventArgs e)
        {
            var size =
                Math.Min(this.ActualHeight, this.ActualWidth);
            var radius = size / 2.0;
            var centerX = size / 2.0;
            var centerY = size / 2.0;

            var clipGeometry =
                new EllipseGeometry(
                    new Point(centerX, centerY)
                    , radius, radius);
            this.Clip = clipGeometry;
        }
        
    }
}
