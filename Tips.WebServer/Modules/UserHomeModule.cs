using Nancy;
using Nancy.Security;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Model.Models;

namespace Tips.WebServer.Modules
{
    public class UserHomeModule : NancyModule
    {
        public UserHomeModule(IEventAggregator eventAgg)
            : base("/home/")
        {
            this.RequiresAuthentication();

            Get["/"] = prms =>
            {
                //var roles =
                //    Context.CurrentUser.Claims
                //    .Select(x => Enum.Parse(typeof(UserRole), x))
                //    .OfType<UserRole>();

                var user =
                    eventAgg.GetEvent<GetUserEvent>().Get(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();

                var projects =
                    eventAgg.GetEvent<GetProjectEvent>().Get(_ => true).ToArray();

                return View["Views/Home", new { Auth = user, Projects = projects }];
            };
        }
    }
}
