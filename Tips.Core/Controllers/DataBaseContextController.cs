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
            this.eventAgg.GetEvent<DeleteUserEvent>().Subscribe(DeleteUser, true);
            this.eventAgg.GetEvent<AddUserIconEvent>().Subscribe(AddUserIcon, true);
            this.eventAgg.GetEvent<AddProjectEvent>().Subscribe(AddProject, true);
            this.eventAgg.GetEvent<GetProjectEvent>().Subscribe(GetProject, true);
            this.eventAgg.GetEvent<UpdateProjectEvent>().Subscribe(UpdateProject, true);
            this.eventAgg.GetEvent<DeleteProjectEvent>().Subscribe(DeleteProject, true);
            this.eventAgg.GetEvent<GetTaskWithRecordEvent>().Subscribe(GetTaskWithRecord, true);
            this.eventAgg.GetEvent<AddTaskCommentEvent>().Subscribe(AddTaskComment, true);
            this.eventAgg.GetEvent<AddTaskRecordEvent>().Subscribe(AddTaskRecord, true);
            this.eventAgg.GetEvent<DeleteTaskRecordEvent>().Subscribe(DeleteTaskRecord, true);
            this.eventAgg.GetEvent<AddUserToTaskEvent>().Subscribe(AddUserToTask, true);
        }

        private void AddUserToTask(AddOrder<IUser, int> order)
        {
            this.context.AddTaskToUser(order.Model, order.WithIn);
        }

        private void AddUserIcon(AddUserWithIcon order)
        {
            var targetUser =
                this.context.GetUser(u => u.Id == order.UserId)
                .FirstOrNothing();

            targetUser.On(u =>
            {
                var bytes = Convert.FromBase64String(order.Base64BytesByImage);
                this.context.AddUserIcon(u, bytes);
            });
        }

        private void AddTaskRecord(AddOrder<ITaskRecord, int> order)
        {
            this.context.AddTaskRecord(order.Model, order.WithIn);
        }
        
        private void DeleteTaskRecord(DeleteOrder<ITaskWithRecord, int> order)
        {
            this.context.DeleteTaskRecord(order.Model, order.WithIn);
        }

        private void AddTaskComment(AddOrder<ITaskComment, int> order)
        {
            this.context.AddTaskComment(order.Model, order.WithIn);
        }

        private void GetTaskWithRecord(GetOrder<ITaskWithRecord> order)
        {
            order.Callback(this.context.GetTaskRecords(order.Predicate));
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

        private void DeleteProject(IProject project)
        {
            this.context.DeleteProject(project);
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
        
        private void DeleteUser(IUser user)
        {
            this.context.DeleteUser(user);
        }
    }
}
