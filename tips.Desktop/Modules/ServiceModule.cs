using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Controllers;
using Tips.Model.Context;

namespace tips.Desktop.Modules
{
    public class ServiceModule : IModule
    {
        private IUnityContainer container;
        private IServiceLocator locator;
        private IEnumerable<object> services;

        public ServiceModule(IServiceLocator locator, IUnityContainer container)
        {
            this.locator = locator;
            this.container = container;
        }

        public void Initialize()
        {
            var dbPath = 
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    , "debugData");
            this.container.RegisterType<IDataBaseContext, FileDataBaseContext>(
                new ContainerControlledLifetimeManager()
                , new InjectionConstructor(dbPath));
            

            this.services = new[]
            {
                locator.GetInstance<DataBaseContextController>(),
            };
        }
    }
}
