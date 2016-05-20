using Haskellable.Code.Monads.Maybe;
using Microsoft.Practices.Unity;
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
using Tips.Core.Services;
using Tips.Model.Context;
using Tips.Model.Models;
using Tips.Model.Models.PermissionModels;

namespace Tips.WebServer.Modules
{
    public class DataApiModule : NancyModule
    {   
        public DataApiModule(
            IDataBaseContext context
            , [Dependency("workdaySettingFolder")] string workdaySettingFolder) 
            : base("/api/")
        {
            this.RequiresAuthentication();


            Get["/users/"] = _ =>
            {
                return
                    Response.AsJson(
                        context.GetUser(p => true)
                        .Select(u => this.AddIconFilePath(this.Request.Url, u))
                        .ToArray());
            };

            Get["/projects/"] = _ =>
            {
                var user =
                    from c in this.Context.CurrentUser.ToMaybe()
                    from name in c.UserName.ToMaybe()
                    from u in context.GetUser(x => x.Id == name).FirstOrNothing()
                    select u;

                if (user.IsNothing)
                    return HttpStatusCode.BadRequest;

                return
                    Response.AsJson(context.GetProjectBelongUser(user.Return())
                        .Select(p => MyClass.ToWithRecordsProject(context, p))
                        .ToArray());
            };

            Get["/project/{id}/report"] = prms =>
            {
                var id = (int)prms.id;
                var left = DateTime.MinValue;
                var right = DateTime.MaxValue;
                var current = DateTime.Now.AddDays(1);
                current = current.Date.AddTicks(-1);    // 現在日付+1から1ミリ秒引いて23:59:59を生成する

                var tryGetRangeDate = Fn.New((DateTime d, string s) => {
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        return d;
                    }
                    var a = DateTime.MinValue;
                    if (DateTime.TryParse(s, out a) == false)
                        return d;
                    return a;
                });
                left = tryGetRangeDate(left, (string)this.Request.Query["startDay"]);
                right = tryGetRangeDate(right, (string)this.Request.Query["endDay"]);
                //if (right != DateTime.MaxValue)
                //{
                //    current = right;
                //}

                var query =
                    from project in context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrNothing()
                    from workdayContext in MyClass.GetWorkdayContext(workdaySettingFolder, id)
                    let sprintToGraphModel = new SprintToGraphModel(workdayContext)
                    select new { project, sprintToGraphModel };
                var view = query.Select(q =>
                {
                    var project = q.project;
                    var sprintToGraphModel = q.sprintToGraphModel;
                    var trendChartModel = MakeTrendChartModel(this.AddIconFilePath(Request.Url,project), sprintToGraphModel);
                    var piChartModel = MakePiChartModel(trendChartModel);
                    var workDaysPV =
                        project.Sprints.Select(sprintToGraphModel.Make)
                        .Aggregate(new GraphModel(), (a, b) => new GraphModel
                        {
                            Pv = sprintToGraphModel.Merge(a.Pv, b.Pv),
                            Ev = sprintToGraphModel.Merge(a.Ev, b.Ev),
                            Ac = sprintToGraphModel.Merge(a.Ac, b.Ac),
                        }).Pv;

                    // workDaysに日付とValueの配列が含まれているので、Valueが0じゃない（非稼働日じゃない）
                    // かつ現在日付以降の日数を計算する
                    var workDays = workDaysPV.Where(x => x.Day > current).Where(x => x.Value != 0).Count();

                    var spi = piChartModel.Item1.Reverse().FirstOrDefault(x => x.Day <= current);
                    var cpi = piChartModel.Item2.Reverse().FirstOrDefault(x => x.Day <= current);
                    var totalValue = project.Sprints.SelectMany(s => s.Tasks).Where(t => t.Value.HasValue).Sum(t => t.Value.Value);
                    var progressValue =
                        project.Sprints.SelectMany(s => s.Tasks)
                        .OfType<ITaskWithRecord>()
                        .SelectMany(t => t.Records)
                        .Where(x => x.Day <= current)
                        .Sum(r => r.Value);
                    var toDayPv =
                        trendChartModel.Pv.Reverse().Where(x => x.Day <= current)
                        .Select(p => p.Value)
                        .FirstOrDefault();
                    var progress = progressValue - toDayPv;
                    var remaining = totalValue - progressValue;
                    var average = (totalValue - progressValue) / workDays;


                    var days = trendChartModel.Pv.Where(x=>left<= x.Day && x.Day <= right).Select(x=> x.Day.ToString("yyyy/MM/dd"));
                    var pvx = trendChartModel.Pv.Where(x=>left<= x.Day && x.Day <= right).Select(x => x.Value);
                    var evx = trendChartModel.Ev.Where(x=>left<= x.Day && x.Day <= right).Select(x => x.Value);
                    var acx = trendChartModel.Ac.Where(x => left <= x.Day && x.Day <= right).Select(x => x.Value);
                    var spix = piChartModel.Item1.Where(x=>left<= x.Day && x.Day <= right).Select(x => x.Value);
                    var cpix = piChartModel.Item2.Where(x => left <= x.Day && x.Day <= right).Select(x => x.Value);

                    return Response.AsJson(
                        new
                        {
                            workDays,
                            spi = spi.Value,
                            cpi = cpi.Value,
                            progress,
                            remaining,
                            average = double.IsInfinity(average) ? remaining : average,
                            days,
                            pvx,
                            evx,
                            acx,
                            spix,
                            cpix,
                        }) as object;
                });
                return
                    view.Return(() => HttpStatusCode.InternalServerError);
            };

