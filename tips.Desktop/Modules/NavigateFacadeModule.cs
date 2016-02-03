using Haskellable.Code.Monads.Maybe;
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
using Tips.Desktop.Views;
using Tips.Core.Events;
using Tips.Model.Models;

namespace Tips.Desktop.Modules
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
            this.container.RegisterType<object, LoginView>(ViewNames.ROOT);
            this.container.RegisterType<object, TopMenuView>(typeof(TopMenuView).ToString());
            this.container.RegisterType<object, UserView>(ViewNames.USER);
            this.container.RegisterType<object, ProjectsView>(ViewNames.PROJECTS);
            this.container.RegisterType<object, CreateProjectView>(ViewNames.CREATE_PROJECT);
            this.container.RegisterType<object, ProjectView>(ViewNames.PROJECT);
            this.container.RegisterType<object, ProjectInBacklogView>(ViewNames.PROJECT_IN_BACKLOG);
            this.container.RegisterType<object, ProjectInBacklogEditView>(ViewNames.PROJECT_IN_BACKLOG_EDIT);
            this.container.RegisterType<object, TaskItemView>(ViewNames.PROJECT_IN_TASKITEM);

            this.eventAgg.GetEvent<NavigateEvent>().Subscribe(Navigate, true);
            this.eventAgg.GetEvent<NavigateInProjectViewEvent>().Subscribe(NavigateInProjectView, true);


            // 初期画面
            this.eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.ROOT);
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
                regionManager.Move(RegionNames.TopMenuRegion, typeof(TopMenuView).ToString(), order.Prms);
            }
            else
            {
                regionManager.Clear(RegionNames.TopMenuRegion);
            }

            regionManager.Clear(RegionNames.ProjectContentRegion);

            regionManager.Move(RegionNames.ContentRegion, order.ViewName, order.Prms);
        }
    }


    

    public class ViewNames
    {
        public static readonly string ROOT = typeof(LoginView).ToString();
        public static readonly string PROJECTS = typeof(ProjectsView).ToString();
        public static readonly string USER = typeof(UserView).ToString();
        public static readonly string PROJECT = typeof(ProjectView).ToString();
        public static readonly string CREATE_PROJECT = typeof(CreateProjectView).ToString();
        public static readonly string PROJECT_IN_BACKLOG = typeof(ProjectInBacklogView).ToString();
        public static readonly string PROJECT_IN_BACKLOG_EDIT = typeof(ProjectInBacklogEditView).ToString();
        public static readonly string PROJECT_IN_TASKITEM = typeof(TaskItemView).ToString();
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
            if (regionManager.Regions.ContainsRegionWithName(regionName) == false)
            {
                return;
            }
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

        public static IMaybe<IProject> TryToGetProject(this NavigationContext @this, IEventAggregator eventAgg)
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

            var query =
                from c in @this.ToMaybe()
                let find =
                    from p in c.Parameters
                    where p.Key == "ProjectId"
                    select p.Value
                from v in find.FirstOrNothing()
                let projects =
                    from p in eventAgg.GetEvent<GetProjectEvent>().Get(_ => true)
                    where p.Id == (int)v
                    select p
                from p in projects.FirstOrNothing()
                let withP = toWithRecordsProject(p)
                select p;
            return query;
        }

        public static IMaybe<ITaskWithRecord> TryToGetProjectInTask(this NavigationContext @this, IEventAggregator eventAgg)
        {
            var query =
                from c in @this.ToMaybe()
                //let find =
                //    from p in c.Parameters
                //    where p.Key == "ProjectId"
                //    select p.Value
                //from v in find.FirstOrNothing()
                let findTaskId =
                    from p in c.Parameters
                    where p.Key == "TaskItemId"
                    select p.Value
                from taskId in findTaskId.FirstOrNothing()
                let taskx =
                    //from p in eventAgg.GetEvent<GetProjectEvent>().Get(_ => true)
                    //where p.Id == (int)v
                    //from s in p.Sprints
                    //from t in s.Tasks
                    from t in eventAgg.GetEvent<GetTaskWithRecordEvent>().Get(_ => true)
                    where t.Id == (int)taskId
                    select t
                from t in taskx.FirstOrNothing()
                select t;
            return query;
        }

        public static IMaybe<IUser> TryToGetUser(this NavigationContext @this, IEventAggregator eventAgg)
        {
            var query =
                from c in @this.ToMaybe()
                let findUserId =
                    from p in c.Parameters
                    where p.Key == "UserId"
                    select p.Value
                from userId in findUserId.FirstOrNothing()
                from user in eventAgg.GetEvent<GetUserEvent>().Get(u => u.Id == userId.ToString()).FirstOrNothing()
                select user;
            return query;
        }
    }

}
