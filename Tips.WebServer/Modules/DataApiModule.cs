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

            Get["/project/{id}/report"] = prms =>
            {
                var id = (int)prms.id;
                var left = DateTime.MinValue;
                var right = DateTime.MaxValue;
                var current = DateTime.Now.AddDays(1).AddTicks(-1);

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
                    from project in eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == id).FirstOrNothing()
                    from workdayContext in eventAgg.GetEvent<GetWorkdayContextEvent>().Get(id)
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
                    from project in eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == id).FirstOrNothing()
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
