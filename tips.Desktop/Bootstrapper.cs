using Microsoft.Practices.Unity;
using Prism.Unity;
using tips.Desktop.Views;
using System.Windows;
using Tips.Model.Context;
using Prism.Modularity;
using tips.Desktop.Modules;
using Prism.Events;
using Tips.Core.Services;
using System.IO;
using System;

namespace tips.Desktop
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

            var dbPath =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    , "debugData");

            this.Container.RegisterType<IDataBaseContext, FileDataBaseContext>(
                new ContainerControlledLifetimeManager()
                , new InjectionConstructor(dbPath));

            this.Container.RegisterType<ITaskToTextFactory, TextToTaskFactory>();

        }

        protected override void ConfigureModuleCatalog()
        {
            base.ConfigureModuleCatalog();
            var catalog = (this.ModuleCatalog as ModuleCatalog);
            catalog.AddModule(typeof(ServiceModule));
            catalog.AddModule(typeof(NavigateFacadeModule));
        }

    }
}
