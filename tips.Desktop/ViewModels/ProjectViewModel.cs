using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using Tips.Desktop.Modules;
using Tips.Model.Models;

namespace Tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class ProjectViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;

        public DelegateCommand ToBacklogCommand { get; private set; }
        public DelegateCommand ToSprintBoardCommand { get; private set; }
        public DelegateCommand ToIssuesCommand { get; private set; }
        public DelegateCommand ToBugsCommand { get; private set; }
        public DelegateCommand ToRecordCommand { get; private set; }
        public IProject Project { get; private set; }

        public ProjectViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.ToBacklogCommand = 
                new DelegateCommand(() => 
                    eventAgg.GetEvent<NavigateInProjectViewEvent>()
                        .Publish(ViewNames.PROJECT_IN_BACKLOG, new NavigateParams { { "ProjectId", this.Project.Id } }));
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
