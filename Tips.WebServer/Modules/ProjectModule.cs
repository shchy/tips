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

                var project =
                    eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == id).FirstOrDefault();


                var user =
                   eventAgg.GetEvent<GetUserEvent>().Get(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();

                return View["Views/Project", new { Auth = user, Project = project }];
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
        }
    }
}
