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

        public GraphModel GraphModel { get; private set; }

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
                this.GraphModel = merged;
            });
            
        }
    }
}
