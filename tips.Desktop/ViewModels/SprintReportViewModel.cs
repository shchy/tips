using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using Tips.Core.Services;
using Tips.Desktop.Modules;
using Tips.Model.Models;

namespace Tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class SprintReportViewModel : BindableBase, INavigationAware
    {
        private ISprintToGraphModel debug;
        private IEventAggregator eventAgg;

        public SprintReportViewModel(IEventAggregator eventAgg
            , ISprintToGraphModel debug) // todo これはWeb側でやるべき
        {
            this.eventAgg = eventAgg;
            this.debug = debug;
        }

        public object PIModel { get; private set; }
        public object TrendModel { get; private set; }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            this.TrendModel = null;

            var query = navigationContext.TryToGetSprints(this.eventAgg);
            query.On(sx =>
            {
                // todo グラフデータに変換
                // xは日付
                // yはValue
                var gx =
                    (from s in sx
                    let g = this.debug.Make(s)
                    select g).ToArray();

                // マージする
                var merged =
                    new GraphModel
                    {
                        Pv = gx.Select(x => x.Pv).Foldl(Enumerable.Empty<IGraphPoint>(), (a, x) => this.debug.Merge(a, x)).ToArray(),
                        Ev = gx.Select(x => x.Ev).Foldl(Enumerable.Empty<IGraphPoint>(), (a, x) => this.debug.Merge(a, x)).ToArray(),
                        Ac = gx.Select(x => x.Ac).Foldl(Enumerable.Empty<IGraphPoint>(), (a, x) => this.debug.Merge(a, x)).ToArray(),
                    };

                var allDays = merged.Pv.Concat(merged.Ev).Concat(merged.Ac).Select(x => x.Day).Distinct();
                var filledBlank =
                    new GraphModel
                    {
                        Pv = this.debug.ToFillBlank(merged.Pv, allDays).ToArray(),
                        Ev = this.debug.ToFillBlank(merged.Ev, allDays).ToArray(),
                        Ac = this.debug.ToFillBlank(merged.Ac, allDays).ToArray(),
                    };
                    
                var stacked =
                    new GraphModel
                    {
                        Pv = this.debug.ToStacked(filledBlank.Pv).ToArray(),
                        Ev = this.debug.ToStacked(filledBlank.Ev).ToArray(),
                        Ac = this.debug.ToStacked(filledBlank.Ac).ToArray(),
                    };

                this.TrendModel = MakeTrendModel(stacked);
                this.PIModel = MakePiModel(stacked);
            });
            
        }

        private object MakePiModel(GraphModel stacked)
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
            var minX = points.Min(x => x.Day).AddDays(-1);
            var maxX = points.Max(x => x.Day).AddDays(+1);
            //var minY = points.Min(x => x.Value);
            var minY = 0;
            var maxY = points.Max(x => x.Value) * 1.1;

            var intervalX = (int)Math.Ceiling((maxX - minX).TotalDays / 4);
            var intervalY = (maxY - minY) / 3;

            return
                new
                {
                    MaxX = maxX,
                    MinX = minX,
                    MaxY = maxY,
                    MinY = minY,
                    IntervalX = intervalX,
                    IntervalY = intervalY,
                    Spi = spis,
                    Cpi = cpis,
                };
        }

        private object MakeTrendModel(GraphModel model)
        {

            var points =
                model.Pv
                .Concat(model.Ev)
                .Concat(model.Ac).ToArray();

            if (points.Any() == false)
            {
                return null;
            }


            var minX = points.Min(x => x.Day).AddDays(-1);
            var maxX = points.Max(x => x.Day).AddDays(+1);
            //var minY = points.Min(x => x.Value);
            var minY = 0;
            var maxY = points.Max(x => x.Value) * 1.1;

            var intervalX = (int)Math.Ceiling((maxX - minX).TotalDays / 4);
            var intervalY = (maxY - minY) / 3;

            return
                new
                {
                    MaxX = maxX,
                    MinX = minX,
                    MaxY = maxY,
                    MinY = minY,
                    IntervalX = intervalX,
                    IntervalY = intervalY,
                    Pv = model.Pv,
                    Ev = model.Ev,
                    Ac = model.Ac,
                };
        }
    }
}
