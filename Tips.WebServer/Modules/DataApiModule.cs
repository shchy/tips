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
using Tips.Model.Models.PermissionModels;

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
                var res = Response.AsJson(new { }, HttpStatusCode.OK);
                var json = this.Request.Body.ToStreamString();
                var jObj = JObject.Parse(json);
                var projectId = jObj["projectid"].Value<int>();
                var permission =
                    eventAgg.GetEvent<GetDeleteProjectPermissionEvent>().Get().Return();

                if (IsEnableUser(eventAgg, permission))
                {
                    var project =
                    eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == projectId).FirstOrNothing();

                    project.On(eventAgg.GetEvent<DeleteProjectEvent>().Publish);
                }
                else res = HttpStatusCode.Forbidden;

                return res;
            };
            
            Delete["/users/"] = _ =>
            {
                var res = Response.AsJson(new { }, HttpStatusCode.OK);
                var json = this.Request.Body.ToStreamString();
                var jObj = JObject.Parse(json);
                var userId = jObj["userid"].Value<string>();
                var permission =
                    eventAgg.GetEvent<GetDeleteUserPermissionEvent>().Get().Return();

                if (IsEnableUser(eventAgg, permission))
                {
                    var user =
                        eventAgg.GetEvent<GetUserEvent>().Get(x => x.Id.Equals(userId)).FirstOrNothing();

                    user.On(eventAgg.GetEvent<DeleteUserEvent>().Publish);
                }
                else res = HttpStatusCode.Forbidden;

                return res;
            };

            Delete["/task/record/"] = _ =>
            {
                var res = Response.AsJson(new { }, HttpStatusCode.OK);
                var model = JsonConvert.DeserializeObject<DeleteTaskRecord>(this.Request.Body.ToStreamString());
                var permission =
                    eventAgg.GetEvent<GetDeleteTaskRecordPermissionEvent>().Get(Tuple.Create(model.TaskId, model.RecordId)).Return();

                var taskWithRecord =
                    eventAgg.GetEvent<GetTaskWithRecordEvent>().Get(x => x.Id.Equals(model.TaskId)).FirstOrNothing();
                
                taskWithRecord.On(task =>
                {
                    if (!IsEnableUser(eventAgg, permission))
                    {
                        // not permitted
                        res = Response.AsJson(new { }, HttpStatusCode.Forbidden);
                        return;
                    }
                    eventAgg.GetEvent<DeleteTaskRecordEvent>().Publish(task, model.RecordId);
                });

                return res;
            };
        }

        private bool IsEnableUser(IEventAggregator eventAgg, IPermission permission)
        {
            var query =
                from current in this.Context.CurrentUser.ToMaybe()
                from name in current.UserName.ToMaybe()
                from user in
                    (from u in eventAgg.GetEvent<GetUserEvent>().Get(x => x.Id.Equals(name))
                     select u).FirstOrNothing()
                where permission.IsPermittedDelete(user)
                select true;

            return
                query.IsSomething;
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

        class DeleteTaskRecord
        {
            public int TaskId { get; set; }
            public int RecordId { get; set; }
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
