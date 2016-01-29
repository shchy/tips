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
    public class TopMenuViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;

        public DelegateCommand ToHomeCommand { get; private set; }
        public DelegateCommand ToProjectHomeCommand { get; private set; }

        public IProject Project { get; private set; }

        public TopMenuViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.ToHomeCommand = new DelegateCommand(() => eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.PROJECTS));
            this.ToProjectHomeCommand = new DelegateCommand(() =>
            {
                eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.PROJECT, new NavigateParams { { "ProjectId", this.Project.Id } });
                eventAgg.GetEvent<NavigateInProjectViewEvent>().Publish(ViewNames.PROJECT_IN_BACKLOG, new NavigateParams { { "ProjectId", this.Project.Id } });
            });
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var query = navigationContext.TryToGetProject(eventAgg);
            query.On(project => this.Project = project);
            query.Or(() => this.Project = null);

        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
    }
}
