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
    // memo 特定のプロジェクトを開いている時はModelにProjectをいれるとMenuBarのprojectManageボタンが使える
    public class ProjectModule : NancyModule
    {
        private IEventAggregator eventAgg;

        public ProjectModule(IEventAggregator eventAgg, ITaskToTextFactory taskToText)
            : base("/project/")
        {
            this.eventAgg = eventAgg;
            
            this.RequiresAuthentication();

            Get["/create"] = prms =>
            {
                return View["Views/CreateProject"];
            };


            Get["/{id}"] = prms =>
            {
                var id = prms.id;
                
                var project =
                    eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == id).FirstOrDefault();
                var withRecord = ToWithRecordsProject(project);

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

            // todo APIに移動すべき
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
                var id = (int)prms.id;

                var query =
                    from project in eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == id).FirstOrNothing()
                    from workdayContext in this.eventAgg.GetEvent<GetWorkdayContextEvent>().Get(id)
                    let sprintToGraphModel = new SprintToGraphModel(workdayContext)
                    select new { project, sprintToGraphModel };
                var view = query.Select(q =>
                {
                    var project = q.project;
                    var sprintToGraphModel = q.sprintToGraphModel;
                    var trendChartModel = MakeTrendChartModel(ToWithRecordsProject(project), sprintToGraphModel);
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
                    var workDays = workDaysPV.Where(x => x.Day > DateTime.Now).Where(x => x.Value != 0).Count();


                    return View["Views/ProjectReport"
                        , new
                        {
                            Project = project
                            ,
                            TrendChartModel = trendChartModel
                            ,
                            PiChartModel = piChartModel
                            ,
                            WorkDays = workDays
                        }] as object;
                });

                return view.Return(() => Response.AsRedirect("/project/" + id));
            };
        }

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

        private IProject ToWithRecordsProject(IProject project)
        {
            // hack BindするためにSprintのITaskItemをITaskWithRecordに差し替える。

            var taskWithRecords = eventAgg.GetEvent<GetTaskWithRecordEvent>().Get(_ => true).ToArray();
            var getWithTask = Fn.New((ITaskItem t) =>
            {
                var finded = taskWithRecords.FirstOrDefault(x => x.Id == t.Id);
                if (finded == null)
                {
                    return t;
                }
                (finded as TaskWithRecord).Assign = this.AddIconFilePath(this.Request.Url, finded.Assign);
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
    }
}
