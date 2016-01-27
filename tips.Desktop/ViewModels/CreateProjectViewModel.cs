using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tips.Desktop.Modules;
using Tips.Core.Events;
using Prism.Regions;

namespace tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class CreateProjectViewModel : BindableBase, Prism.Regions.INavigationAware
    {
        private IEventAggregator eventAgg;

        public string ProjectName { get; set; }
        public string Describe { get; set; }
        public DelegateCommand CreateCommand { get; private set; }

        public CreateProjectViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.CreateCommand =
                new DelegateCommand(Create);
        }

        private void Create()
        {
            this.eventAgg.GetEvent<AddProjectEvent>().Publish(
                new AddProjectOrder
                {
                    Name = this.ProjectName,
                    Describe = this.Describe,
                });

            this.eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.PROJECTS);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            this.ProjectName = null;
            this.Describe = null;   
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
            //throw new NotImplementedException();
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //throw new NotImplementedException();
        }
    }
}
