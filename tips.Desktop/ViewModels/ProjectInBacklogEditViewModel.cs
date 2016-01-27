using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using tips.Desktop.Modules;
using Tips.Core.Events;
using Tips.Core.Services;
using Tips.Model.Models;

namespace tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class ProjectInBacklogEditViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;
        private ITaskToTextFactory taskToText;

        public DelegateCommand SaveCommand { get; private set; }
        public IProject Project { get; private set; }
        public string EditText { get; set; }

        public ProjectInBacklogEditViewModel(IEventAggregator eventAgg, ITaskToTextFactory taskToText)
        {
            this.eventAgg = eventAgg;
            this.taskToText = taskToText;
            this.SaveCommand = 
                new DelegateCommand(Save);
        }

        private void Save()
        {
            var updateSprints = this.taskToText.Make(this.EditText);

            // todo conflictの事を考える
            var query =
                from p in eventAgg.GetEvent<GetProjectEvent>().Get(_ => true)
                where p.Id == this.Project.Id
                select p;
            var newerProject = query.FirstOrNothing();

            newerProject.On(p =>
            {
                p.Sprints = updateSprints;
                this.Project = p;
                eventAgg.GetEvent<UpdateProjectEvent>().Publish(this.Project);
                eventAgg.GetEvent<NavigateInProjectViewEvent>()
                            .Publish(ViewNames.PROJECT_IN_BACKLOG, new NavigateParams { { "ProjectId", this.Project.Id } });

            });
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
            query.On(p =>
            {
                this.Project = p;
                this.EditText = taskToText.Make(this.Project.Sprints);
            });
        }
    }
}
