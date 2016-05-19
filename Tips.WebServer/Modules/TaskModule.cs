using Nancy;
using Nancy.Security;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Model.Context;
using Tips.Model.Models;

namespace Tips.WebServer.Modules
{
    public class TaskModule : NancyModule
    {
        public TaskModule(
            IDataBaseContext context)
            :base("/task/")
        {
            this.RequiresAuthentication();

            Get["/{id}"] = prms =>
            {
                var id = prms.id;

                var task =
                    context.GetTaskRecords(x => x.Id == id).FirstOrDefault();
                (task as TaskWithRecord).Assign = this.AddIconFilePath(Request.Url, task.Assign);

                var project =
                    context.GetProjectFromTask(id);

                var user =
                   context.GetUser(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();

                return View["Views/ProjectInTask"
                    , new
                    {
                        Auth = user,
                        Task = task,
                        Project = project,
                        Progress = (task.Records.Sum(x=>x.Value) / task.Value) * 100.0,
                        ProgressValue = task.Records.Sum(x => x.Value),
                    }];
            };

            Post["/{id}/assign"] = prms =>
            {
                var id = prms.id;

                var json = this.Request.Body.ToStreamString();
                var jObj = JObject.Parse(json);

                var query =
                    from task in context.GetTaskRecords(x => x.Id == id).FirstOrNothing()
                    from userid in jObj["userid"].Value<string>().ToMaybe()
                    from user in context.GetUser(u => u.Id == userid).FirstOrNothing()
                    select new { task, user };
                query.On(a =>
                {
                    context.AddTaskToUser(a.user, a.task.Id);
                });

                var status = query.Select(_ => HttpStatusCode.OK);

                return Response.AsJson(new { }, status.Return(HttpStatusCode.BadRequest));
                
            };
            

        }
    }
}
