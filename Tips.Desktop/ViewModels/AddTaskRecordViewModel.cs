using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Tips.Desktop.ViewModels
{
    public class AddTaskRecordViewModel : BindableBase
    {
        public DateTime Day { get; set; }
        public double Value { get; set; }
        public double WorkValue { get; set; }
    }
}
