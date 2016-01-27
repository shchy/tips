using Microsoft.Practices.ServiceLocation;
using Prism.Events;
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
            this.ea.GetEvent<GetUserEvent>().Subscribe(GetUser, true);
            this.ea.GetEvent<AddUserEvent>().Subscribe(AddUser, true);
            this.ea.GetEvent<AddProjectEvent>().Subscribe(AddProject, true);
            this.ea.GetEvent<GetProjectEvent>().Subscribe(GetProject, true);
        }

        

        private void GetProject(GetOrder<IProject> order)
        {
            order.Callback(this.context.GetProjects(order.Predicate));
        }

        private void AddProject(AddProjectOrder order)
        {
            var model = new Project
            {
                Name = order.Name,
                Describe = order.Describe,
            };
            this.context.AddProject(model);
        }

        private void AddUser(IUser user)
        {
            this.context.AddUser(user);
        }


        private void GetUser(GetOrder<IUser> order)
        {
            order.Callback(this.context.GetUser(order.Predicate));
        }
    }
}
