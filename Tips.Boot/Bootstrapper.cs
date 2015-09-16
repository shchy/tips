using Nancy.Conventions;
using Microsoft.Practices.Unity;
using Tips.Model.Context;
using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.ServiceLocation;
using Tips.Core.Controllers;
using Nancy.Bootstrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using Tips.Boot.Auth;
using Nancy.Security;

namespace Tips.Boot
{
    public class Bootstrapper : Nancy.Bootstrappers.Unity.UnityNancyBootstrapper
    {
        private IEnumerable<object> controllers;

        protected override void ConfigureConventions(Nancy.Conventions.NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.StaticContentsConventions.AddDirectory("css", @"content/css");
            nancyConventions.StaticContentsConventions.AddDirectory("fonts", @"content/fonts");
            nancyConventions.StaticContentsConventions.AddDirectory("js", @"content/js");
        }


        protected override void ConfigureApplicationContainer(IUnityContainer existingContainer)
        {
            base.ConfigureApplicationContainer(existingContainer);

            existingContainer.RegisterType<IEventAggregator, EventAggregator>(new ContainerControlledLifetimeManager());
            existingContainer.RegisterType<IDataBaseContext, InMemoryDataBaseContext>(new ContainerControlledLifetimeManager());

            // Basic認証
            existingContainer.RegisterType<IUserValidator, BasicAuthUserValidator>();

            // Locatorを生成
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(existingContainer));
        }

        protected override void ApplicationStartup(IUnityContainer container, IPipelines pipelines)
        {
            // 認証設定
            EnableBasicAuth(container, pipelines);
            // コントローラーを起動
            this.controllers = MakeControllers().ToArray();

            base.ApplicationStartup(container, pipelines);

        }

        private void EnableBasicAuth(IUnityContainer container, IPipelines pipelines)
        {
            var config =
                new BasicAuthenticationConfiguration(
                    container.Resolve<IUserValidator>()
                    , "you need login"
                    , UserPromptBehaviour.NonAjax);

            BasicAuthentication.Enable(pipelines, config);
        }

        private IEnumerable<object> MakeControllers()
        {
            yield return this.ApplicationContainer.Resolve<DataBaseContextController>();
        }
    }
}