using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Desktop.Modules;
using Tips.Core.Events;
using Tips.Model.Models;
using Haskellable.Code.Monads.Maybe;

namespace Tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class TopMenuViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;

        public DelegateCommand ToHomeCommand { get; private set; }
        public DelegateCommand ToUserCommand { get; private set; }
        public DelegateCommand ToProjectHomeCommand { get; private set; }

        public DelegateCommand ToManageCommand { get; private set; }
        public DelegateCommand ToManageProjectCommand { get; private set; }



        public IProject Project { get; private set; }
        public IUser MySelf { get; private set; }

        public TopMenuViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.ToHomeCommand = new DelegateCommand(() => eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.PROJECTS));
            this.ToUserCommand = new DelegateCommand(() => eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.USER, new NavigateParams { { "UserId", this.MySelf.Id } }));

            this.ToProjectHomeCommand = new DelegateCommand(() =>
            {
                eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.PROJECT, new NavigateParams { { "ProjectId", this.Project.Id } });
                eventAgg.GetEvent<NavigateInProjectViewEvent>().Publish(ViewNames.PROJECT_IN_BACKLOG, new NavigateParams { { "ProjectId", this.Project.Id } });
            });
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            this.Project = null;
            this.ToManageCommand = null;
            this.ToManageProjectCommand = null;

            var query = navigationContext.TryToGetProject(eventAgg);
            query.On(project =>
            {
                this.Project = project;
            });

            this.MySelf = this.eventAgg.GetEvent<GetAuthUserEvent>().Get().Return();
            if (this.MySelf != null && this.MySelf.Role == UserRole.Admin)
            {
                this.ToManageCommand = new DelegateCommand(() =>
                    eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.SYSTEM_MANAGE));
            }
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
