using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using tips.Desktop.Modules;
using Tips.Model.Models;

namespace tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class ProjectInBacklogViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;

        public DelegateCommand ToEditBacklogCommand { get; private set; }
        public IProject Project { get; private set; }

        public ProjectInBacklogViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.ToEditBacklogCommand = 
                new DelegateCommand(()=> 
                    eventAgg.GetEvent<NavigateInProjectViewEvent>()
                        .Publish(ViewNames.PROJECT_IN_BACKLOG_EDIT, new NavigateParams { {"ProjectId", this.Project.Id } }));
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var query = navigationContext.TryToGetProject(eventAgg);
            query.On(p => this.Project = p);
        }
    }

    
}
