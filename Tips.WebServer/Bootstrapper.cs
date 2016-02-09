using Microsoft.Practices.Unity;
using Nancy.Authentication.Basic;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Unity;
using Nancy.Conventions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Controllers;
using Tips.Core.Events;
using Tips.Core.Services;
using Tips.Model.Context;
using Tips.Model.Models;
using Tips.WebServer.Services;

namespace Tips.WebServer
{
    public class Bootstrapper : UnityNancyBootstrapper
    {
        private object[] controllers;

        /// <summary>
        /// 静的コンテンツの読み込み先
        /// </summary>
        /// <param name="nancyConventions"></param>
        protected override void ConfigureConventions(Nancy.Conventions.NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.StaticContentsConventions.AddDirectory("css", @"content/css");
            nancyConventions.StaticContentsConventions.AddDirectory("fonts", @"content/fonts");
            nancyConventions.StaticContentsConventions.AddDirectory("js", @"content/js");
            nancyConventions.StaticContentsConventions.AddDirectory("img", @"content/img");
        }

        /// <summary>
        /// 依存モジュールの定義
        /// </summary>
        /// <param name="existingContainer"></param>
        protected override void ConfigureApplicationContainer(IUnityContainer existingContainer)
        {
            base.ConfigureApplicationContainer(existingContainer);

            // PubSubイベントAggregator
            existingContainer.RegisterType<IEventAggregator, EventAggregator>(new ContainerControlledLifetimeManager());
            var authUser = existingContainer.Resolve<UserValidator>();
            existingContainer.RegisterInstance<IUserValidator>(authUser);
            existingContainer.RegisterInstance<IUserMapper>(authUser);

            var dbPath =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    , "db");
            existingContainer.RegisterType<IDataSource, SqliteContext>(
                new InjectionConstructor(dbPath));

            existingContainer.RegisterType<IDataBaseSource<SqliteContext>, DataBaseSource<SqliteContext>>(
                new InjectionConstructor(Fn.New(() => existingContainer.Resolve<IDataSource>() as SqliteContext)));

            existingContainer.RegisterType<IDataBaseContext, DataBaseContext<SqliteContext>>();

            existingContainer.RegisterType<ITaskToTextFactory, TaskToTextFactory>();

            //// ssh git server test
            //var projectPath = @"C:\Users\Shuichi\home\src";
            //var gitPath = @"C:\Users\Shuichi\AppData\Local\Atlassian\SourceTree\git_local\bin";
            //this.sshGitTest = new SshModule(gitPath, projectPath);
            //sshGitTest.Start();

            //// Locatorを生成
            //ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(existingContainer));
        }

        /// <summary>
        /// 起動処理
        /// </summary>
        /// <param name="container"></param>
        /// <param name="pipelines"></param>
        protected override void ApplicationStartup(IUnityContainer container, IPipelines pipelines)
        {
            // 認証設定
            EnableBasicAuth(container, pipelines);
            EnableFormAuth(container, pipelines);

            // コントローラー群を起動
            this.controllers = MakeControllers(container).ToArray();

            // debug admin追加
            var isNothingAdmin =
                from c in container.ToMaybe()
                from ev in c.Resolve<IEventAggregator>().ToMaybe()
                let admin = ev.GetEvent<GetUserEvent>().Get(u => u.Id == "admin").FirstOrNothing()
                where admin.IsNothing
                select ev;
            isNothingAdmin.On(ev => ev.GetEvent<AddUserEvent>().Publish(new User { Id = "admin", Name = "Admin", Password = "admin", Role = UserRole.Admin }));

            Nancy.Json.JsonSettings.MaxJsonLength = int.MaxValue;

            base.ApplicationStartup(container, pipelines);
        }

        private IEnumerable<object> MakeControllers(IUnityContainer container)
        {
            yield return container.Resolve<DataBaseContextController>();
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

        private void EnableFormAuth(IUnityContainer container, IPipelines pipelines)
        {
            var formsAuthConfiguration =
               new FormsAuthenticationConfiguration()
               {
                   RedirectUrl = "~/",
                   UserMapper = container.Resolve<IUserMapper>(),
               };

            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);
        }
    }
}
