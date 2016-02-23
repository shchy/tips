using Nancy;
using Nancy.Extensions;
using Nancy.IO;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
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
                return
                    Response.AsJson(
                        eventAgg.GetEvent<GetUserEvent>().Get(p => true)
                        .Select(u => this.AddIconFilePath(this.Request.Url, u))
                        .ToArray());
            };

            Get["/projects/"] = _ =>
            {
                return
                    Response.AsJson(eventAgg.GetEvent<GetProjectEvent>().Get(p => true).ToArray());
            };

            Get["/tasks/"] = _ =>
            {
                return
                    Response.AsJson(
                        eventAgg.GetEvent<GetTaskWithRecordEvent>().Get(p => true).Select(t=>
                        {
                            (t as TaskWithRecord).Records = t.Records.Select(r =>
                                {
                                    (r as TaskRecord).Who = this.AddIconFilePath(this.Request.Url, r.Who);
                                    return r;
                                }).ToArray();
                            return t;
                        }).ToArray());
            };

            Post["/users/"] = _ =>
            {
                //var model = this.Bind<User>();
                var model = JsonConvert.DeserializeObject<User>(this.Request.Body.ToStreamString());
                eventAgg.GetEvent<AddUserEvent>().Publish(model);

                return Response.AsJson(new { }, HttpStatusCode.OK);
            }; 

            Post["/users/withIcon/"] = _ =>
            {
                try
                {

                    //var model = this.Bind<AddUserWithIcon>();
                    var model = JsonConvert.DeserializeObject<AddUserWithIcon>(this.Request.Body.ToStreamString());
                    var targetUser =
                        eventAgg.GetEvent<GetUserEvent>().Get(p => p.Id == model.UserId).FirstOrDefault();
                    if (targetUser == null)
                    {
                        return HttpStatusCode.BadRequest;
                    }

                    eventAgg.GetEvent<AddUserIconEvent>().Publish(model);

                    return Response.AsJson(new { }, HttpStatusCode.OK);
                }
                catch (Exception)
                {

                    throw;
                }

            };

            Post["/projects/"] = _ =>
            {
                var project = 
                    Project.FromJson(this.Request.Body.ToStreamString())
                    .ToMaybe();

                project.On(eventAgg.GetEvent<UpdateProjectEvent>().Publish);

                return Response.AsJson(new { }, project.IsSomething ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            };

            Post["/task/comment/"] = _ =>
            {
                //var model = this.Bind<AddTaskComment>();
                var json = this.Request.Body.ToStreamString();
                var model = JsonConvert.DeserializeObject<AddTaskComment>(json);
                eventAgg.GetEvent<AddTaskCommentEvent>().Publish(model.Comment, model.TaskId);

                return Response.AsJson(json, HttpStatusCode.OK);
            };
            Post["/task/record/"] = _ =>
            {
                var model = JsonConvert.DeserializeObject<AddTaskRecord>(this.Request.Body.ToStreamString());
                eventAgg.GetEvent<AddTaskRecordEvent>().Publish(model.Record, model.TaskId);

                return Response.AsJson(new { }, HttpStatusCode.OK);
            };


            Delete["/projects/"] = _ =>
            {
                var json = this.Request.Body.ToStreamString();
                var jObj = JObject.Parse(json);
                var projectId = jObj["projectid"].Value<int>();

                var project =
                    eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == projectId).FirstOrNothing();

                project.On(eventAgg.GetEvent<DeleteProjectEvent>().Publish);

                return Response.AsJson(new { }, HttpStatusCode.OK);
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

    public static class MyClass
    {
        // todo パスはコンストラクタで
        static string httpFolder = "img/userIcons/";
        static string localFolder = "content/img/userIcons/";
        static string defaulticon = "default";

        public static IUser AddIconFilePath(this NancyModule @this, Url url, IUser user)
        {
            if (user == null)
            {
                return user;
            }

            var iconname = user.Id;

            if (File.Exists(Path.Combine( localFolder, iconname + ".png")) == false)
            {
                iconname = defaulticon;
            }

            var iconUri =
                new Uri(
                    new Uri(url.SiteBase)
                    , Path.Combine(httpFolder, iconname + ".png"));
            (user as User).IconFile = iconUri.ToString();
            return user;
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
                    Response.AsJson(this.AddIconFilePath(Request.Url, user), HttpStatusCode.OK);
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
