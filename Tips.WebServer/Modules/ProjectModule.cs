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
        private ISprintToGraphModel sprintToGraphModel;
        private IEventAggregator eventAgg;

        public ProjectModule(IEventAggregator eventAgg, ITaskToTextFactory taskToText, ISprintToGraphModel sprintToGraphModel)
            : base("/project/")
        {
            this.eventAgg = eventAgg;
            this.sprintToGraphModel = sprintToGraphModel;

            this.RequiresAuthentication();

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

            Get["/create"] = prms =>
            {
                return View["Views/CreateProject"];
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
                
                var project =
                    eventAgg.GetEvent<GetProjectEvent>().Get(x => x.Id == id).FirstOrDefault();
                var trendChartModel = MakeTrendChartModel(ToWithRecordsProject(project));
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
                    , new {
                        Project = project
                        , TrendChartModel = trendChartModel
                        , PiChartModel = piChartModel
                        , WorkDays = workDays
                    }];

                //var piChartModel = MakePiChartModel(trendChartModel);

                //var totalValue = project.Sprints.SelectMany(x => x.Tasks).Where(x=>x.Value.HasValue).Sum(x => x.Value.Value);
                //var progressValue =
                //    project.Sprints.SelectMany(x => x.Tasks).OfType<ITaskWithRecord>()
                //    .SelectMany(x => x.Records)
                //    .Sum(x => x.Value);
                //var toDayPv = trendChartModel.Pv.Reverse().Where(x => x.Day <= DateTime.Now).Select(x=>x.Value).FirstOrDefault();
                //var workDays =
                //    project.Sprints.Select(sprintToGraphModel.Make)
                //    .Aggregate(new GraphModel(), (a, b) => new GraphModel
                //    {
                //        Pv = sprintToGraphModel.Merge(a.Pv, b.Pv),
                //        Ev = sprintToGraphModel.Merge(a.Ev, b.Ev),
                //        Ac = sprintToGraphModel.Merge(a.Ac, b.Ac),
                //    }).Pv.Where(x => x.Value != 0).Count();

                //return View["Views/ProjectReport"
                //    , new
                //    {
                //        Project = project,
                //        Days = string.Join(",", trendChartModel.Pv.Select(x => string.Format("'{0}'", x.Day.ToString("yyyy/MM/dd"))).ToArray()),
                //        Trend = new
                //        {
                //            Pv = string.Join(",", trendChartModel.Pv.Select(x => x.Value.ToString()).ToArray()),
                //            Ev = string.Join(",", trendChartModel.Ev.Select(x => x.Value.ToString()).ToArray()),
                //            Ac = string.Join(",", trendChartModel.Ac.Select(x => x.Value.ToString()).ToArray()),
                //        },
                //        PI = new
                //        {
                //            Spi = string.Join(",", piChartModel.Item1.Select(x => x.Value.ToString()).ToArray()),
                //            Cpi = string.Join(",", piChartModel.Item2.Select(x => x.Value.ToString()).ToArray()),
                //        },
                //        Spi = piChartModel.Item1.Reverse().FirstOrDefault(x => x.Day <= DateTime.Now),
                //        Cpi = piChartModel.Item2.Reverse().FirstOrDefault(x => x.Day <= DateTime.Now),
                //        Progress = progressValue - toDayPv,
                //        Remaining = totalValue - progressValue,
                //        Average = (totalValue - progressValue) / workDays,
                //    }];
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

        private IGraphModel MakeTrendChartModel(IProject project)
        {
            var sx = project.Sprints;

            // todo グラフデータに変換
            // xは日付
            // yはValue
            var gx =
                (from s in sx
                 let g = this.sprintToGraphModel.Make(s)
                 select g).ToArray();

            // マージする
            var merged =
                new GraphModel
                {
                    Pv = gx.Select(x => x.Pv).Foldl(Enumerable.Empty<IGraphPoint>(), (a, x) => this.sprintToGraphModel.Merge(a, x)).ToArray(),
                    Ev = gx.Select(x => x.Ev).Foldl(Enumerable.Empty<IGraphPoint>(), (a, x) => this.sprintToGraphModel.Merge(a, x)).ToArray(),
                    Ac = gx.Select(x => x.Ac).Foldl(Enumerable.Empty<IGraphPoint>(), (a, x) => this.sprintToGraphModel.Merge(a, x)).ToArray(),
                };
            // 抜けてる日付をorする
            var allDays = merged.Pv.Concat(merged.Ev).Concat(merged.Ac).Select(x => x.Day).Distinct();
            var filledBlank =
                new GraphModel
                {
                    Pv = this.sprintToGraphModel.ToFillBlank(merged.Pv, allDays).ToArray(),
                    Ev = this.sprintToGraphModel.ToFillBlank(merged.Ev, allDays).ToArray(),
                    Ac = this.sprintToGraphModel.ToFillBlank(merged.Ac, allDays).ToArray(),
                };
            // 値を積み上げる
            var stacked =
                new GraphModel
                {
                    Pv = this.sprintToGraphModel.ToStacked(filledBlank.Pv).ToArray(),
                    Ev = this.sprintToGraphModel.ToStacked(filledBlank.Ev).ToArray(),
                    Ac = this.sprintToGraphModel.ToStacked(filledBlank.Ac).ToArray(),
                };
            return stacked;
            
        }
    }
}
