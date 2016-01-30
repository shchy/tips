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
        private IEventAggregator eventAgg;

        public DataBaseContextController(
            IEventAggregator eventAgg
            , IDataBaseContext context)
        {
            this.eventAgg = eventAgg;
            this.context = context;
            this.eventAgg.GetEvent<AuthUserEvent>().Subscribe(AuthUser, true);
            this.eventAgg.GetEvent<GetUserEvent>().Subscribe(GetUser, true);
            this.eventAgg.GetEvent<AddUserEvent>().Subscribe(AddUser, true);
            this.eventAgg.GetEvent<AddProjectEvent>().Subscribe(AddProject, true);
            this.eventAgg.GetEvent<GetProjectEvent>().Subscribe(GetProject, true);
            this.eventAgg.GetEvent<UpdateProjectEvent>().Subscribe(UpdateProject, true);
            
        }

        private void AuthUser(AuthOrder order)
        {
            order.Callback(this.context.AuthUser(order.AuthUser));
        }

        private void UpdateProject(IProject model)
        {
            this.context.AddProject(model);
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
