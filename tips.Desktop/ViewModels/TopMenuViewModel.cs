using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tips.Desktop.Modules;

namespace tips.Desktop.ViewModels
{
    public class TopMenuViewModel : BindableBase
    {
        public DelegateCommand ToHomeCommand { get; private set; } 
        public TopMenuViewModel(IEventAggregator eventAgg)
        {
            this.ToHomeCommand = new DelegateCommand(() => eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.PROJECTS));
        }
    }
}
