using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Tips.Desktop.Views;

namespace Tips.Desktop.Modules
{
    public class NotifyUser : IModule
    {
        private IUnityContainer container;
        private IEventAggregator eventAgg;

        public NotifyUser(IEventAggregator eventAgg, IUnityContainer container)
        {
            this.eventAgg = eventAgg;
            this.container = container;
        }

        public void Initialize()
        {
            this.eventAgg.GetEvent<AskUserEvent>().Subscribe(AskUser, true);
            this.eventAgg.GetEvent<SelectFileEvent>().Subscribe(SelectFile, true);
        }

        private void SelectFile(Action<string> callback)
        {
            var dialog = new OpenFileDialog();
            var isOk = dialog.ShowDialog() ?? false;
            if (isOk == false)
            {
                callback(null);
            }
            else
            {
                callback(dialog.FileName);
            }
        }

        private void AskUser(AskUserOrder order)
        {
            var view = this.container.Resolve<AskUserView>();
            order.OkCommand = ToCloseView(order.OkCommand, view);
            order.CancelCommand = ToCloseView(order.CancelCommand, view);
            view.DataContext = order;
            view.ShowDialog();
        }

        private ICommand ToCloseView(ICommand command, AskUserView view)
        {
            if (command == null)
            {
                return null;
            }
            return
                new DelegateCommand(() =>
                {
                    command.Execute(null);
                    view.Close();
                });
        }
    }

    public class AskUserEvent : PubSubEvent<AskUserOrder> { }
    public class SelectFileEvent : PubSubEvent<Action<string>> { }

    public class AskUserOrder
    {
        public AskUserOrder()
        {
            this.IsOk = false;
            OkCommand = new DelegateCommand(() => this.IsOk = true);
        }
        public object ViewModel { get; set; }
        public ICommand OkCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public bool IsOk { get; set; }
    }


}
