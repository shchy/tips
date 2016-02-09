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
using Tips.Core.Services;
using Tips.Model.Models;

namespace Tips.WebServer.Modules
{
    public class ProjectModule : NancyModule
    {
        public ProjectModule(IEventAggregator eventAgg, ITaskToTextFactory taskToText)
            : base("/project/")
        {

            this.RequiresAuthentication();

            Get["/{id}"] = prms =>
            {
                var id = prms.id;


                // hack BindするためにSprintのITaskItemをITaskWithRecordに差し替える。

                var taskWithRecords = eventAgg.GetEvent<GetTaskWithRecordEvent>().Get(_ => true).ToArray();
                var getWithTask = Fn.New((ITaskItem t) =>
                {
                    var finded = taskWithRecords.FirstOrDefault(x => x.Id == t.Id);
                    if (finded == null)
                    {
                        return t;
                    }
                    return finded;
                });
                var toWithRecords = Fn.New((ISprint s) =>
                {
                    var sprint = s as Sprint;
                    sprint.Tasks =
                        sprint.Tasks.Select(getWithTask).ToArray();
                    return sprint;
                });
                var toWithRecordsProject = Fn.New((IProject p) =>
                {
                    p.Sprints = p.Sprints.Select(toWithRecords).ToArray();
                    return p;
                });

                var project =
                    eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == id).FirstOrDefault();
                var withRecord = toWithRecordsProject(project);

                var user =
                   eventAgg.GetEvent<GetUserEvent>().Get(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();

                return View["Views/Project", new { Auth = user, Project = withRecord }];
            };

            Get["/{id}/board"] = prms =>
            {
                var a = "/project/" + prms.id as string;
                return
                    Response.AsRedirect(a);
            };

            Get["/{id}/edit"] = prms =>
            {
                var id = prms.id;

                var project =
                    eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == id).FirstOrDefault();

                var sprintText = taskToText.Make(project.Sprints);

                var user =
                   eventAgg.GetEvent<GetUserEvent>().Get(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();

                return View["Views/ProjectEdit"
                    , new
                    {
                        Auth = user
                        , Project = project
                        , SprintText = sprintText
                    }];
            };

            Post["/{id}/save"] = prms =>
            {
                var id = prms.id;

                var project =
                    eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == id).FirstOrDefault();
                // todo 競合や削除の警告

                var json = this.Request.Body.ToStreamString();
                var jObj = JObject.Parse(json);
                var sprints = taskToText.Make(jObj["edittext"].Value<string>());
                
                project.Sprints = sprints;
                eventAgg.GetEvent<UpdateProjectEvent>().Publish(project);

                //var user =
                //   eventAgg.GetEvent<GetUserEvent>().Get(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();
                return
                    Response.AsJson(json, HttpStatusCode.OK);
            };

            Get["/{id}/report"] = prms =>
            {
                var id = prms.id;



                return View["Views/ProjectReport"];
            };
        }
    }
}
