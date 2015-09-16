using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Model.Context;
using Tips.Model.Models;

namespace Tips.Core.Controllers
{
    public class DataBaseContextController
    {
        private IDataBaseContext context;
        private IEventAggregator ea;

        public DataBaseContextController()
        {
            this.ea = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.context = ServiceLocator.Current.GetInstance<IDataBaseContext>();
            this.ea.GetEvent<GetUserEvent>().Subscribe(a => GetUser(a.Item1, a.Item2), true);
            this.ea.GetEvent<AddUserEvent>().Subscribe(AddUser, true);
        }

        private void AddUser(IUser user)
        {
            this.context.AddUser(user);
        }

        private void GetUser(Action<IEnumerable<IUser>> callback, Func<IUser, bool> predicate)
        {
            callback(this.context.GetUser(predicate));
        }
    }
}
