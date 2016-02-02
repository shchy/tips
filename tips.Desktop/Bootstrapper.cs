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


            //var dbPath =
            //    Path.Combine(
            //        Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            //        , "db");
            //this.Container.RegisterType<IDataSource, SqliteContext>(
            //    new InjectionConstructor(dbPath));

            //this.Container.RegisterType<IDataBaseSource<SqliteContext>, DataBaseSource<SqliteContext>>(
            //    new InjectionConstructor(Fn.New(()=> Container.Resolve<IDataSource>() as SqliteContext)));

            //this.Container.RegisterType<IDataBaseContext, DataBaseContext<SqliteContext>>();
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
