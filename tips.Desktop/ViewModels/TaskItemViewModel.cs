using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using Tips.Desktop.Modules;
using Tips.Core.Events;
using Tips.Model.Models;
using System.Globalization;

namespace Tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class TaskItemViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;

        public TaskItemViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.AddCommentCommand = new DelegateCommand(AddComment);
            this.AddRecordCommand = new DelegateCommand(AddRecord);
        }

        private void AddRecord()
        {
            //ITaskRecord;
            var vm = new AddTaskRecordViewModel();
            eventAgg.GetEvent<AskUserEvent>().Publish(
                new AskUserOrder
                {
                    ViewModel = vm,
                });

            var record = new TaskRecord
            {
                Day = DateTime.Now, // todo 
                Who = eventAgg.GetEvent<GetAuthUserEvent>().Get().Return(),
                Value = vm.Value,
                WorkValue = vm.WorkValue,
            };
            eventAgg.GetEvent<AddTaskRecordEvent>().Publish(record, this.TaskItemWithRecord.Id);

            eventAgg.GetEvent<NavigateInProjectViewEvent>().Publish(ViewNames.PROJECT_IN_TASKITEM
                , new NavigateParams { { "TaskItemId", this.TaskItemWithRecord.Id } });
        }

        private void AddComment()
        {
            var comment = new TaskComment
            {
                Day = DateTime.Now,
                Text = this.AddCommentText,
                Who = this.eventAgg.GetEvent<GetAuthUserEvent>().Get().Return(),
            };
            eventAgg.GetEvent<AddTaskCommentEvent>().Publish(comment, this.TaskItemWithRecord.Id);

            eventAgg.GetEvent<NavigateInProjectViewEvent>().Publish(ViewNames.PROJECT_IN_TASKITEM
                , new NavigateParams { { "TaskItemId", this.TaskItemWithRecord.Id } });
        }

        public DelegateCommand AddCommentCommand { get; private set; }
        public string AddCommentText { get; set; }
        public ITaskWithRecord TaskItemWithRecord { get; private set; }
        public DelegateCommand AddRecordCommand { get; private set; }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            this.AddCommentText = string.Empty;
            var query = navigationContext.TryToGetProjectInTask(eventAgg);
            query.On(x => TaskItemWithRecord = x);
        }
    }

    public class RecordValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var records = value as IEnumerable<ITaskRecord>;
            if (records == null )
            {
                return value;
            }

            return
                records.Sum(x => x.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