            Get["/project/{id}/works"] = prms =>
            {
                var id = (int)prms.id;

                var query =
                    from project in context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrNothing()
                    let records =
                        from sprint in project.Sprints
                        from task in sprint.Tasks.OfType<ITaskWithRecord>()
                        from record in task.Records
                        select record
                    let dx = project.Sprints.SelectMany(x=>new[] { x.Left, x.Right } )
                            .Where(x=>x.HasValue)
                    select new {
                        records,
                        minDay = dx.Min(x=>x.Value),
                        maxDay = dx.Max(x=>x.Value) };
                var view = query.Select(q =>
                {   
                    return Response.AsJson(q) as object;
                });
                return
                    view.Return(() => HttpStatusCode.InternalServerError);
            };

            Get["/tasks/"] = _ =>
            {
                return
                    Response.AsJson(
                        context.GetTaskRecords(p => true).Select(t=>
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
                context.AddUser(model);

                return Response.AsJson(new { }, HttpStatusCode.OK);
            }; 

            Post["/users/withIcon/"] = _ =>
            {
                try
                {

                    //var model = this.Bind<AddUserWithIcon>();
                    var model = JsonConvert.DeserializeObject<AddUserWithIcon>(this.Request.Body.ToStreamString());
                    var targetUser =
                        context.GetUser(p => p.Id == model.UserId).FirstOrDefault();
                    if (targetUser == null)
                    {
                        return HttpStatusCode.BadRequest;
                    }
                    
                    var bytes = Convert.FromBase64String(model.Base64BytesByImage);
                    context.AddUserIcon(targetUser, bytes);

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

                var user =
                    from c in this.Context.CurrentUser.ToMaybe()
                    from name in c.UserName.ToMaybe()
                    from u in context.GetUser(x => x.Id == name).FirstOrNothing()
                    select u;

                project.On(p => context.AddProject(p, user.Return()));

                return Response.AsJson(new { }, project.IsSomething ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            };

            Post["/task/comment/"] = _ =>
            {
                //var model = this.Bind<AddTaskComment>();
                var json = this.Request.Body.ToStreamString();
                var model = JsonConvert.DeserializeObject<AddTaskComment>(json);
                var project = context.GetProjectFromTask(model.TaskId);
                var permission = context.GetAccessProjectPermission(project.Id);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                context.AddTaskComment(model.Comment, model.TaskId);

                return Response.AsJson(json, HttpStatusCode.OK);
            };
            Post["/task/record/"] = _ =>
            {
                var model = JsonConvert.DeserializeObject<AddTaskRecord>(this.Request.Body.ToStreamString());
                var project = context.GetProjectFromTask(model.TaskId);
                var permission = context.GetAccessProjectPermission(project.Id);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                context.AddTaskRecord(model.Record, model.TaskId);

                return Response.AsJson(new { }, HttpStatusCode.OK);
            };
            
            Delete["/projects/"] = _ =>
            {
                var res = Response.AsJson(new { }, HttpStatusCode.OK);
                var json = this.Request.Body.ToStreamString();
                var jObj = JObject.Parse(json);
                var projectId = jObj["projectid"].Value<int>();
                var permission =
                    context.GetDeleteProjectPermission();

                if (IsEnableUser(context, permission))
                {
                    var project =
                    context.GetProjects(x => x.Id == projectId)
                        .Select(p => MyClass.ToWithRecordsProject(context, p))
                        .FirstOrNothing();

                    project.On(context.DeleteProject);
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
                    context.GetDeleteUserPermission();

                if (IsEnableUser(context, permission))
                {
                    var user =
                        context.GetUser(x => x.Id.Equals(userId)).FirstOrNothing();

                    user.On(context.DeleteUser);
                }
                else res = HttpStatusCode.Forbidden;

                return res;
            };

            Delete["/task/record/"] = _ =>
            {
                var res = Response.AsJson(new { }, HttpStatusCode.OK);
                var model = JsonConvert.DeserializeObject<DeleteTaskRecord>(this.Request.Body.ToStreamString());
                var permission =
                    context.GetDeleteTaskRecordPermission(Tuple.Create(model.TaskId, model.RecordId));

                var taskWithRecord =
                    context.GetTaskRecords(x => x.Id.Equals(model.TaskId)).FirstOrNothing();
                
                taskWithRecord.On(task =>
                {
                    if (!IsEnableUser(context, permission))
                    {
                        // not permitted
                        res = Response.AsJson(new { }, HttpStatusCode.Forbidden);
                        return;
                    }
                    context.DeleteTaskRecord(task, model.RecordId);
                });

                return res;
            };

            Post["/task/status/save"] = _ =>
            {
                var res = Response.AsJson(new { }, HttpStatusCode.OK);
                var model = JsonConvert.DeserializeObject<SaveTasksStatus>(this.Request.Body.ToStreamString());
                var permission = context.GetAccessProjectPermission(model.ProjectId);

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                var query =
                    from task in context.GetTaskRecords(a => true)
                    from a in model.Tasks
                    where task.Id == a.TaskId
                    where task.StatusCode != a.StatusCode
                    select new { T = task, Status = a.StatusCode };

                // todo database のNameを参照する？
                var toStatusName = Fn.New<int, string>(x => x.ToGuards()
                                                                .When(3, a => "Done")
                                                                .When(2, a => "In Progress")
                                                                .When(1, a => "Ready")
                                                                .Return("Backlog"));
                var user = context.GetUser(x => x.Id.Equals(Context.CurrentUser.UserName)).First();
                var date = DateTime.Now;
                foreach (var item in query)
                {
                    var oldStatus = toStatusName(item.T.StatusCode);
                    var newStatus = toStatusName(item.Status);
                    var message = string.Format("Changed status: {0} -> {1}", oldStatus, newStatus);
                    var comment = new TaskComment()
                    {
                        Who = user,
                        Day = date,
                        Text = message,
                    };
                    context.AddTaskComment(comment, item.T.Id);
                    item.T.StatusCode = item.Status;
                    context.UpdateTask(item.T);
                }

                return res;
            };

            Get["/project/{id}/members"] = prms =>
            {
                var id = (int)prms.id;
                var users =
                    context.GetUserOfProject(id)
                            .Select(u => this.AddIconFilePath(this.Request.Url, u))
                            .ToArray();

                return
                    Response.AsJson(users, HttpStatusCode.OK);
            };

            Post["/project/members/add"] = prms =>
            {
                var json = this.Request.Body.ToStreamString();
                var jObj = JObject.Parse(json);
                var projectId = jObj["projectId"].Value<int>();
                var userId = jObj["userId"].Value<string>();
                var user =
                    context.GetUser(x => x.Id == userId).FirstOrDefault();
                var permission =
                    context.GetAddProjectMemberPermission();

                if (user == default(IUser))
                    return HttpStatusCode.BadRequest;

                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                context.AddProjectMember(user, projectId);

                return
                    Response.AsJson(new { }, HttpStatusCode.OK);
            };
            
            Post["/project/members/delete"] = prms =>
            {
                var json = this.Request.Body.ToStreamString();
                var jObj = JObject.Parse(json);
                var projectId = jObj["projectId"].Value<int>();
                var userId = jObj["userId"].Value<string>();
                var user =
                    context.GetUser(x => x.Id == userId).FirstOrDefault();
                var permission =
                    context.GetDeleteProjectMemberPermission();

                if (user == default(IUser))
                    return HttpStatusCode.BadRequest;
                
                if (!IsEnableUser(context, permission))
                    return HttpStatusCode.Forbidden;

                context.DeleteProjectMember(user, projectId);

                return
                    Response.AsJson(new { }, HttpStatusCode.OK);
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

        // todo クラス化
        private Tuple<IEnumerable<IGraphPoint>, IEnumerable<IGraphPoint>> MakePiChartModel(IGraphModel stacked)
        {
            var days =
                stacked.Pv.Concat(stacked.Ev).Concat(stacked.Ac).Select(x => x.Day).Distinct();

            var pis =
                from d in days
                let pv = stacked.Pv.Where(x => x.Day == d).Select(x => x.Value).FirstOrDefault()
                let ev = stacked.Ev.Where(x => x.Day == d).Select(x => x.Value).FirstOrDefault()
                let ac = stacked.Ac.Where(x => x.Day == d).Select(x => x.Value).FirstOrDefault()
                select new { d, spi = ev / pv, cpi = ev / ac };
            var spis = pis.Select(x => new GraphPoint { Day = x.d, Value = double.IsNaN(x.spi) ? 0.0 : x.spi }).ToArray();
            var cpis = pis.Select(x => new GraphPoint { Day = x.d, Value = double.IsNaN(x.cpi) ? 0.0 : x.cpi }).ToArray();

            var points =
                spis
                .Concat(cpis).ToArray();

            if (points.Any() == false)
            {
                return null;
            }

            return Tuple.Create(spis.OfType<IGraphPoint>(), cpis.OfType<IGraphPoint>());
        }
        // todo クラス化
        private IGraphModel MakeTrendChartModel(IProject project, ISprintToGraphModel sprintToGraphModel)
        {
            var sx = project.Sprints;

            // todo グラフデータに変換
            // xは日付
            // yはValue
            var gx =
                (from s in sx
                 let g = sprintToGraphModel.Make(s)
                 select g).ToArray();

            // マージする
            var merged =
                new GraphModel
                {
                    Pv = gx.Select(x => x.Pv).Foldl(Enumerable.Empty<IGraphPoint>(), (a, x) => sprintToGraphModel.Merge(a, x)).ToArray(),
                    Ev = gx.Select(x => x.Ev).Foldl(Enumerable.Empty<IGraphPoint>(), (a, x) => sprintToGraphModel.Merge(a, x)).ToArray(),
                    Ac = gx.Select(x => x.Ac).Foldl(Enumerable.Empty<IGraphPoint>(), (a, x) => sprintToGraphModel.Merge(a, x)).ToArray(),
                };
            // 抜けてる日付をorする
            var allDays = merged.Pv.Concat(merged.Ev).Concat(merged.Ac).Select(x => x.Day).Distinct();
            var filledBlank =
                new GraphModel
                {
                    Pv = sprintToGraphModel.ToFillBlank(merged.Pv, allDays).ToArray(),
                    Ev = sprintToGraphModel.ToFillBlank(merged.Ev, allDays).ToArray(),
                    Ac = sprintToGraphModel.ToFillBlank(merged.Ac, allDays).ToArray(),
                };
            // 値を積み上げる
            var stacked =
                new GraphModel
                {
                    Pv = sprintToGraphModel.ToStacked(filledBlank.Pv).ToArray(),
                    Ev = sprintToGraphModel.ToStacked(filledBlank.Ev).ToArray(),
                    Ac = sprintToGraphModel.ToStacked(filledBlank.Ac).ToArray(),
                };
            return stacked;

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

        public class SaveTasksStatus
        {
            public int ProjectId { get; set; }
            public TaskStatus[] Tasks { get; set; }
        }

        public class TaskStatus
        {
            public int TaskId { get; set; }
            public int StatusCode { get; set; }
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

        public static IProject AddIconFilePath(this NancyModule @this, Url url, IProject project)
        {
            if (project == null)
            {
                return project;
            }

            project.Sprints.ForEach(x => x.Tasks.OfType<ITaskWithRecord>().ForEach(t =>
              {
                  (t as TaskWithRecord).Assign = @this.AddIconFilePath(url, t.Assign);

                  t.Comments.ForEach(c => (c as TaskComment).Who = @this.AddIconFilePath(url, c.Who));
                  t.Records.ForEach(r => (r as TaskRecord).Who = @this.AddIconFilePath(url, r.Who));
              }));
            return project;
        }

        public static IProject ToWithRecordsProject(IDataBaseContext context, IProject project)
        {
            // hack BindするためにSprintのITaskItemをITaskWithRecordに差し替える。
            var taskWithRecords = context.GetTaskRecords(_ => true).ToArray();
            var getWithTask = Fn.New((ITaskItem t) =>
            {
                var finded = taskWithRecords.FirstOrDefault(x => x.Id == t.Id);
                if (finded == null)
                {
                    return t;
                }
                //(finded as TaskWithRecord).Assign = this.AddIconFilePath(this.Request.Url, finded.Assign);
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

            return toWithRecordsProject(project);
        }


        public static IMaybe<IWorkdayContext> GetWorkdayContext(string workdaySettingFolder, int projectId)
        {
            var context = new WorkdayContext(Path.Combine(workdaySettingFolder, projectId + ".json"));
            return (context as IWorkdayContext).ToMaybe();
        }
    }

    public class LoginApiModule : NancyModule
    {
        public LoginApiModule(IDataBaseContext context) : base("/api/")
        {
            Post["/login/"] = _ =>
            {
                var model = this.Bind<User>();

                var user =
                    context.GetUser(u => u.Id == model.Id && u.Password == model.Password)
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
