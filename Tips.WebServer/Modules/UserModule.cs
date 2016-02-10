using Nancy;
using Nancy.Security;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;

namespace Tips.WebServer.Modules
{
    public class UserModule : NancyModule
    {
        public UserModule(IEventAggregator eventAgg)
            : base("/user/")
        {
            this.RequiresAuthentication();

            Get["/"] = prms =>
            {
                var userId = this.Context.CurrentUser.UserName;
                var url = string.Format("/user/{0}", userId);
                return Response.AsRedirect(url);
            };

            Get["/{id}"] = prms =>
            {
                var userId = prms.id.ToString();
                var url = string.Format("/user/{0}/edit", userId as string);
                return Response.AsRedirect(url);
            };


            Get["/{id}/edit"] = prms =>
            {
                var userId = prms.id;

                var user =
                    eventAgg.GetEvent<GetUserEvent>().Get(u => u.Id == userId).FirstOrDefault();

                return View["Views/UserEdit", this.AddIconFilePath(Request.Url, user)];
            };
        }
    }
}
