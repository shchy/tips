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
    public class SystemManageModule : NancyModule
    {
        public SystemManageModule(
            IDataBaseContext context)
            : base("/systemmanage/")
        {
            this.RequiresAuthentication();
            this.RequiresClaims(new[] { UserRole.Admin.ToString() });

            Get["/"] = prms =>
            {
                return Response.AsRedirect("/systemmanage/user");
            };

            Get["/user"] = prms =>
            {
                var users =
                    context.GetUser(_ => true)
                    .Select(u => this.AddIconFilePath(Request.Url, u))
                    .ToArray();

                return View["Views/SystemManageUser", new { Users = users }];
            };

            Get["/user/add"] = prms =>
            {
                return View["Views/SystemManageUserAdd"];
            };
        }
    }
}
