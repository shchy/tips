using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using tips.Desktop.Modules;
using Tips.Model.Models;

namespace tips.Desktop.ViewModels
{
    public class ProjectCardViewModel : BindableBase
    {
        public DelegateCommand SelectProjectCommand { get; private set; }
        public IProject Project { get; set; }

        public ProjectCardViewModel(IEventAggregator eventAgg)
        {
            this.SelectProjectCommand =
                new DelegateCommand(() =>
                {
                    eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.PROJECT, new NavigateParams { { "ProjectId", this.Project.Id } });
                    eventAgg.GetEvent<NavigateInProjectViewEvent>().Publish(ViewNames.PROJECT_IN_BACKLOG, new NavigateParams { { "ProjectId", this.Project.Id } });
                });
        }
    }
}
