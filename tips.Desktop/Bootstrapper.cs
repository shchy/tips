using Microsoft.Practices.Unity;
using Prism.Unity;
using Tips.Desktop.Views;
using System.Windows;
using Tips.Model.Context;
using Prism.Modularity;
using Tips.Desktop.Modules;
using Prism.Events;
using Tips.Core.Services;
using System.IO;
using System;
using System.Data.Entity;
using Tips.Core.Events;
using Tips.Model.Models;

namespace Tips.Desktop
{
    class Bootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            Application.Current.MainWindow.Show();
            Application.Current.Exit += Current_Exit;
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            this.Container.Dispose();
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            this.Container.RegisterType<IEventAggregator, EventAggregator>(new ContainerControlledLifetimeManager());

            // todo たぶんこれはWeb側でやるべき
            this.Container.RegisterType<ISprintToGraphModel, SprintToGraphModel>(
                new InjectionConstructor(Fn.New((DateTime day) => 
                    day.DayOfWeek == DayOfWeek.Monday
                    || day.DayOfWeek == DayOfWeek.Tuesday
                    || day.DayOfWeek == DayOfWeek.Wednesday
                    || day.DayOfWeek == DayOfWeek.Thursday
                    || day.DayOfWeek == DayOfWeek.Friday)));
            
            this.Container.RegisterType<IDataBaseContext, WebApiContext>(
                new InjectionConstructor(
                    "http://localhost:9876/"
                    , Fn.New(() => 
                        this.Container.Resolve<IEventAggregator>()
                        .GetEvent<GetAuthUserEvent>()
                        .Get().Return(null as IUser))));

            this.Container.RegisterType<ITaskToTextFactory, TaskToTextFactory>();

        }

        protected override void ConfigureModuleCatalog()
        {
            base.ConfigureModuleCatalog();
            var catalog = (this.ModuleCatalog as ModuleCatalog);
            catalog.AddModule(typeof(ServiceModule));
            catalog.AddModule(typeof(NavigateFacadeModule));
            catalog.AddModule(typeof(NotifyUser));
        }

    }
}
