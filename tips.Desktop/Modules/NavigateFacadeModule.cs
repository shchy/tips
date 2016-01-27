using Microsoft.Practices.Unity;
using Prism.Events;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using tips.Desktop.Views;

namespace tips.Desktop.Modules
{
    public class NavigateFacadeModule : IModule
    {
        private IUnityContainer container;
        private IEventAggregator eventAgg;
        private IRegionManager regionManager;

        
        
        public NavigateFacadeModule(IEventAggregator eventAgg, IRegionManager regionManager, IUnityContainer container)
        {
            this.eventAgg = eventAgg;
            this.regionManager = regionManager;
            this.container = container;
        }

        public void Initialize()
        {
            this.container.RegisterType<object, TopMenuView>(typeof(TopMenuView).ToString());
            this.container.RegisterType<object, ProjectsView>(ViewNames.PROJECTS);
            this.container.RegisterType<object, CreateProjectView>(ViewNames.CREATE_PROJECT);
            this.container.RegisterType<object, ProjectView>(ViewNames.PROJECT);
            this.container.RegisterType<object, ProjectInBacklogView>(ViewNames.PROJECT_IN_BACKLOG);

            this.eventAgg.GetEvent<NavigateEvent>().Subscribe(Navigate, true);
            this.eventAgg.GetEvent<NavigateInProjectViewEvent>().Subscribe(NavigateInProjectView, true);
            


            // 初期画面
            this.eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.PROJECTS);
        }

        private void NavigateInProjectView(NavigateOrder order)
        {
            regionManager.Move(RegionNames.ProjectContentRegion, order.ViewName, order.Prms);
        }

        private void Navigate(NavigateOrder order)
        {
            var isVisibleTop =
                    order.ViewName != ViewNames.ROOT;

            if (isVisibleTop)
            {
                regionManager.Move(RegionNames.TopMenuRegion, typeof(TopMenuView).ToString());
            }
            else
            {
                regionManager.Clear(RegionNames.TopMenuRegion);
            }

            regionManager.Move(RegionNames.ContentRegion, order.ViewName, order.Prms);
        }
    }


    

    public class ViewNames
    {
        public static readonly string ROOT = "/";
        public static readonly string PROJECTS = typeof(ProjectsView).ToString();
        public static readonly string PROJECT = typeof(ProjectView).ToString();
        public static readonly string CREATE_PROJECT = typeof(CreateProjectView).ToString();
        public static readonly string PROJECT_IN_BACKLOG = typeof(ProjectInBacklogView).ToString();

    }

    class RegionNames 
    {
        public static readonly string ContentRegion = "ContentRegion";
        public static readonly string TopMenuRegion = "TopMenuRegion";
        public static readonly string ProjectContentRegion = "ProjectContentRegion";
    }

    public class NavigateEvent : PubSubEvent<NavigateOrder> { }
    public class NavigateInProjectViewEvent : PubSubEvent<NavigateOrder> { }

    public class NavigateOrder
    {
        public string ViewName { get; set; }
        public IDictionary<string,object> Prms { get; set; }
    }

    public class NavigateParams : Dictionary<string,object>
    {

    }

    public static class IRegionManagerExtension
    {
        public static void Move(this IRegionManager regionManager, string regionName, string viewName, IDictionary<string, object> prms = null)
        {
            regionManager.Clear(regionName);

            var uri = new Uri(viewName, UriKind.Relative);

            var param = new NavigationParameters();
            param.Add(prms);
            regionManager.RequestNavigate(regionName, uri, param);

        }

        public static void Clear(this IRegionManager regionManager, string regionName)
        {
            var currentViews = regionManager.Regions[regionName].ActiveViews.ToArray();
            currentViews.ForEach(regionManager.Regions[regionName].Deactivate);
        }

        public static void Add(this NavigationParameters @this, IDictionary<string, object> prms)
        {
            if (prms == null)
            {
                return;
            }
            foreach (var item in prms)
            {
                @this.Add(item.Key, item.Value);
            }
        }

        public static void Publish(this NavigateEvent @this, string viewName, IDictionary<string, object> prms = null)
        {
            Navigate(@this, viewName, prms);

        }

        public static void Publish(this NavigateInProjectViewEvent @this, string viewName, IDictionary<string, object> prms = null)
        {
            Navigate(@this, viewName, prms);
        }

        private static void Navigate(PubSubEvent<NavigateOrder> @this, string viewName, IDictionary<string, object> prms)
        {
            var order = new NavigateOrder
            {
                ViewName = viewName,
                Prms = prms,
            };
            @this.Publish(order);
        }
    }

}
