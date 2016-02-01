using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Events;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using tips.Desktop.Views;

namespace tips.Desktop.Modules
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
