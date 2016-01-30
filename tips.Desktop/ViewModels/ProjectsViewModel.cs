using Microsoft.Practices.ServiceLocation;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tips.Desktop.Modules;
using Tips.Core.Events;
using Tips.Model.Models;

namespace tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class ProjectsViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;
        private IServiceLocator locator;

        public ProjectsViewModel(IEventAggregator eventAgg, IServiceLocator locator)
        {
            this.locator = locator;
            this.eventAgg = eventAgg;
            this.CreateProjectCommand =
                new DelegateCommand(() =>
                    eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.CREATE_PROJECT));

        }

        public DelegateCommand CreateProjectCommand { get; private set; }
        public IEnumerable<ProjectCardViewModel> Projects { get; private set; }


        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var debug = new User
            {
                Id = "admin",
                Password = "admin"
            };
            this.eventAgg.GetEvent<AuthUserEvent>().Get(debug);
            this.eventAgg.GetEvent<SetAuthUserEvent>().Publish(debug);


            var projects =
                from p in eventAgg.GetEvent<GetProjectEvent>().Get(_ => true)
                let vm = this.locator.GetInstance<ProjectCardViewModel>()
                let _ = vm.Project = p
                select vm;
            this.Projects = projects.ToArray();
        }
    }
}
