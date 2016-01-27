using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using tips.Desktop.Modules;

namespace tips.Desktop.ViewModels
{
    public class ProjectViewModel : BindableBase, INavigationAware
    {
        public DelegateCommand ToBacklogCommand { get; private set; }
        public DelegateCommand ToSprintBoardCommand { get; private set; }
        public DelegateCommand ToIssuesCommand { get; private set; }
        public DelegateCommand ToBugsCommand { get; private set; }
        public DelegateCommand ToRecordCommand { get; private set; }

        public ProjectViewModel(IEventAggregator eventAgg)
        {
            this.ToBacklogCommand = new DelegateCommand(() => eventAgg.GetEvent<NavigateInProjectViewEvent>().Publish(ViewNames.PROJECT_IN_BACKLOG));
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

        }
    }
}
