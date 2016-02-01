using Nancy;
using Nancy.IO;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Model.Models;

namespace Tips.WebServer.Modules
{
    public class DataApiModule : NancyModule
    {
        public DataApiModule(IEventAggregator eventAgg) : base("/api/")
        {
            this.RequiresAuthentication();
            
            Get["/users/"] = _ =>
            {
                var users =
                    from u in eventAgg.GetEvent<GetUserEvent>().Get(u => true)
                    select new { Id = u.Id, Name = u.Name, Role = u.Role.ToString() };
                return 
                    Response.AsJson(users.ToArray());
            };

            Get["/projects/"] = _ =>
            {
                return
                    Response.AsJson(eventAgg.GetEvent<GetProjectEvent>().Get(p => true).ToArray());
            };

            Get["/tasks/"] = _ =>
            {
                return
                    Response.AsJson(eventAgg.GetEvent<GetTaskWithRecordEvent>().Get(p => true).ToArray());
            };

            Post["/users/"] = _ =>
            {
                var model = this.Bind<User>();
                eventAgg.GetEvent<AddUserEvent>().Publish(model);

                return HttpStatusCode.OK;
            };

            Post["/projects/"] = _ =>
            {
                var project = 
                    Project.FromJson(this.Request.Body.ToStreamString())
                    .ToMaybe();

                project.On(eventAgg.GetEvent<UpdateProjectEvent>().Publish);

                return project.IsSomething ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
            };

            Post["/task/comment/"] = _ =>
            {
                //var model = this.Bind<AddTaskComment>();
                var json = this.Request.Body.ToStreamString();
                var model = JsonConvert.DeserializeObject<AddTaskComment>(json);
                eventAgg.GetEvent<AddTaskCommentEvent>().Publish(model.Comment, model.TaskId);

                return HttpStatusCode.OK;
            };
            Post["/task/record/"] = _ =>
            {
                //var model = this.Bind<AddTaskRecord>();
                var model = JsonConvert.DeserializeObject<AddTaskRecord>(this.Request.Body.ToStreamString());
                eventAgg.GetEvent<AddTaskRecordEvent>().Publish(model.Record, model.TaskId);

                return HttpStatusCode.OK;
            };
        }

        class AddTaskComment
        {
            public int TaskId { get; set; }
            public TaskComment Comment { get; set; }
        }

        class AddTaskRecord
        {
            public int TaskId { get; set; }
            public TaskRecord Record { get; set; }

        }
    }

    public class LoginApiModule : NancyModule
    {
        public LoginApiModule(IEventAggregator eventAgg) : base("/api/")
        {
            Post["/login/"] = _ =>
            {
                var model = this.Bind<User>();

                var user =
                    eventAgg.GetEvent<GetUserEvent>()
                    .Get(u => u.Id == model.Id && u.Password == model.Password)
                    .FirstOrDefault();

                return
                    Response.AsJson(user, HttpStatusCode.OK);
            };
        }

    }

    public static class NancyModuleExtension
    {
        public static string ToStreamString(this RequestStream @this)
        {
            using (var r = new StreamReader(@this))
            {
                return r.ReadToEnd();
            }
        }
    }
}
