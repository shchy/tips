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
    public class TaskItemViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;

        public TaskItemViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
        }

        public ITaskItem TaskItem { get; private set; }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var query = navigationContext.TryToGetProjectInTask(eventAgg);
            query.On(x => this.TaskItem = x);
        }
    }
}
