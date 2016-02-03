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

        public IGraphModel GraphModel { get; private set; }
        public double MaxX { get; private set; }
        public double MaxY { get; private set; }
        public double MinX { get; private set; }
        public double MinY { get; private set; }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            this.GraphModel = null;

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

                var stacked =
                    new GraphModel
                    {
                        Pv = this.debug.ToStacked(merged.Pv).ToArray(),
                        Ev = this.debug.ToStacked(merged.Ev).ToArray(),
                        Ac = this.debug.ToStacked(merged.Ac).ToArray(),
                    };

                this.GraphModel = stacked;

                var points =
                    this.GraphModel.Pv
                    .Concat(this.GraphModel.Ev)
                    .Concat(this.GraphModel.Ac).ToArray();

                if (points.Any()==false)
                {
                    return;
                }
                this.MinX = points.Min(x => x.Day).AddHours(-1).ToOADate();
                this.MaxX = points.Max(x => x.Day).AddHours(+1).ToOADate();
                //var minY = points.Min(x => x.Value);
                var maxY = points.Max(x => x.Value);
                this.MinY = 0;
                this.MaxY = maxY * 1.1;

            });
            
        }
    }
}
