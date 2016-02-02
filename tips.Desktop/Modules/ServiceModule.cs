using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Prism.Events;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Controllers;
using Tips.Model.Context;
using Tips.Model.Models;

namespace Tips.Desktop.Modules
{
    public class ServiceModule : IModule
    {
        private IUnityContainer container;
        private IEventAggregator eventAgg;
        private IEnumerable<object> services;
        private IUser authUser;

        public ServiceModule(IEventAggregator eventAgg, IUnityContainer container)
        {
            this.eventAgg = eventAgg;
            this.container = container;

            this.eventAgg.GetEvent<SetAuthUserEvent>().Subscribe(SetAuthUser, true);
            this.eventAgg.GetEvent<GetAuthUserEvent>().Subscribe(GetAuthUser, true);

        }

        private void GetAuthUser(Action<IUser> callback)
        {
            callback(this.authUser);
        }

        private void SetAuthUser(IUser authUser)
        {
            this.authUser = authUser;
        }

        public void Initialize()
        {
            this.services = new[]
            {
                container.Resolve<DataBaseContextController>(),
            };
        }
    }

    public class SetAuthUserEvent : PubSubEvent<IUser> { }
    public class GetAuthUserEvent : PubSubEvent<Action<IUser>> { }
}
