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
using Tips.Model.Context;
using Tips.Model.Models;
using Tips.Model.Models.PermissionModels;

namespace Tips.WebServer.Modules
{
    // memo 特定のプロジェクトを開いている時はModelにProjectをいれるとMenuBarのprojectManageボタンが使える
    public class ProjectModule : NancyModule
    {
        public ProjectModule(
            IDataBaseContext context
            , ITaskToTextFactory taskToText)
            : base("/project/")
        {
            this.RequiresAuthentication();

            Get["/create"] = prms =>
            {
                return View["Views/CreateProject"];
            };


            Get["/{id}"] = prms =>
            {
                var id = prms.id;
                
                var project =
                    context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrDefault();
                
                var permission = context.GetAccessProjectPermission(project.Id);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                var withRecord = this.AddIconFilePath(Request.Url,project);

                var user =
                   context.GetUser(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();
                
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
                    context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrDefault();

                var permission = context.GetAccessProjectPermission(project.Id);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                var sprintText = taskToText.Make(project.Sprints);

                var user =
                   context.GetUser(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();

                return View["Views/ProjectEdit"
                    , new
                    {
                        Auth = user
                        , Project = project
                        , SprintText = sprintText
                    }];
            };

            // todo APIに移動すべき
            Post["/{id}/save"] = prms =>
            {
                var id = prms.id;

                var user =
                    from c in this.Context.CurrentUser.ToMaybe()
                    from name in c.UserName.ToMaybe()
                    from u in context.GetUser(x => x.Id == name).FirstOrNothing()
                    select u;

                var project =
                    context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrDefault();
                
                var permission = context.GetAccessProjectPermission(project.Id);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                // todo 競合や削除の警告

                var json = this.Request.Body.ToStreamString();
                var jObj = JObject.Parse(json);
                var sprints = taskToText.Make(jObj["edittext"].Value<string>());
                
                project.Sprints = sprints;
                context.AddProject(project, user.Return());

                //var user =
                //   eventAgg.GetEvent<GetUserEvent>().Get(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();
                return
                    Response.AsJson(json, HttpStatusCode.OK);
            };

            Get["/{id}/report"] = prms =>
            {
                var id = (int)prms.id;

                var permission = context.GetAccessProjectPermission(id);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                var view =
                    from project in context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrNothing()
                    select View["Views/ProjectReport", new { Project = project }] as object;

                return view.Return(() => Response.AsRedirect("/project/" + id));
            };

            Get["/{id}/works"] = prms =>
            {
                var id = (int)prms.id;

                var permission = context.GetAccessProjectPermission(id);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                var view =
                    from project in context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrNothing()
                    select View["Views/ProjectWorks", new { Project = project }] as object;

                return view.Return(() => Response.AsRedirect("/project/" + id));
            };
            
            Get["/{id}/kanban"] = prms =>
            {
                // projectid
                var id = (int)prms.id;

                var permission = context.GetAccessProjectPermission(id);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                var view =
                    from project in context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrNothing()
                    let withrecord = this.AddIconFilePath(Request.Url, project)
                let tasks =
                        from sprint in withrecord.Sprints
                        from task in sprint.Tasks
                        select task as ITaskWithRecord
                    let backlogTasks =
                        from task in tasks
                        where task.StatusCode == 0
                        select task
                    let readyTasks =
                        from task in tasks
                        where task.StatusCode == 1
                        select task
                    let inProgressTasks =
                        from task in tasks
                        where task.StatusCode == 2
                        select task
                    let doneTasks =
                        from task in tasks
                        where task.StatusCode == 3
                        select task
                    select View["Views/Kanban", new { Project = withrecord, BacklogTasks = backlogTasks.ToArray(), ReadyTasks = readyTasks.ToArray(), InProgressTasks = inProgressTasks.ToArray(), DoneTasks = doneTasks.ToArray()}] as object;

                return view.Return(() => Response.AsRedirect("/project/" + id));
            };
            
            Get["/{id}/member"] = prms =>
            {
                var id = (int)prms.id;

                var permission = context.GetAccessProjectPermission(id);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                var view =
                    from project in context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrNothing()
                    select View["Views/ProjectMember", new { Project = project }] as object;

                return view.Return(() => Response.AsRedirect("/project/" + id));
            };
        }
        
        private bool IsEnableUser(IDataBaseContext context, IPermission permission)
        {
            var query =
                from current in this.Context.CurrentUser.ToMaybe()
                from name in current.UserName.ToMaybe()
                from user in
                    (from u in context.GetUser(x => x.Id.Equals(name))
                     select u).FirstOrNothing()
                where permission.IsPermittedUser(user)
                select true;

            return
                query.IsSomething;
        }
    }
}
