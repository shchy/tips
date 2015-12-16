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
using Nancy.Authentication.Forms;
using Tips.Web.Modules;
using Tips.Model.Models;
using Tips.GitServer;

namespace Tips.Boot
{
    public class Bootstrapper : Nancy.Bootstrappers.Unity.UnityNancyBootstrapper
    {
        private IEnumerable<object> controllers;
        private SshModule sshGitTest;

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
            existingContainer.RegisterInstance<IUserToGuid>(new UserToGuid());
            // Basic認証
            //existingContainer.RegisterType<IUserValidator, BasicAuthUserValidator>();
            // Form認証
            existingContainer.RegisterType<IUserMapper, FormAuthUserMapper>();

            // Locatorを生成
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(existingContainer));
        }

        protected override void ApplicationStartup(IUnityContainer container, IPipelines pipelines)
        {
            // 認証設定
            //EnableBasicAuth(container, pipelines);
            EnableFormAuth(container, pipelines);

            // コントローラーを起動
            this.controllers = MakeControllers().ToArray();

            // ssh git server test
            var projectPath = @"C:\Users\Shuichi\home\src";
            var gitPath = @"C:\Users\Shuichi\AppData\Local\Atlassian\SourceTree\git_local\bin";
            this.sshGitTest = new SshModule(gitPath, projectPath);
            sshGitTest.Start();

            base.ApplicationStartup(container, pipelines);

        }

        private void EnableFormAuth(IUnityContainer container, IPipelines pipelines)
        {
            var formsAuthConfiguration =
               new FormsAuthenticationConfiguration()
               {
                   RedirectUrl = "~/login",
                   UserMapper = container.Resolve<IUserMapper>(),
               };

            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);
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

    class UserToGuid : IUserToGuid
    {
        private Dictionary<string, Guid> cache;

        public UserToGuid()
        {
            this.cache = new Dictionary<string, Guid>();
        }

        public Guid ToGuid(IUser model)
        {
            if (this.cache.ContainsKey(model.Id) == false)
            {
                this.cache[model.Id] = Guid.NewGuid();
            }
            return this.cache[model.Id];
        }
    }
}