using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using Tips.Desktop.Modules;
using Tips.Model.Models;
using System.Globalization;
using System.Windows.Media;

namespace Tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class ProjectInBacklogViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;

        public DelegateCommand ToEditBacklogCommand { get; private set; }
        public DelegateCommand<ISprint> ToSprintGraphCommand { get; private set; } 
        public DelegateCommand<ITaskItem> SelectTaskCommand { get; private set; } 
        public IProject Project { get; private set; }

        public ProjectInBacklogViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.ToEditBacklogCommand = 
                new DelegateCommand(()=> 
                    eventAgg.GetEvent<NavigateInProjectViewEvent>()
                        .Publish(ViewNames.PROJECT_IN_BACKLOG_EDIT, new NavigateParams { {"ProjectId", this.Project.Id } }));
            this.SelectTaskCommand =
                new DelegateCommand<ITaskItem>(SelectTask);
            this.ToSprintGraphCommand =
                new DelegateCommand<ISprint>(ToSprintGraph);
        }

        private void ToSprintGraph(ISprint model)
        {
            eventAgg.GetEvent<NavigateInProjectViewEvent>()
                        .Publish(ViewNames.PROJECT_IN_SPRINT_REPORT
                            , new NavigateParams
                            {
                                { "ProjectId", this.Project.Id },
                                { "SprintIds", new [] { model.Id } }
                            });

        }

        private void SelectTask(ITaskItem model)
        {
            eventAgg.GetEvent<NavigateInProjectViewEvent>()
                        .Publish(ViewNames.PROJECT_IN_TASKITEM
                            , new NavigateParams
                            {
                                { "ProjectId", this.Project.Id },
                                { "TaskItemId", model.Id }
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
            query.On(p => this.Project = p);
        }
    }

    public class TaskStatusColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var taskWithRecord = value as ITaskWithRecord;
            if (taskWithRecord == null || taskWithRecord.Value.HasValue==false)
            {
                return Brushes.Transparent;
            }

            // todo ステータスをenumで定義する
            // todo ステータスに変換する部分は専用クラスを用意する
            var totalValue = taskWithRecord.Value;

            var currentValue =
                taskWithRecord.Records.Sum(x => x.Value);
            var isComplete = totalValue <= currentValue;
            if (isComplete)
            {
                return Brushes.LightGreen;
            }
            else
            {
                return Brushes.Transparent;
            }



        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
