using Nancy;
using Nancy.Security;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Model.Context;
using Tips.Model.Models;

namespace Tips.WebServer.Modules
{
    public class UserHomeModule : NancyModule
    {
        public UserHomeModule(
            IDataBaseContext context)
            : base("/home/")
        {
            this.RequiresAuthentication();

            Get["/"] = prms =>
            {
                var user =
                    context.GetUser(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();

                var projects =
                    context.GetProjectBelongUser(user).Select(p => MyClass.ToWithRecordsProject(context, p)).ToArray();

                return View["Views/Home", new { Auth = user, Projects = projects }];
            };
        }
    }
}
